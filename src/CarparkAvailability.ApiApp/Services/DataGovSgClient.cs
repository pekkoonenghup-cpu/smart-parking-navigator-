using System.Net.Http.Headers;
using System.Text.Json;
using CarparkAvailability.ApiApp.Models;

namespace CarparkAvailability.ApiApp.Services;

public sealed class DataGovSgClient(HttpClient httpClient, IConfiguration configuration, ILogger<DataGovSgClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<DataGovSgResponse?> GetCarparkAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        string? apiKey = configuration["DataGovSg:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey) &&
            !apiKey.StartsWith("{{", StringComparison.Ordinal))
        {
            httpClient.DefaultRequestHeaders.Remove("x-api-key");
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }

        try
        {
            using var response = await httpClient.GetAsync("transport/carpark-availability", cancellationToken);
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
