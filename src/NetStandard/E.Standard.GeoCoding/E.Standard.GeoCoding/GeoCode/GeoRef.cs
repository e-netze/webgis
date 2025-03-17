using System;

namespace E.Standard.GeoCoding.GeoCode;

public class GeoRef : IGeoCode
{
    private const string zones = "ABCDEFGHJKLMNPQRSTUVWXYZ";      //24 zones

    #region IGeoCode

    // Voraussetzung: Code geht bis zu Minute

    public GeoLocation Decode(string geoCode)
    {
        try
        {
            // 15 degree quad

            int indexLon = zones.IndexOf(geoCode[0]);
            int lon = indexLon * 15 - 180;

            int indexLat = zones.IndexOf(geoCode[1]);
            int lat = indexLat * 15 - 90;

            // 1 degree quad

            int lon1, lat1;

            if (geoCode.Length > 2)
            {
                int indexLon1 = zones.IndexOf(geoCode[2]);
                lon1 = lon + indexLon1;

                int indexLat1 = zones.IndexOf(geoCode[3]);
                lat1 = lat + indexLat1;

            }
            else
            {
                //result for 15 degree quad 

                GeoLocation result = new GeoLocation();

                // center
                result.Longitude = lon + 15 / 2.0;
                result.Latitude = lat + 15 / 2.0;

                // south west 
                GeoLocation SW = new GeoLocation();
                SW.Longitude = lon;
                SW.Latitude = lat;
                result.SouthWest = SW;

                // north east 
                GeoLocation NE = new GeoLocation();
                NE.Longitude = lon + 15;
                NE.Latitude = lat + 15;
                result.NorthEast = NE;

                return result;
            }


            // 1 minute

            int minuteLength = (geoCode.Length - 4) / 2;

            if (geoCode.Length > 4)
            {

                double moveDecimalPoint = 1;

                if (geoCode.Length > 6)
                {
                    moveDecimalPoint = Math.Pow(10, minuteLength - 2);
                }

                double DeltaLon = Convert.ToDouble(geoCode.Substring(4, minuteLength)) / moveDecimalPoint / 60.0;
                double DeltaLat = Convert.ToDouble(geoCode.Substring(4 + minuteLength, minuteLength)) / moveDecimalPoint / 60.0;

                double lon2 = lon1 + DeltaLon;
                double lat2 = lat1 + DeltaLat;


                GeoLocation result = new GeoLocation();

                // center
                result.Longitude = lon2 + 1 / Math.Pow(10, minuteLength - 2) / 60.0 / 2.0;
                result.Latitude = lat2 + 1 / Math.Pow(10, minuteLength - 2) / 60.0 / 2.0;

                // south west 
                GeoLocation SW = new GeoLocation();
                SW.Longitude = lon2;
                SW.Latitude = lat2;
                result.SouthWest = SW;

                // north east 
                GeoLocation NE = new GeoLocation();
                NE.Longitude = lon2 + 1 / Math.Pow(10, minuteLength - 2) / 60.0;
                NE.Latitude = lat2 + 1 / Math.Pow(10, minuteLength - 2) / 60.0;
                result.NorthEast = NE;

                return result;

            }
            else
            {
                //result for 1 degree quad 

                GeoLocation result = new GeoLocation();

                // center
                result.Longitude = lon1 + 1 / 2.0;
                result.Latitude = lat1 + 1 / 2.0;

                // south west 
                GeoLocation SW = new GeoLocation();
                SW.Longitude = lon1;
                SW.Latitude = lat1;
                result.SouthWest = SW;

                // north east 
                GeoLocation NE = new GeoLocation();
                NE.Longitude = lon1 + 1;
                NE.Latitude = lat1 + 1;
                result.NorthEast = NE;

                return result;
            }
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

        try
        {
            string result;

            lon = (lon + 180.0) % 360;
            lat += 90.0;

            // 15 degree quad
            int indexLon1 = (int)Math.Floor(lon / 15.0);
            int indexLat1 = (int)Math.Floor(lat / 15.0);

            result = zones[indexLon1].ToString() + zones[indexLat1].ToString();

            // 1 degree quad
            if (precision > 0)
            {
                double lon1 = lon - indexLon1 * 15.0;
                double lat1 = lat - indexLat1 * 15.0;

                int indexLon2 = (int)Math.Floor(lon1 / 1.0);
                int indexLat2 = (int)Math.Floor(lat1 / 1.0);

                result += zones[indexLon2].ToString() + zones[indexLat2].ToString();
            }

            // 1 minute 
            if (precision > 1)
            {
                int minuteLength = precision;
                result += ((int)Math.Floor((lon - (int)Math.Floor(lon)) * 60 * Math.Pow(10, minuteLength - 2))).ToString().PadRight(minuteLength, '0');       // 1 minute easting
                result += ((int)Math.Floor((lat - (int)Math.Floor(lat)) * 60 * Math.Pow(10, minuteLength - 2))).ToString().PadRight(minuteLength, '0');       // 1 minute northing
            }
            return result;
        }

        catch (Exception ex)
        {
            return ex.Message;
        }
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
        //gerade anzahl
        if (geoCode.Length % 2 == 1)
        {
            return false;
        }

        //  min size
        if (geoCode.Length < 2)
        {
            return false;
        }

        // ersten 4 Buchstaben von Zones
        // dann 4 zahlen
        int count = 0;
        foreach (char c in geoCode)
        {
            if (count < 4)
            {
                if (!Char.IsLetter(c))
                {
                    return false;
                }
            }
            else
            {
                if (!Char.IsDigit(c))
                {
                    return false;
                }
            }
            count++;
        }

        // only characters from zones
        string validChar = "ABCDEFGHJKLMNPQRSTUVWXYZ1234567890";
        foreach (char c in geoCode)
        {
            if (!validChar.Contains(c.ToString()) && c != '+')
            {
                // Console.WriteLine("invalid char in GeoRefCode: " + c);
                return false;
            }
        }
        return Decode(geoCode).IsValid;         // check if phi lam values are possible
    }


    public string DisplayName => "GeoRefCode";

    public string[] Links => new string[] { "https://en.wikipedia.org/wiki/World_Geographic_Reference_System" };

    public string[] Examples => new string[] { "PKAC26100427" };

    public string Description(string language)
    {
        if (language == "de")
        {
            return "GEOREF Code (World Geographic Reference System) ist ein gitterbasierter Geocode, welcher auf geographische Breiten- und Längengrade aufbaut.";
        }
        else
        {
            return "The World Geographic Reference System (GEOREF) is a geocode, a grid-based method of specifying locations on the surface of the Earth. GEOREF is essentially based on the geographic system of latitude and longitude flexible notation.";
        }
    }


    #endregion

}
