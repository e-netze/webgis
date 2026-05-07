using System;

namespace E.Standard.GeoCoding.GeoCode;

public class GeoLocation
{
    public GeoLocation()
    {

    }
    public double Longitude { get; set; }
    public double Latitude { get; set; }

    public GeoLocation? SouthWest { get; set; }
    public GeoLocation? NorthEast { get; set; }

    public bool IsValid
    {
        get
        {
            if (!String.IsNullOrEmpty(ErrorMessage))
            {
                return false;
            }

            if (Latitude > 90 || Latitude < -90)
            {
                return false;
            }
            if (Latitude > 180 || Latitude < -180)
            {
                return false;
            }
            return true;
        }
    }


    public string ErrorMessage { get; set; } = "";

    public void Deconstruct(out double x, out double y)
    {
        x = Longitude;
        y = Latitude;
    }
}
