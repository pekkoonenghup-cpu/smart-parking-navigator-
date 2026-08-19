using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CarparkAvailability.WebApp.Models;

public enum DataFreshness { Fresh, Stale, Unavailable }

public sealed record LotAvailability(
    [property: JsonPropertyName("lotType")] string LotType,
    [property: JsonPropertyName("totalLots")] int TotalLots,
    [property: JsonPropertyName("lotsAvailable")] int LotsAvailable);

public sealed record CarparkSummary(
    [property: JsonPropertyName("carparkNo")] string CarparkNo,
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("carparkType")] string CarparkType,
    [property: JsonPropertyName("typeOfParkingSystem")] string TypeOfParkingSystem,
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude,
    [property: JsonPropertyName("distanceMetres")] double DistanceMetres,
    [property: JsonPropertyName("freshness")] DataFreshness Freshness,
    [property: JsonPropertyName("availabilityUpdatedAt")] DateTimeOffset? AvailabilityUpdatedAt,
    [property: JsonPropertyName("totalCarLots")] int TotalCarLots,
    [property: JsonPropertyName("availableCarLots")] int AvailableCarLots);

public sealed record CarparkDetail(
    [property: JsonPropertyName("carparkNo")] string CarparkNo,
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("carparkType")] string CarparkType,
    [property: JsonPropertyName("typeOfParkingSystem")] string TypeOfParkingSystem,
    [property: JsonPropertyName("shortTermParking")] string ShortTermParking,
    [property: JsonPropertyName("freeParking")] string FreeParking,
    [property: JsonPropertyName("nightParking")] string NightParking,
    [property: JsonPropertyName("carparkDecks")] int CarparkDecks,
    [property: JsonPropertyName("gantryHeight")] double GantryHeight,
    [property: JsonPropertyName("carparkBasement")] bool CarparkBasement,
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude,
    [property: JsonPropertyName("freshness")] DataFreshness Freshness,
    [property: JsonPropertyName("availabilityUpdatedAt")] DateTimeOffset? AvailabilityUpdatedAt,
    [property: JsonPropertyName("lots")] IReadOnlyList<LotAvailability> Lots);

public sealed record DataStatusResponse(
    [property: JsonPropertyName("freshness")] DataFreshness Freshness,
    [property: JsonPropertyName("lastFetchedAt")] DateTimeOffset? LastFetchedAt,
    [property: JsonPropertyName("dataTimestamp")] DateTimeOffset? DataTimestamp,
    [property: JsonPropertyName("errorMessage")] string? ErrorMessage);
