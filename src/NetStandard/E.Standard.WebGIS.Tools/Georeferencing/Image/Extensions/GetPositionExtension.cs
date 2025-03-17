using E.Standard.WebGIS.Tools.Georeferencing.Image.Models;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image.Extensions;

static public class GetPositionExtension
{
    static public bool IsValid(this GeoPosition geoPosition)
    {
        return geoPosition != null &&
            (geoPosition.X != 0.0 || geoPosition.Y != 0.0 || geoPosition.Latitude != 0.0 || geoPosition.Longitude != 0.0);
    }
}
