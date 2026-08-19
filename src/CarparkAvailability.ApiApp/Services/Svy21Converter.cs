namespace CarparkAvailability.ApiApp.Services;

/// <summary>
/// Converts SVY21 (Singapore projected coordinate system) to WGS84 latitude/longitude
/// using the Transverse Mercator inverse projection (Snyder 1987).
/// SVY21 parameters: origin 1°22'N 103°50'E, k0=1, FE=28001.642m, FN=38744.572m.
/// </summary>
public static class Svy21Converter
{
    // WGS84 ellipsoid
    private const double A = 6378137.0;
    private const double F = 1.0 / 298.257223563;
    private static readonly double E2 = 2 * F - F * F;
    private static readonly double E4 = E2 * E2;
    private static readonly double E6 = E4 * E2;

    // SVY21 projection parameters
    private const double Lat0Deg = 1.366666666667;
    private const double Lon0Deg = 103.833333333333;
    private static readonly double Lat0 = Lat0Deg * Math.PI / 180.0;
    private static readonly double Lon0 = Lon0Deg * Math.PI / 180.0;
    private const double K0 = 1.0;
    private const double E0 = 28001.642; // false easting
    private const double N0 = 38744.572; // false northing

    // Meridional arc from equator to origin latitude
    private static readonly double M0 = MeridionalArc(Lat0);

    private static double MeridionalArc(double lat) =>
        A * ((1 - E2 / 4 - 3 * E4 / 64 - 5 * E6 / 256) * lat
            - (3 * E2 / 8 + 3 * E4 / 32 + 45 * E6 / 1024) * Math.Sin(2 * lat)
            + (15 * E4 / 256 + 45 * E6 / 1024) * Math.Sin(4 * lat)
            - (35 * E6 / 3072) * Math.Sin(6 * lat));

    /// <summary>Converts SVY21 (x=easting, y=northing) to (latitude, longitude) in degrees.</summary>
    public static (double Latitude, double Longitude) Convert(double x, double y)
    {
        double ePrime = x - E0;
        double nPrime = y - N0;
        double m = nPrime / K0 + M0;

        double e1 = (1 - Math.Sqrt(1 - E2)) / (1 + Math.Sqrt(1 - E2));
        double mu = m / (A * (1 - E2 / 4 - 3 * E4 / 64 - 5 * E6 / 256));

        // Footprint latitude
        double phi1 = mu
            + (3 * e1 / 2 - 27 * Math.Pow(e1, 3) / 32) * Math.Sin(2 * mu)
            + (21 * e1 * e1 / 16 - 55 * Math.Pow(e1, 4) / 32) * Math.Sin(4 * mu)
            + (151 * Math.Pow(e1, 3) / 96) * Math.Sin(6 * mu)
            + (1097 * Math.Pow(e1, 4) / 512) * Math.Sin(8 * mu);

        double sinPhi1 = Math.Sin(phi1);
        double cosPhi1 = Math.Cos(phi1);
        double tanPhi1 = Math.Tan(phi1);

        double n1 = A / Math.Sqrt(1 - E2 * sinPhi1 * sinPhi1);
        double r1 = A * (1 - E2) / Math.Pow(1 - E2 * sinPhi1 * sinPhi1, 1.5);
        double t1 = tanPhi1 * tanPhi1;
        double c1 = E2 / (1 - E2) * cosPhi1 * cosPhi1;
        double d = ePrime / (n1 * K0);
        double d2 = d * d;
        double d3 = d * d2;
        double d4 = d * d3;
        double d5 = d * d4;
        double d6 = d * d5;

        double lat = phi1 - (n1 * tanPhi1 / r1) *
            (d2 / 2
            - (5 + 3 * t1 + 10 * c1 - 4 * c1 * c1 - 9 * E2 / (1 - E2)) * d4 / 24
            + (61 + 90 * t1 + 298 * c1 + 45 * t1 * t1 - 252 * E2 / (1 - E2) - 3 * c1 * c1) * d6 / 720);

        double lon = Lon0 + (d
            - (1 + 2 * t1 + c1) * d3 / 6
            + (5 - 2 * c1 + 28 * t1 - 3 * c1 * c1 + 8 * E2 / (1 - E2) + 24 * t1 * t1) * d5 / 120) / cosPhi1;

        return (lat * 180.0 / Math.PI, lon * 180.0 / Math.PI);
    }
}
