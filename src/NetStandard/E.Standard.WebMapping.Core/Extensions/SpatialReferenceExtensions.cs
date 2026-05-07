using System;
using System.Linq;

using E.Standard.WebMapping.Core.Geometry;

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
