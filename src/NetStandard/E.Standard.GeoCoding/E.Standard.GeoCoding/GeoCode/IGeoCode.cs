namespace E.Standard.GeoCoding.GeoCode;

public interface IGeoCode
{
    string Encode(double lon, double lat, int precesion);

    string Encode(GeoLocation geoLocation, int precision);

    GeoLocation Decode(string geoCode);

    bool IsValidGeoCode(string geoCode);

    string DisplayName { get; }

    string[] Links { get; }

    string[] Examples { get; }

    string Description(string language = "en");
}
