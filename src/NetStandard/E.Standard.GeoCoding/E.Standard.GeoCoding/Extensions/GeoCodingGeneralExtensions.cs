using E.Standard.GeoCoding.GeoCode;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.GeoCoding.Extensions;

static public class GeoCodingGeneralExtensions
{
    extension(string? codecName)
    {
        public IGeoCoder? TryGetGeoCoderByName()
            => AllGeoCoders()
                .FirstOrDefault(c => c.Name?.Equals(codecName, StringComparison.OrdinalIgnoreCase) == true);
    }

    static internal IEnumerable<IGeoCoder> AllGeoCoders()
        => [
            new UtmRef(),
            new OpenLocation(),
            new GeoHash(),
            new GeoRef(),
            new GeographicCoordinates()
           ];
}
