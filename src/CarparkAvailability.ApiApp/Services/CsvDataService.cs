using System.Globalization;
using CarparkAvailability.ApiApp.Models;

namespace CarparkAvailability.ApiApp.Services;

public sealed class CsvDataService
{
    private static readonly string[] RequiredHeaders =
    [
        "car_park_no", "address", "x_coord", "y_coord", "car_park_type",
        "type_of_parking_system", "short_term_parking", "free_parking",
        "night_parking", "car_park_decks", "gantry_height", "car_park_basement"
    ];

    private readonly string _csvPath;
    private readonly ILogger<CsvDataService> _logger;

    public CsvDataService(IConfiguration configuration, ILogger<CsvDataService> logger)
    {
        _csvPath = configuration["StaticData:CsvPath"] ?? "Data/HDBCarparkInformation.csv";
        _logger = logger;
    }

    public IReadOnlyDictionary<string, HdbCarparkInfo> Load()
    {
        if (!File.Exists(_csvPath))
        {
            _logger.LogError("HDB CSV file not found at {Path}", _csvPath);
            return new Dictionary<string, HdbCarparkInfo>(StringComparer.OrdinalIgnoreCase);
        }

        using var reader = new StreamReader(_csvPath);
        string? headerLine = reader.ReadLine();
        if (headerLine is null)
        {
            _logger.LogError("HDB CSV file is empty");
            return new Dictionary<string, HdbCarparkInfo>(StringComparer.OrdinalIgnoreCase);
        }

        string[] headers = headerLine.Split(',');
        ValidateHeaders(headers);
        var colIndex = headers.Select((h, i) => (h.Trim().ToLowerInvariant(), i))
                              .ToDictionary(x => x.Item1, x => x.i);

        var result = new Dictionary<string, HdbCarparkInfo>(StringComparer.OrdinalIgnoreCase);
        int lineNumber = 1;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            var info = ParseRow(line, colIndex, lineNumber);
            if (info is not null)
                result[info.CarparkNo] = info;
        }

        _logger.LogInformation("Loaded {Count} HDB carpark records from CSV", result.Count);
        return result;
    }

    private void ValidateHeaders(string[] headers)
    {
        var normalized = headers.Select(h => h.Trim().ToLowerInvariant()).ToHashSet();
        var missing = RequiredHeaders.Where(r => !normalized.Contains(r)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"HDB CSV is missing required headers: {string.Join(", ", missing)}");
    }

    private HdbCarparkInfo? ParseRow(string line, Dictionary<string, int> colIndex, int lineNumber)
    {
        var cols = line.Split(',');
        try
        {
            string carparkNo = GetCol(cols, colIndex, "car_park_no");
            if (string.IsNullOrWhiteSpace(carparkNo)) return null;

            double x = ParseDouble(GetCol(cols, colIndex, "x_coord"));
            double y = ParseDouble(GetCol(cols, colIndex, "y_coord"));
            var (lat, lon) = Svy21Converter.Convert(x, y);

            return new HdbCarparkInfo(
                CarparkNo: carparkNo,
                Address: GetCol(cols, colIndex, "address"),
                XCoord: x,
                YCoord: y,
                CarparkType: GetCol(cols, colIndex, "car_park_type"),
                TypeOfParkingSystem: GetCol(cols, colIndex, "type_of_parking_system"),
                ShortTermParking: GetCol(cols, colIndex, "short_term_parking"),
                FreeParking: GetCol(cols, colIndex, "free_parking"),
                NightParking: GetCol(cols, colIndex, "night_parking"),
                CarparkDecks: ParseInt(GetCol(cols, colIndex, "car_park_decks")),
                GantryHeight: ParseDouble(GetCol(cols, colIndex, "gantry_height")),
                CarparkBasement: GetCol(cols, colIndex, "car_park_basement").Equals("Y", StringComparison.OrdinalIgnoreCase),
                Latitude: lat,
                Longitude: lon);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Skipping invalid row at line {Line}", lineNumber);
            return null;
        }
    }

    private static string GetCol(string[] cols, Dictionary<string, int> colIndex, string name) =>
        colIndex.TryGetValue(name, out int i) && i < cols.Length ? cols[i].Trim() : string.Empty;

    private static double ParseDouble(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0.0;

    private static int ParseInt(string s) =>
        int.TryParse(s, out int v) ? v : 0;
}
