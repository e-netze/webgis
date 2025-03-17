using E.Standard.Api.App.DTOs;
using E.Standard.Json;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using System.Collections.Generic;

namespace E.Standard.Api.App.Extensions;

static public class FeatureCollectionExtensions
{
    static public string ToGeoJson(this FeatureCollection featureCollection)
    {
        var features = new FeaturesDTO(featureCollection);

        return JSerializer.Serialize(features);
    }

    static public string ToGeoJson(this Shape shape, ICollection<Attribute> attributes = null)
    {
        var featureCollection = new FeatureCollection();

        var feature = new E.Standard.WebMapping.Core.Feature();
        feature.Shape = shape;

        if (attributes != null)
        {
            feature.Attributes.AddRange(attributes);
        }

        featureCollection.Add(feature);

        return featureCollection.ToGeoJson();
    }
}
