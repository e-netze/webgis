using E.Standard.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace E.Standard.GeoCoding.GeoCode;

public class GeographicCoordinates : IGeoCode
{

    public GeoLocation Decode(string geoCode)
    {
        try
        {
            double lon = 0;
            double lat = 0;

            // removes all leading and trailing whitespaces
            geoCode = geoCode.Trim();

            // replace any kind of whitespace (e.g. tabs, newlines, etc.) with a single space
            geoCode = Regex.Replace(geoCode, @"\s+", " ");

            // remove spaces before and after commas
            geoCode = Regex.Replace(geoCode, " *, *", ",");


            // count number of whitespaces and commas
            var count_whitespace = geoCode.Count(x => x == ' ');
            var count_comma = geoCode.Count(x => x == ',');


            if (count_whitespace == 1)
            {
                string[] v = geoCode.Split(' ');
                lat = ParseCoordinateValue(v[0]);
                lon = ParseCoordinateValue(v[1]);
            }
            else if (count_comma == 1)
            {
                string[] v = geoCode.Split(',');
                lat = ParseCoordinateValue(v[0]);
                lon = ParseCoordinateValue(v[1]);
            }

            else if (count_whitespace % 2 == 1 && count_whitespace != 1)
            {
                var foundIndexes = new List<int>();
                for (int i = 0; i < geoCode.Length; i++)
                {
                    if (geoCode[i] == ' ')
                    {
                        foundIndexes.Add(i);
                    }
                }
                int middleIndex = Convert.ToInt32(Math.Floor(count_whitespace / 2.0));
                int splitIndex = foundIndexes[middleIndex];
                lat = ParseCoordinateValue(geoCode.Substring(0, splitIndex));
                lon = ParseCoordinateValue(geoCode.Substring(splitIndex + 1));
            }

            else if (count_comma % 2 == 1 && count_comma != 1)
            {
                var foundIndexes = new List<int>();
                for (int i = 0; i < geoCode.Length; i++)
                {
                    if (geoCode[i] == ',')
                    {
                        foundIndexes.Add(i);
                    }
                }
                int middleIndex = Convert.ToInt32(Math.Floor(count_comma / 2.0));
                int splitIndex = foundIndexes[middleIndex];
                lat = ParseCoordinateValue(geoCode.Substring(0, splitIndex));
                lon = ParseCoordinateValue(geoCode.Substring(splitIndex + 1));
            }
            else
            {
                throw new Exception("Syntax error");
            }


            GeoLocation result = new GeoLocation();

            // center
            result.Longitude = lon;
            result.Latitude = lat;

            // south west 
            GeoLocation SW = new GeoLocation();
            SW.Longitude = lon;
            SW.Latitude = lat;
            result.SouthWest = SW;

            // north east 
            GeoLocation NE = new GeoLocation();
            NE.Longitude = lon;
            NE.Latitude = lat;
            result.NorthEast = NE;

            return result;

        }
        catch (Exception ex)
        {
            return new GeoLocation() { ErrorMessage = ex.Message };
        }
    }

    public string Encode(double lon, double lat, int precesion)
    {
        string result = lon.ToString() + ", " + lat.ToString();
        return result;
    }

    public string Encode(GeoLocation geoLocation, int precision)
    {
        double lon = geoLocation.Longitude;
        double lat = geoLocation.Latitude;

        string result = Encode(lon, lat, precision);

        return result;
    }

    public bool IsValidGeoCode(string geoCode)
    {
        // only characters from 
        string validChar = "1234567890 ,.°g\"'´`";
        foreach (char c in geoCode)
        {
            if (!validChar.Contains(c.ToString()) && c != '+')
            {
                // Console.WriteLine("invalid char in geographic Coordinates: " + c);
                return false;
            }
        }

        return Decode(geoCode).IsValid;     // check if phi lam values are possible
    }

    public string DisplayName => "geographic Coordinates (phi, lambda)";

    public string[] Links => new string[] { "https://www.latlong.net/", "https://en.wikipedia.org/wiki/Geographic_coordinate_system" };

    public string[] Examples => new string[] { "47.078167, 15.439833",
                                               "47.078167 15.439833",
                                               "47,078167 15,439833",
                                               "47,078167, 15,439833",
                                               "47°04,69' 15°26,39'",
                                               "47°04'41,4'' 15°26'23,4''",
                                               "47 04 41,4 15 26 23,4" };

    public string Description(string language)
    {
        if (language == "de")
        {
            return "Die geographischen Koordinaten sind Kugelkoordinaten, mit denen sich die Lage eines Punktes auf der Erdoberfläche mittels geographischer Breite (vom Äquator aus 0° bis 90° nach Norden/Süden -> phi) und geographische Länge (vom Nullmeridian aus von 0° bis 180° gegen Osten/Westen -> lambda) beschreiben lässt.";
        }
        else
        {
            return "Latitude (phi) and Longitude (lambda) are the units that represent the coordinates at geographic coordinate system. Every single point on the surface of earth can be specified by the latitude and longitude coordinates";
        }
    }


    #region Helper

    private double ParseCoordinateValue(string val)
    {
        try
        {
            val = val.Trim().Replace(",", ".");

            #region Sonderzeichen für Grad, Minuten, Sekunden durch Leerzeichen ersetzen und doppelte Leererzeichen entfernen

            foreach (var s in new string[] { "°", "\"", "'", "g", "´", "`" })
            {
                val = val.Replace(s, " ").Trim();
            }

            while (val.Contains("  "))
            {
                val = val.Replace("  ", " ");
            }

            int sign = 1;
            if (val.Contains(":") || val.Split(':').Length == 2)
            {
                if (val.ToLower().StartsWith("w:") || val.ToLower().StartsWith("s:"))   // West/South -> negative Koordinaten
                {
                    sign = -1;
                }
                val = val.Split(':')[1].Trim();
            }

            #endregion

            string[] v = val.Split(' ');

            switch (v.Length)
            {
                case 1:
                    return v[0].ToPlatformDouble() * sign;
                case 2:
                    return (v[0].ToPlatformDouble() + v[1].ToPlatformDouble() / 60D) * sign;
                case 3:
                    return (v[0].ToPlatformDouble() + v[1].ToPlatformDouble() / 60D + v[2].ToPlatformDouble() / 3600D) * sign;
                default:
                    throw new Exception("Koordinatenwert kann nicht ermittelt werden.");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Fehler bei der Eingabe: " + val + "\n" + ex.Message);
        }
    }



    #endregion
}
