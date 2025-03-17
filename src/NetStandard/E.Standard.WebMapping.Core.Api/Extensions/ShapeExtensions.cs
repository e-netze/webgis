using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebMapping.Core.Api.Extensions;

public static class ShapeExtensions
{
    public static Shape Project(this Shape shape, int targetSrs,
                                                  int defaultSourceSrs = 4326,
                                                  ShapeSrsProperties appendShapeProperterties = ShapeSrsProperties.SrsId)
    {
        if (shape != null)
        {
            shape.SrsId = shape.SrsId > 0 ? shape.SrsId : defaultSourceSrs;

            using (var transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, shape.SrsId, targetSrs))
            {
                transformer.Transform(shape, appendShapeProperterties);
            }
        }

        return shape;
    }

    public static void Project(this FeatureCollection featureCollection, int targetSrs,
                                                                         int defaultSourceSrs = 4326,
                                                                         ShapeSrsProperties appendShapeProperterties = ShapeSrsProperties.SrsId)
    {
        if (featureCollection == null)
        {
            return;
        }

        foreach (var feature in featureCollection)
        {
            feature.Shape = feature.Shape.Project(targetSrs, defaultSourceSrs, appendShapeProperterties);
        }
    }
}
