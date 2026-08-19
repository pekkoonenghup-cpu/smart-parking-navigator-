namespace CarparkAvailability.ApiApp.Models;

public enum DataFreshness { Fresh, Stale, Unavailable }

public sealed record LotAvailability(
    string LotType,
    int TotalLots,
    int LotsAvailable);

public sealed record CarparkLiveData(
    string CarparkNo,
    DateTimeOffset? UpdatedAt,
    DataFreshness Freshness,
    IReadOnlyList<LotAvailability> Lots);

public sealed record CarparkSummary(
    string CarparkNo,
    string Address,
    string CarparkType,
    string TypeOfParkingSystem,
    double Latitude,
    double Longitude,
    double DistanceMetres,
    DataFreshness Freshness,
    DateTimeOffset? AvailabilityUpdatedAt,
    int TotalCarLots,
    int AvailableCarLots);

public sealed record CarparkDetail(
    string CarparkNo,
    string Address,
    string CarparkType,
    string TypeOfParkingSystem,
    string ShortTermParking,
    string FreeParking,
    string NightParking,
    int CarparkDecks,
    double GantryHeight,
    bool CarparkBasement,
    double Latitude,
    double Longitude,
    DataFreshness Freshness,
    DateTimeOffset? AvailabilityUpdatedAt,
    IReadOnlyList<LotAvailability> Lots);

public sealed record DataStatusResponse(
    DataFreshness Freshness,
    DateTimeOffset? LastFetchedAt,
    DateTimeOffset? DataTimestamp,
    string? ErrorMessage);
