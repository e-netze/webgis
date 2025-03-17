using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonFeatureLayers
{
    [JsonProperty("layers")]
    [System.Text.Json.Serialization.JsonPropertyName("layers")]
    public JsonFeatureLayer[] Layers { get; set; }

    public JsonFeatureLayer LayerById(int id)
    {
        if (Layers == null)
        {
            return null;
        }

        return (from l in Layers where l.Id == id select l).FirstOrDefault();
    }

    public bool HasDataLayers()
    {
        var count = CountDataLayers();

        return count > 0;
    }

    public int CountDataLayers()
    {
        if (Layers == null)
        {
            return 0;
        }

        string[] dataLayerTypes = new string[]
        {
            "Feature Layer",
            "Feature-Layer",
            "Annotation Layer",
            "Raster Layer",
            "Raster Catalog Layer",
            "Raster-Layer",
            "Raster-Catalog-Layer",
            "Mosaic Layer",
            "Mosaic-Layer"
        };

        return this.Layers.Where(l => dataLayerTypes.Contains(l.Type)).Count();
    }

    public IEnumerable<JsonFeatureLayer> EditableLayers()
    {
        return this.Layers.Where(l =>
        (l.Type == "Feature Layer" || l.Type == "Feature-Layer") &&
        !String.IsNullOrEmpty(l.Capabilities) &&
        l.Capabilities.Split(',').Select(c => c.Trim().ToLower()).Contains("editing"));
    }
}
