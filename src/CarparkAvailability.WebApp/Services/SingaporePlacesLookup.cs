namespace CarparkAvailability.WebApp.Services;

/// <summary>
/// Simple lookup for common Singapore landmarks and town centres.
/// Provides basic geocoding without requiring an external API key.
/// </summary>
public static class SingaporePlacesLookup
{
    private static readonly Dictionary<string, (double Lat, double Lng)> KnownPlaces =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["tampines"] = (1.3530, 103.9450),
        ["bugis"] = (1.3005, 103.8557),
        ["toa payoh"] = (1.3321, 103.8470),
        ["jurong east"] = (1.3330, 103.7420),
        ["clementi"] = (1.3152, 103.7650),
        ["bedok"] = (1.3240, 103.9300),
        ["woodlands"] = (1.4370, 103.7865),
        ["ang mo kio"] = (1.3700, 103.8490),
        ["bishan"] = (1.3510, 103.8490),
        ["choa chu kang"] = (1.3840, 103.7470),
        ["hougang"] = (1.3710, 103.8930),
        ["serangoon"] = (1.3505, 103.8690),
        ["sengkang"] = (1.3915, 103.8952),
        ["punggol"] = (1.3985, 103.9091),
        ["pasir ris"] = (1.3731, 104.0197),
        ["bukit timah"] = (1.3295, 103.8038),
        ["queenstown"] = (1.2945, 103.8055),
        ["marine parade"] = (1.3020, 103.8945),
        ["geylang"] = (1.3185, 103.8840),
        ["kallang"] = (1.3112, 103.8707),
        ["novena"] = (1.3200, 103.8435),
        ["orchard"] = (1.3046, 103.8318),
        ["city hall"] = (1.2929, 103.8520),
        ["raffles place"] = (1.2841, 103.8511),
        ["marina bay"] = (1.2807, 103.8596),
        ["harbourfront"] = (1.2660, 103.8206),
        ["tanjong pagar"] = (1.2761, 103.8453),
        ["chinatown"] = (1.2831, 103.8448),
        ["little india"] = (1.3067, 103.8520),
        ["dhoby ghaut"] = (1.2991, 103.8454),
        ["somerset"] = (1.3005, 103.8390),
        ["dover"] = (1.3118, 103.7783),
        ["buona vista"] = (1.3073, 103.7907),
        ["kent ridge"] = (1.2942, 103.7847),
        ["one-north"] = (1.2993, 103.7872),
        ["science park"] = (1.2939, 103.7986),
        ["boon lay"] = (1.3393, 103.7059),
        ["pioneer"] = (1.3373, 103.6975),
        ["tuas"] = (1.3221, 103.6460),
    };

    /// <summary>
    /// Attempts to resolve a query to Singapore coordinates.
    /// Returns null if the query is not recognized.
    /// </summary>
    public static (double Lat, double Lng)? TryResolve(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        // Exact match
        if (KnownPlaces.TryGetValue(query.Trim(), out var coords))
            return coords;

        // Partial match (query contains or is contained by a known place)
        string lower = query.Trim().ToLowerInvariant();
        foreach (var (place, location) in KnownPlaces)
        {
            if (lower.Contains(place.ToLowerInvariant()) || place.ToLowerInvariant().Contains(lower))
                return location;
        }

        return null;
    }
}
