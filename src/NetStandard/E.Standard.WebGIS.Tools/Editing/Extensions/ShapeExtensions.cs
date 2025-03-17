using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Editing.Extensions;

static internal class ShapeExtensions
{
    static public void TryTransform(this Shape shape, int targetSRefId)
    {
        if (shape == null)
        {
            return;
        }

        if (shape.SrsId > 0 && targetSRefId > 0 && shape.SrsId != targetSRefId)
        {
            using (var geotransformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, shape.SrsId, targetSRefId))
            {
                geotransformer.Transform(shape);
            }
        }
    }

    static public bool HasAllGeometry<T>(this IEnumerable<Shape> shapes)
        where T : Shape
    {
        if (shapes == null || shapes.Count() == 0)
        {
            return false;
        }

        return shapes.Where(s => s == null || !(s is T)).Count() == 0;
    }

    static public Shape ToMultipartShape(this IEnumerable<Shape> shapes, int srsId = 0)
    {
        if (shapes.HasAllGeometry<Polyline>())
        {
            var result = new Polyline() { SrsId = srsId };

            foreach (Polyline polyline in shapes)
            {
                for (int p = 0; p < polyline.PathCount; p++)
                {
                    if (polyline[p] != null && polyline[p].Length > 0)
                    {
                        result.AddPath(polyline[p]);
                    }
                }
            }

            return result;
        }
        else if (shapes.HasAllGeometry<Polygon>())
        {
            var result = new Polygon() { SrsId = srsId };

            foreach (Polygon polygon in shapes)
            {
                for (int r = 0; r < polygon.RingCount; r++)
                {
                    if (polygon[r] != null && polygon[r].Length > 0)
                    {
                        result.AddRing(polygon[r]);
                    }
                }
            }

            return result;
        }
        else
        {
            throw new Exception("ToMultipartShape: unsupported geometry type");
        }
    }

    static public void ToWGS84(this IEnumerable<Shape> shapes, IBridge bridge, int fromSrefId)
    {
        if (shapes == null || shapes.Count() == 0 || fromSrefId == 0)
        {
            return;
        }

        var fromSref = bridge.CreateSpatialReference(fromSrefId);

        using (var transformer = new GeometricTransformerPro(fromSref, bridge.CreateSpatialReference(4326)))
        {
            foreach (var shape in shapes)
            {
                if (shape != null)
                {
                    transformer.Transform(shape);
                }
            }
        }
    }
}
