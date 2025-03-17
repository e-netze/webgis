using E.Standard.Api.App.DTOs.Geometry;
using E.Standard.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Api.App.DTOs.Print;

public sealed class QueryFeatureDTO
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
    public object properties { get; set; }

    private IEnumerable<Dictionary<string, object>> _serializedProperties = null;
    public IEnumerable<Dictionary<string, object>> Properties
    {
        get
        {
            if (_serializedProperties != null)
            {
                return _serializedProperties;
            }

            if (this.properties == null)
            {
                return null;
            }

            try
            {
                var jsonString = this.properties.ToString().Trim();
                if (jsonString.StartsWith("["))
                {
                    _serializedProperties = JSerializer.Deserialize<IEnumerable<Dictionary<string, object>>>(jsonString);
                }
                else
                {
                    _serializedProperties = new Dictionary<string, object>[] { JSerializer.Deserialize<Dictionary<string, object>>(jsonString) };
                }
            }
            catch
            {
                _serializedProperties = new Dictionary<string, object>[0];
            }

            return _serializedProperties;
        }
    }

    [JsonProperty("_findex")]
    [System.Text.Json.Serialization.JsonPropertyName("_findex")]
    public int MarkerIndex { get; set; }

    public E.Standard.WebMapping.Core.Feature ToFeature()
    {
        var feature = new E.Standard.WebMapping.Core.Feature();

        if (this.Geometry?.coordinates != null)
        {
            feature.Shape = new E.Standard.WebMapping.Core.Geometry.Point(this.Geometry.coordinates[0], this.Geometry.coordinates[1]);
        }

        feature.Attributes.Add(new WebMapping.Core.Attribute("MarkerIndex", (MarkerIndex + 1).ToString()));

        var firstProperties = this.Properties.FirstOrDefault();
        if (firstProperties != null)
        {
            foreach (var property in firstProperties.Keys)
            {
                feature.Attributes.Add(new WebMapping.Core.Attribute(property, firstProperties[property]?.ToString() ?? String.Empty));
            }
        }

        return feature;
    }
}
