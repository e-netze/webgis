namespace E.Standard.Maths.Primitives;

public class GeoLocation
{
    public GeoLocation(double longitude, double latitude)
    {
        this.Longitude = longitude;
        this.Latitude = latitude;
    }

    public readonly double Longitude;
    public readonly double Latitude;
}
