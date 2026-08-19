using CarparkAvailability.ApiApp.Services;

namespace CarparkAvailability.Tests;

public class Svy21ConverterTests
{
    // Albert Centre is near Bugis, expected ~lat 1.302, lng 103.857
    [Theory]
    [InlineData(30314.7936, 31490.4942, 1.30, 103.84, 1.32, 103.87)]
    // Tampines area should be roughly lat 1.35, lng 103.94
    [InlineData(42416.6565, 38176.3753, 1.33, 103.92, 1.37, 103.97)]
    public void Convert_KnownSingaporePoints_ReturnsReasonableCoordinates(
        double x, double y,
        double minLat, double minLng, double maxLat, double maxLng)
    {
        var (lat, lng) = Svy21Converter.Convert(x, y);

        Assert.InRange(lat, minLat, maxLat);
        Assert.InRange(lng, minLng, maxLng);
    }

    [Fact]
    public void Convert_SingaporeOriginApproximation_IsInSingapore()
    {
        // Singapore is between lat 1.15–1.48, lng 103.6–104.1
        var (lat, lng) = Svy21Converter.Convert(28001.642, 38744.572);
        Assert.InRange(lat, 1.15, 1.48);
        Assert.InRange(lng, 103.6, 104.1);
    }

    [Fact]
    public void Convert_LatitudeIsPositive()
    {
        var (lat, _) = Svy21Converter.Convert(30000, 35000);
        Assert.True(lat > 0);
    }
}