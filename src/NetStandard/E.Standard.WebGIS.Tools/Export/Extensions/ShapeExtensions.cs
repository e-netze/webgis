#nullable enable

using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebGIS.Tools.Export.Extensions;

static internal class ShapeExtensions
{
    static public MultiPoint? MultiPointFromPointOrMultiPoint(this Shape shape)
        => shape switch
        {
            MultiPoint multiPoint => multiPoint,
            Point point => new MultiPoint([point]),
            _ => null
        };
}
