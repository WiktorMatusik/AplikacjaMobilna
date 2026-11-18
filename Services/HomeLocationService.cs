namespace AplikacjaMobilna.Services;

public static class HomeLocationService
{
    public static double CalculateDistanceMeters(Location current, Location home)
    {
        return Location.CalculateDistance(current, home, DistanceUnits.Kilometers) * 1000.0;
    }

    public static double CalculateBearing(Location start, Location end)
    {
        double lat1 = DegreesToRadians(start.Latitude);
        double lon1 = DegreesToRadians(start.Longitude);
        double lat2 = DegreesToRadians(end.Latitude);
        double lon2 = DegreesToRadians(end.Longitude);

        double dLon = lon2 - lon1;

        double y = Math.Sin(dLon) * Math.Cos(lat2);
        double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

        double brng = Math.Atan2(y, x);
        return (RadiansToDegrees(brng) + 360) % 360;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
    private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;
}