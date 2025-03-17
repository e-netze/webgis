using System;
using System.Collections.Generic;

namespace E.Standard.GeoCoding.GeoCode;

public class GeoHash : IGeoCode
{

    // vgl http://geohash.co/

    private const string BASE32 = "0123456789bcdefghjkmnpqrstuvwxyz";
    IList<int> BITS = new List<int>() { 16, 8, 4, 2, 1 };

    #region IGeoCode

    private void refine_interval(ref double interval0, ref double interval1, int cd, int mask)
    {
        if (Convert.ToBoolean(cd & mask))
        {
            interval0 = (interval0 + interval1) / 2;
        }
        else
        {
            interval1 = (interval0 + interval1) / 2;
        }
    }

    public GeoLocation Decode(string geoCode)
    {

        try
        {
            bool is_even = true;
            double lat0 = -90.0;
            double lat1 = 90.0;
            double lon0 = -180.0;
            double lon1 = 180.0;
            double lat_err = 90.0;
            double lon_err = 180.0;

            for (int i = 0; i < geoCode.Length; i++)
            {
                char c = geoCode[i];
                int cd = BASE32.IndexOf(c);
                for (int j = 0; j < 5; j++)
                {
                    int mask = BITS[j];
                    if (is_even)
                    {
                        lon_err /= 2;
                        refine_interval(ref lon0, ref lon1, cd, mask);
                    }
                    else
                    {
                        lat_err /= 2;
                        refine_interval(ref lat0, ref lat1, cd, mask);
                    }
                    is_even = !is_even;
                }
            }
            double lat2 = (lat0 + lat1) / 2;
            double lon2 = (lon0 + lon1) / 2;

            GeoLocation result = new GeoLocation();

            // center
            result.Longitude = lon2;
            result.Latitude = lat2;

            // south west 
            GeoLocation SW = new GeoLocation();
            SW.Longitude = lon0;
            SW.Latitude = lat0;
            result.SouthWest = SW;

            // north east 
            GeoLocation NE = new GeoLocation();
            NE.Longitude = lon1;
            NE.Latitude = lat1;
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
        if (precision < 0)
        {
            Console.WriteLine("precision value (" + precision + ") not in [0,4]");
            return "invalid precision input - could not create GeoRef Code";
        }

        try
        {
            bool is_even = true;
            int bit = 0;
            var ch = 0;
            string geohash = "";

            double lat0 = -90.0;
            double lat1 = 90.0;
            double lon0 = -180.0;
            double lon1 = 180.0;

            while (geohash.Length < precision)
            {
                if (is_even)
                {
                    double mid = (lon0 + lon1) / 2.0;
                    if (lon > mid)
                    {
                        ch |= BITS[bit];
                        lon0 = mid;
                    }
                    else
                    {
                        lon1 = mid;
                    }
                }
                else
                {
                    double mid = (lat0 + lat1) / 2;
                    if (lat > mid)
                    {
                        ch |= BITS[bit];
                        lat0 = mid;
                    }
                    else
                    {
                        lat1 = mid;
                    }
                }

                is_even = !is_even;
                if (bit < 4)
                {
                    bit++;
                }
                else
                {
                    geohash += BASE32[ch];
                    bit = 0;
                    ch = 0;
                }
            }
            return geohash;
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
        // only characters from BASE32
        foreach (char c in geoCode)
        {
            if (!BASE32.Contains(c.ToString()) && c != '+')
            {
                // Console.WriteLine("invalid char in GeoHashCode: " + c);
                return false;
            }
        }
        return Decode(geoCode).IsValid;         // check if phi lam values are possible
    }


    public string DisplayName => "GeoHashCode";

    public string[] Links => new string[] { "https://en.wikipedia.org/wiki/Geohash" };

    public string[] Examples => new string[] { "u26gz1p3x069" };

    public string Description(string language)
    {
        if (language == "de")
        {
            return "GeoHash ist ein GeoCode, welcher geographische Breiten- und Längengrade in einem kurzen String aus Buchstaben und Zahlen darstellt.";
        }
        else
        {
            return "Geohash is a public domain geocoding system that encodes a geographic location (LatLong) into a short string of letters and digits (a base32 positive integer number).";
        }
    }

    #endregion
}
