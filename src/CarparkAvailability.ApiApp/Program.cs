using CarparkAvailability.ApiApp.Models;
using CarparkAvailability.ApiApp.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Register services
builder.Services.AddSingleton<CsvDataService>();
builder.Services.AddHttpClient<DataGovSgClient>(client =>
{
    string baseUrl = builder.Configuration["DataGovSg:BaseUrl"] ?? "https://api.data.gov.sg/v1/";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddSingleton<CarparkDataService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<CarparkDataService>());

WebApplication app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CORS for WebApp
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

var api = app.MapGroup("/api");

api.MapGet("/", () => Results.Ok(new { name = "Smart Parking Navigator API", version = "1.0" }));

// GET /api/carparks/status
api.MapGet("/carparks/status", (CarparkDataService svc) =>
{
    var snap = svc.GetSnapshot();
    return Results.Ok(new DataStatusResponse(
        Freshness: svc.GetOverallFreshness(),
        LastFetchedAt: snap.LastFetchedAt,
        DataTimestamp: snap.DataTimestamp,
        ErrorMessage: snap.ErrorMessage));
});

// GET /api/carparks/search?lat=&lng=&radius=500&availableOnly=false&freeParking=false&nightParking=false&vehicleType=C
api.MapGet("/carparks/search", (
    CarparkDataService svc,
    double lat,
    double lng,
    double radius = 500,
    bool availableOnly = false,
    bool? freeParking = null,
    bool? nightParking = null,
    string? vehicleType = null) =>
{
    if (lat < 1.1 || lat > 1.5 || lng < 103.5 || lng > 104.1)
        return Results.Problem("Coordinates must be within Singapore bounds.", statusCode: 400);

    if (radius <= 0 || radius > 5000)
        return Results.Problem("Radius must be between 1 and 5000 metres.", statusCode: 400);

    var staticData = svc.StaticData;
    var snap = svc.GetSnapshot();

    var results = new List<CarparkSummary>();

    foreach (var (_, info) in staticData)
    {
        double dist = HaversineMetres(lat, lng, info.Latitude, info.Longitude);
        if (dist > radius) continue;

        // Apply filters
        if (freeParking == true && info.FreeParking.Equals("NO", StringComparison.OrdinalIgnoreCase))
            continue;
        if (nightParking == true && info.NightParking.Equals("NO", StringComparison.OrdinalIgnoreCase))
            continue;

        var avail = svc.GetAvailability(info.CarparkNo);
        var lots = avail?.Lots ?? [];

        var carLots = vehicleType is not null
            ? lots.Where(l => l.LotType.Equals(vehicleType, StringComparison.OrdinalIgnoreCase)).ToList()
            : lots.Where(l => l.LotType == "C").ToList();

        int totalCar = carLots.Sum(l => l.TotalLots);
        int availCar = carLots.Sum(l => l.LotsAvailable);

        if (availableOnly && availCar <= 0) continue;

        results.Add(new CarparkSummary(
            CarparkNo: info.CarparkNo,
            Address: info.Address,
            CarparkType: info.CarparkType,
            TypeOfParkingSystem: info.TypeOfParkingSystem,
            Latitude: info.Latitude,
            Longitude: info.Longitude,
            DistanceMetres: Math.Round(dist, 1),
            Freshness: avail?.Freshness ?? DataFreshness.Unavailable,
            AvailabilityUpdatedAt: avail?.UpdatedAt,
            TotalCarLots: totalCar,
            AvailableCarLots: availCar));
    }

    results.Sort((a, b) => a.DistanceMetres.CompareTo(b.DistanceMetres));
    return Results.Ok(results);
});

// GET /api/carparks/{id}
api.MapGet("/carparks/{id}", (string id, CarparkDataService svc) =>
{
    if (!svc.StaticData.TryGetValue(id, out var info))
        return Results.NotFound(new { message = $"Carpark '{id}' not found." });

    var avail = svc.GetAvailability(id);

    return Results.Ok(new CarparkDetail(
        CarparkNo: info.CarparkNo,
        Address: info.Address,
        CarparkType: info.CarparkType,
        TypeOfParkingSystem: info.TypeOfParkingSystem,
        ShortTermParking: info.ShortTermParking,
        FreeParking: info.FreeParking,
        NightParking: info.NightParking,
        CarparkDecks: info.CarparkDecks,
        GantryHeight: info.GantryHeight,
        CarparkBasement: info.CarparkBasement,
        Latitude: info.Latitude,
        Longitude: info.Longitude,
        Freshness: avail?.Freshness ?? DataFreshness.Unavailable,
        AvailabilityUpdatedAt: avail?.UpdatedAt,
        Lots: avail?.Lots ?? []));
});

app.MapDefaultEndpoints();

app.Run();

static double HaversineMetres(double lat1, double lng1, double lat2, double lng2)
{
    const double R = 6371000;
    double dLat = (lat2 - lat1) * Math.PI / 180.0;
    double dLng = (lng2 - lng1) * Math.PI / 180.0;
    double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
               Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
               Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
    return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
}

public partial class Program { }

