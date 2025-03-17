using System;
using System.Collections.Generic;

namespace E.Standard.GeoCoding.GeoCode;

public class GeoLocation
{
    public GeoLocation()
    {

    }
    public double Longitude { get; set; }
    public double Latitude { get; set; }

    public GeoLocation SouthWest { get; set; }
    public GeoLocation NorthEast { get; set; }

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


    public string ErrorMessage { get; set; }

    #region Static Members

    static public IEnumerable<IGeoCode> ValidGeoCoders(string geoCode)
    {

        if (new GeoRef().IsValidGeoCode(geoCode))
        {
            yield return new GeoRef();
        }
        if (new OpenLocation().IsValidGeoCode(geoCode))
        {
            yield return new OpenLocation();
        }
        if (new GeoHash().IsValidGeoCode(geoCode))
        {
            yield return new GeoHash();
        }
        if (new GeographicCoordinates().IsValidGeoCode(geoCode))
        {
            yield return new GeographicCoordinates();
        }
    }

    static public IEnumerable<IGeoCode> Implentations
    {
        get
        {
            return new IGeoCode[]
            {
                new GeoRef(),
                new OpenLocation(),
                new GeoHash(),
                new GeographicCoordinates()
            };
        }
    }

    #endregion
}
