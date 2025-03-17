using E.Standard.Api.App.DTOs;
using System.Collections.Generic;

namespace Api.Core.AppCode.Extensions;

static public class FeaturesExtensions
{
    static public void Union(this FeaturesDTO inputFeatures)
    {
        List<E.Standard.Api.App.DTOs.FeatureDTO> features = new List<E.Standard.Api.App.DTOs.FeatureDTO>(inputFeatures.features);
        int index = 0;
        while (index < features.Count)
        {
            if (features[index].geometry != null)
            {
                for (int c = index + 1; c < features.Count; c++)
                {
                    if (features[c].geometry != null)
                    {
                        var canditate = features[c];
                        if (features[index].geometry.Equals(canditate.geometry))
                        {
                            if (features[index].properties is object[])
                            {
                                List<object> propertiesList = new List<object>((object[])features[index].properties);
                                propertiesList.Add(canditate.properties);
                                features[index].properties = propertiesList.ToArray();
                            }
                            else
                            {
                                features[index].properties = new object[] { features[index].properties, canditate.properties };
                            }
                            features.RemoveAt(c);
                            c--;
                        }
                    }
                }
            }
            index++;
        }

        inputFeatures.features = features.ToArray();
    }

    static public void AppendFeatures(this FeaturesDTO features, FeaturesDTO appendFeatures)
    {
        if (features == null || appendFeatures?.features == null)
        {
            return;
        }

        var featureList = new List<FeatureDTO>(features.features);
        featureList.AddRange(appendFeatures.features);

        features.features = featureList.ToArray();
    }
}
