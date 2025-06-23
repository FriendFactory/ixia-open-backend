using NetTopologySuite.Geometries;

namespace Frever.Shared.MainDb.Extensions;

public static class Geo
{
    public static Point FromLatLon(decimal lat, decimal lon)
    {
        return new Point((double) lon, (double) lat) {SRID = 4326};
    }
}