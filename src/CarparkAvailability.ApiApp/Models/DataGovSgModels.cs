using System.Text.Json.Serialization;

namespace CarparkAvailability.ApiApp.Models;

public sealed class DataGovSgResponse
{
    [JsonPropertyName("items")]
    public List<DataGovSgItem> Items { get; set; } = [];
}

public sealed class DataGovSgItem
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("carpark_data")]
    public List<CarparkData> CarparkData { get; set; } = [];
}

public sealed class CarparkData
{
    [JsonPropertyName("carpark_number")]
    public string CarparkNumber { get; set; } = string.Empty;

    [JsonPropertyName("update_datetime")]
    public string UpdateDatetime { get; set; } = string.Empty;

    [JsonPropertyName("carpark_info")]
    public List<CarparkLotInfo> CarparkInfo { get; set; } = [];
}

public sealed class CarparkLotInfo
{
    [JsonPropertyName("lot_type")]
    public string LotType { get; set; } = string.Empty;

    [JsonPropertyName("total_lots")]
    public string TotalLots { get; set; } = "0";

    [JsonPropertyName("lots_available")]
    public string LotsAvailable { get; set; } = "0";
}
