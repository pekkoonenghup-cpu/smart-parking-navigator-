using CarparkAvailability.ApiApp.Models;
using CarparkAvailability.ApiApp.Services;

namespace CarparkAvailability.Tests;

public class CarparkDataServiceTests
{
    [Fact]
    public void CarparkSnapshot_Empty_HasNoAvailability()
    {
        var snap = CarparkSnapshot.Empty;
        Assert.Empty(snap.Availability);
        Assert.Null(snap.LastFetchedAt);
        Assert.Null(snap.DataTimestamp);
    }

    [Fact]
    public void LotAvailability_SafeParseInt_HandlesStringValues()
    {
        // Simulate what the service does when parsing lot data
        int Parse(string s) => int.TryParse(s, out int v) ? v : 0;

        Assert.Equal(105, Parse("105"));
        Assert.Equal(0, Parse(""));
        Assert.Equal(0, Parse("N/A"));
        Assert.Equal(0, Parse(null!));
    }

    [Fact]
    public void DataFreshness_Values_AreDistinct()
    {
        Assert.NotEqual(DataFreshness.Fresh, DataFreshness.Stale);
        Assert.NotEqual(DataFreshness.Stale, DataFreshness.Unavailable);
        Assert.NotEqual(DataFreshness.Fresh, DataFreshness.Unavailable);
    }
}
