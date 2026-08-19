using System.Net.Http.Headers;
using System.Text.Json;
using CarparkAvailability.ApiApp.Models;

namespace CarparkAvailability.ApiApp.Services;

public sealed class DataGovSgClient(HttpClient httpClient, IConfiguration configuration, ILogger<DataGovSgClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private string? ApiKey
    {
        get
        {
            string? key = configuration["DataGovSg:ApiKey"];
            return !string.IsNullOrWhiteSpace(key) && !key.StartsWith("{{", StringComparison.Ordinal)
                ? key : null;
        }
    }

    public async Task<DataGovSgResponse?> GetCarparkAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "transport/carpark-availability");
            string? apiKey = ApiKey;
            if (apiKey is not null)
                request.Headers.Add("x-api-key", apiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<DataGovSgResponse>(content, JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch carpark availability from data.gov.sg");
            return null;
        }
    }
}
