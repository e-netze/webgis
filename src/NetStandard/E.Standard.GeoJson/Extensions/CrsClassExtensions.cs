using System;
using System.Linq;

namespace E.Standard.GeoJson.Extensions;

static public class CrsClassExtensions
{
    static public int TryGetEpsg(this GeoJsonFeatures.CrsClass crsClass)
    {
        if (crsClass == null)
        {
            return 0;
        }

        // https://gist.github.com/sgillies/1233327
        if (crsClass.Type == "name" && crsClass.Properties != null && crsClass.Properties.ContainsKey("name"))
        {
            var crsName = crsClass.Properties["name"].ToString();

            if (crsName.Equals("urn:ogc:def:crs:OGC:1.3:CRS84", StringComparison.OrdinalIgnoreCase))
            {
                return 4326;
            }

            if (crsName.Contains("EPSG"))  // urn:ogc:def:crs:EPSG::3857
            {
                var epsgString = crsName.Split(':').Last();
                if (int.TryParse(epsgString, out var epsg))
                {
                    return epsg;
                }
            }
        }

        return 0;
    }
}
