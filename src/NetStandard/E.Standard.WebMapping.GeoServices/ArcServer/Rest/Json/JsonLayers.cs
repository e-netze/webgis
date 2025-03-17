using Newtonsoft.Json;
using System;
using System.Linq;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonLayers
{
    [JsonProperty("layers")]
    [System.Text.Json.Serialization.JsonPropertyName("layers")]
    public JsonLayer[] Layers { get; set; }

    public void SetParentLayers()
    {
        if (Layers == null)
        {
            return;
        }

        foreach (var layer in Layers)
        {
            if (layer.ParentLayer != null)
            {
                layer.ParentLayer = LayerById(layer.ParentLayer.Id);
            }
        }
    }

    public JsonLayer LayerById(int id)
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
}
