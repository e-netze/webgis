using Google.OpenLocationCode;
using System;

namespace E.Standard.GeoCoding.GeoCode;

public class OpenLocation : IGeoCode
{
    private const string codeDigits = "23456789CFGHJMPQRVWX";

    #region IGeoCode

    public GeoLocation Decode(string geoCode)
    {

        try
        {
            CodeArea codeArea = OpenLocationCode.Decode(geoCode);

            GeoPoint areaMin = codeArea.Min;
            GeoPoint areaMax = codeArea.Max;
            GeoPoint areaCenter = codeArea.Center;

            GeoLocation result = new GeoLocation();

            // center
            result.Longitude = areaCenter.Longitude;
            result.Latitude = areaCenter.Latitude;

            // south west 
            GeoLocation SW = new GeoLocation();
            SW.Longitude = areaMin.Longitude;
            SW.Latitude = areaMin.Latitude;
            result.SouthWest = SW;

            // north east 
            GeoLocation NE = new GeoLocation();
            NE.Longitude = areaMax.Longitude;
            NE.Latitude = areaMax.Latitude;
            result.NorthEast = NE;

            return result;

        }
        catch (Exception ex)
        {
            return new GeoLocation() { ErrorMessage = ex.Message };
        }

    }

    public string Encode(double lon, double lat, int precision)
    {
        //check if input is valid
        if (lat > 90 || lat < -90)
        {
            Console.WriteLine("lat value (" + lat + ") not in [-90,90]");
            return "invalid lat input - could not create GeoRef Code";
        }
        if (lon > 180 || lon < -180)
        {
            Console.WriteLine("lon value (" + lon + ") not in [-180,180]");
            return "invalid lon input - could not create GeoRef Code";
        }
        if (precision < 0 || precision > 4)
        {
            Console.WriteLine("precision value (" + precision + ") not in [0,4]");
            return "invalid precision input - could not create GeoRef Code";
        }
        return OpenLocationCode.Encode(lat, lon);
    }

    public string Encode(GeoLocation geoLocation, int precision)
    {
        double lon = geoLocation.Longitude;
        double lat = geoLocation.Latitude;

        return OpenLocationCode.Encode(lon, lat);
    }

    public bool IsValidGeoCode(string geoCode)
    {
        return OpenLocationCode.IsValid(geoCode) && OpenLocationCode.IsFull(geoCode) && Decode(geoCode).IsValid; ;
    }

    public string DisplayName => "OpenLocationCode";

    public string[] Links => new string[] { "https://plus.codes/howitworks" };

    public string[] Examples => new string[] { "8FVQ3CCP+F2" };

    public string Description(string language)
    {
        if (language == "de")
        {
            return "Mit Open Location Code, auch Plus Code genannt, wird ein Raster, basierend auf Breiten- und Längengraden, über den ganzen Planeten beschrieben.";
        }
        else
        {
            return "Open Location Codes, also known as Plus codes, are based on latitude and longitude – the grid that can be used to describe every point on the planet. By using a simpler code system, they end up much shorter and easier to use than traditional global coordinates.";
        }
    }

    #endregion
}
