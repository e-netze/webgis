#nullable enable

using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Export.Extensions;

static internal class FeatureCollectionExtensions
{
    static public Shape GeometryPrototype(this FeatureCollection? featureCollection)
        => featureCollection?.FirstOrDefault()?.Shape switch
        {
            Point _ => new Point(),
            MultiPoint _ => new MultiPoint(),
            Polyline _ => new Polyline(),
            Polygon _ => new Polygon(),
            _ => throw new NotSupportedException("Geometry type is not supported.")
        };

}
