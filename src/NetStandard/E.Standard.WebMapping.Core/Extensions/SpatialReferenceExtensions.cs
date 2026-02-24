using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Linq;

namespace E.Standard.WebMapping.Core.Extensions;

static public class SpatialReferenceExtensions
{
    static public bool IsWebMercator(this SpatialReference sRef)
    {
        if (sRef != null && KnownSRef.WebMercatorIds.Contains(sRef.Id))
        {
            return true;
        }

        return false;
    }
}
