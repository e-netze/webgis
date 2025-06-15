using E.Standard.Api.App.DTOs.Geometry;
using E.Standard.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Api.App.DTOs.Print;

public sealed class CoordinateFeatureDTO
{
    [JsonProperty("oid")]
    [System.Text.Json.Serialization.JsonPropertyName("oid")]
    public string Oid { get; set; }

    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonProperty("geometry")]
    [System.Text.Json.Serialization.JsonPropertyName("geometry")]
    public PointDTO Geometry { get; set; }

    [JsonProperty("properties")]
    [System.Text.Json.Serialization.JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; }

    [JsonProperty("_fIndex")]
    [System.Text.Json.Serialization.JsonPropertyName("_fIndex")]
    public int MarkerIndex { get; set; }

    public E.Standard.WebMapping.Core.Feature ToFeature(string coordinateField)
    {
        var feature = new E.Standard.WebMapping.Core.Feature();

        if (this.Geometry?.coordinates != null)
        {
            feature.Shape = new E.Standard.WebMapping.Core.Geometry.Point(this.Geometry.coordinates[0], this.Geometry.coordinates[1]);
        }

        feature.Attributes.Add(new WebMapping.Core.Attribute("MarkerIndex", (MarkerIndex).ToString()));

        if (this.Properties != null)
        {
            foreach (var property in this.Properties.Keys)
            {
                try
                {
                    var coords = JSerializer.Deserialize<string[]>(this.Properties[property].ToString())
                                        .Where(c => !String.IsNullOrEmpty(c))
                                        .ToArray();

                    if (coords.Length == 2 && coordinateField == property)
                    {
                        feature.Attributes.Add(new WebMapping.Core.Attribute("Rechtswert", coords[0]));
                        feature.Attributes.Add(new WebMapping.Core.Attribute("Hochwert", coords[1]));
                    }
                    else if (coords.Length == 1)
                    {
                        feature.Attributes.Add(new WebMapping.Core.Attribute(property, coords[0]));
                    }
                }
                catch { }
            }
        }

        return feature;
    }
}
