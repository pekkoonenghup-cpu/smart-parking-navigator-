using System.Net.Http.Json;
using System.Text;
using CarparkAvailability.WebApp.Models;

namespace CarparkAvailability.WebApp.Services;

public sealed class CarparkApiClient(HttpClient httpClient)
{
    public async Task<List<CarparkSummary>?> SearchAsync(
        double lat, double lng,
        double radius = 500,
        bool availableOnly = false,
        bool? freeParking = null,
        bool? nightParking = null,
        string? vehicleType = null,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder($"api/carparks/search?lat={lat}&lng={lng}&radius={radius}");
        if (availableOnly) sb.Append("&availableOnly=true");
        if (freeParking == true) sb.Append("&freeParking=true");
        if (nightParking == true) sb.Append("&nightParking=true");
        if (!string.IsNullOrWhiteSpace(vehicleType)) sb.Append($"&vehicleType={Uri.EscapeDataString(vehicleType!)}");

        return await httpClient.GetFromJsonAsync<List<CarparkSummary>>(sb.ToString(), cancellationToken);
    }

    public async Task<CarparkDetail?> GetDetailAsync(string carparkNo, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<CarparkDetail>($"api/carparks/{carparkNo}", cancellationToken);

    public async Task<DataStatusResponse?> GetStatusAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<DataStatusResponse>("api/carparks/status", cancellationToken);
}
