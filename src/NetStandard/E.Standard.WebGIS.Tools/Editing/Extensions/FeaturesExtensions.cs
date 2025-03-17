using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Editing;
using E.Standard.WebMapping.Core.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Extensions;

static internal class FeaturesExtensions
{
    static public IEnumerable<Feature> ExplodeMultipartFeatures(this IEnumerable<Feature> features)
    {
        List<Feature> result = new List<Feature>();

        foreach (var originalFeature in features)
        {
            if (originalFeature.Shape != null && originalFeature.Shape.IsMultipart)
            {
                result.AddRange(originalFeature.Shape.Multiparts.Select(shape =>
                {
                    var feature = originalFeature.Clone(false);
                    feature.Oid = 0;
                    feature.Shape = shape;

                    return feature;
                }));
            }
            else
            {
                result.Add(originalFeature);
            }
        }

        return result;
    }

    async static public Task<EditUndoableDTO> CreateUndoable(this IEnumerable<Feature> features,
                                                          IBridge bridge,
                                                          IFeatureWorkspace ws,
                                                          SqlCommand command,
                                                          IEnumerable<string> fields = null)
    {
        if (ws is IFeatureWorkspaceUndo)
        {
            return await ((IFeatureWorkspaceUndo)ws).CreateUndo(bridge,
                                                                features.Where(f => f.Oid > 0).Select(f => (long)f.Oid).ToArray(),
                                                                command,
                                                                fields: fields?.ToArray());

        }

        return null;
    }

    static public bool HasAllGeometry<T>(this IEnumerable<Feature> features)
        where T : Shape
    {
        if (features == null || features.Count() == 0)
        {
            return false;
        }

        return features.Where(f => f.Shape == null || !(f.Shape is T)).Count() == 0;
    }
}
