using System.Reflection;
using CarparkAvailability.ApiApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarparkAvailability.Tests;

public class CsvDataServiceTests
{
    private static CsvDataService CreateService(string csvPath)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StaticData:CsvPath"] = csvPath
            })
            .Build();
        return new CsvDataService(config, NullLogger<CsvDataService>.Instance);
    }

    [Fact]
    public void Load_MissingFile_ReturnsEmptyDictionary()
    {
        var svc = CreateService("/tmp/nonexistent.csv");
        var result = svc.Load();
        Assert.Empty(result);
    }

    [Fact]
    public void Load_ValidCsvRow_ParsesCorrectly()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path,
            "car_park_no,address,x_coord,y_coord,car_park_type,type_of_parking_system,short_term_parking,free_parking,night_parking,car_park_decks,gantry_height,car_park_basement\n" +
            "ACB,BLK 270 ALBERT CENTRE,30314.7936,31490.4942,BASEMENT CAR PARK,ELECTRONIC PARKING,WHOLE DAY,NO,YES,1,1.8,Y\n");
        try
        {
            var svc = CreateService(path);
            var data = svc.Load();

            Assert.Single(data);
            Assert.True(data.ContainsKey("ACB"));
            var info = data["ACB"];
            Assert.Equal("ACB", info.CarparkNo);
            Assert.Equal("BLK 270 ALBERT CENTRE", info.Address);
            Assert.True(info.CarparkBasement);
            Assert.Equal(1, info.CarparkDecks);
            Assert.Equal(1.8, info.GantryHeight);
            // Coordinates should be within Singapore bounds
            Assert.InRange(info.Latitude, 1.15, 1.48);
            Assert.InRange(info.Longitude, 103.6, 104.1);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_MissingRequiredHeader_ThrowsInvalidOperation()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "car_park_no,address\nACB,TEST\n");
        try
        {
            var svc = CreateService(path);
            Assert.Throws<InvalidOperationException>(() => svc.Load());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_CaseInsensitiveLookup_FindsCarpark()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path,
            "car_park_no,address,x_coord,y_coord,car_park_type,type_of_parking_system,short_term_parking,free_parking,night_parking,car_park_decks,gantry_height,car_park_basement\n" +
            "ACB,BLK 270 ALBERT CENTRE,30314.7936,31490.4942,BASEMENT CAR PARK,ELECTRONIC PARKING,WHOLE DAY,NO,YES,1,1.8,Y\n");
        try
        {
            var svc = CreateService(path);
            var data = svc.Load();
            Assert.True(data.ContainsKey("acb"));
            Assert.True(data.ContainsKey("ACB"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Load_RealCsvFile_LoadsThousandsOfRecords()
    {
        // Use the bundled CSV if available
        string csvPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "Data", "HDBCarparkInformation.csv");

        if (!File.Exists(csvPath))
            return; // Skip if not present in test output

        var svc = CreateService(csvPath);
        var data = svc.Load();
        Assert.True(data.Count > 2000, $"Expected > 2000 records, got {data.Count}");
    }
}
