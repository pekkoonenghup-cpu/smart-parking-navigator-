using CarparkAvailability.ApiApp.Models;

namespace CarparkAvailability.ApiApp.Services;

/// <summary>
/// Background service that periodically fetches live carpark availability and
/// maintains an immutable last-known-good snapshot. Holds the static HDB data
/// in memory after startup.
/// </summary>
public sealed class CarparkDataService : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(5);

    private readonly CsvDataService _csvService;
    private readonly DataGovSgClient _dataGovSgClient;
    private readonly ILogger<CarparkDataService> _logger;

    // Immutable state - replaced atomically
    private volatile CarparkSnapshot _snapshot = CarparkSnapshot.Empty;

    public CarparkDataService(
        CsvDataService csvService,
        DataGovSgClient dataGovSgClient,
        ILogger<CarparkDataService> logger)
    {
        _csvService = csvService;
        _dataGovSgClient = dataGovSgClient;
        _logger = logger;
    }

    public IReadOnlyDictionary<string, HdbCarparkInfo> StaticData { get; private set; } =
        new Dictionary<string, HdbCarparkInfo>(StringComparer.OrdinalIgnoreCase);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StaticData = _csvService.Load();
        await RefreshAvailabilityAsync(stoppingToken);

        using var timer = new PeriodicTimer(RefreshInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await timer.WaitForNextTickAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            await RefreshAvailabilityAsync(stoppingToken);
        }
    }

    private async Task RefreshAvailabilityAsync(CancellationToken stoppingToken)
    {
        var response = await _dataGovSgClient.GetCarparkAvailabilityAsync(stoppingToken);
        var fetchedAt = DateTimeOffset.UtcNow;

        if (response is null || response.Items.Count == 0)
        {
            _snapshot = _snapshot with
            {
                LastFetchedAt = fetchedAt,
                ErrorMessage = "Failed to retrieve data from data.gov.sg"
            };
            return;
        }

        var item = response.Items[0];
        var availability = new Dictionary<string, CarparkLiveData>(StringComparer.OrdinalIgnoreCase);

        foreach (var cp in item.CarparkData)
        {
            if (string.IsNullOrWhiteSpace(cp.CarparkNumber)) continue;

            var lots = cp.CarparkInfo
                .Select(l => new LotAvailability(
                    LotType: l.LotType,
                    TotalLots: SafeParseInt(l.TotalLots),
                    LotsAvailable: SafeParseInt(l.LotsAvailable)))
                .ToList();

            DateTimeOffset? updatedAt = DateTimeOffset.TryParse(cp.UpdateDatetime, out var dt) ? dt : null;

            availability[cp.CarparkNumber] = new CarparkLiveData(
                CarparkNo: cp.CarparkNumber,
                UpdatedAt: updatedAt,
                Freshness: DetermineFreshness(updatedAt, fetchedAt),
                Lots: lots);
        }

        _snapshot = new CarparkSnapshot(
            Availability: availability,
            DataTimestamp: item.Timestamp,
            LastFetchedAt: fetchedAt,
            ErrorMessage: null);

        _logger.LogInformation("Refreshed carpark availability: {Count} records at {Timestamp}",
            availability.Count, item.Timestamp);
    }

    public CarparkSnapshot GetSnapshot() => _snapshot;

    public CarparkLiveData? GetAvailability(string carparkNo) =>
        _snapshot.Availability.TryGetValue(carparkNo, out var avail) ? avail : null;

    public DataFreshness GetOverallFreshness()
    {
        var snap = _snapshot;
        if (snap.LastFetchedAt is null) return DataFreshness.Unavailable;
        if (snap.ErrorMessage is not null) return DataFreshness.Unavailable;
        var age = DateTimeOffset.UtcNow - snap.LastFetchedAt.Value;
        return age > StaleThreshold ? DataFreshness.Stale : DataFreshness.Fresh;
    }

    private static DataFreshness DetermineFreshness(DateTimeOffset? updatedAt, DateTimeOffset fetchedAt)
    {
        if (updatedAt is null) return DataFreshness.Unavailable;
        var age = fetchedAt - updatedAt.Value;
        return age > StaleThreshold ? DataFreshness.Stale : DataFreshness.Fresh;
    }

    private static int SafeParseInt(string s) =>
        int.TryParse(s, out int v) ? v : 0;
}

public sealed record CarparkSnapshot(
    IReadOnlyDictionary<string, CarparkLiveData> Availability,
    DateTimeOffset? DataTimestamp,
    DateTimeOffset? LastFetchedAt,
    string? ErrorMessage)
{
    public static readonly CarparkSnapshot Empty = new(
        new Dictionary<string, CarparkLiveData>(StringComparer.OrdinalIgnoreCase),
        null, null, null);
}
