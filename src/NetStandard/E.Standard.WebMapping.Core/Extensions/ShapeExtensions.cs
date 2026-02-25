#nullable enable

using E.Standard.WebMapping.Core.Geometry;
using System.Linq;

namespace E.Standard.WebMapping.Core.Extensions;

static public class ShapeExtensions
{
    static public void TransformTo(this Shape? shape, SpatialReference? sRef)
      => shape.TransformTo(sRef?.Id ?? 0);

    static public void TransformTo(this Shape? shape, int sRefId)
    {
        if (shape?.SrsId > 0 && sRefId > 0 && shape.SrsId != sRefId)
        {
            using (var transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore.SpatialReferences, shape.SrsId, sRefId))
            {
                transformer.Transform(shape);
                shape.SrsId = sRefId;
            }
        }
    }

    static public bool IsWebMercator(this Shape? shape)
    {
        if (shape is not null && KnownSRef.WebMercatorIds.Contains(shape.SrsId))
        {
            return true;
        }

        return false;
    }
}
