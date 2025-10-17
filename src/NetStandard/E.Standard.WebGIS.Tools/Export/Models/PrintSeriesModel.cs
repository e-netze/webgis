using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Export.Models;

internal class PrintSeriesModel
{
    [JsonProperty("layoutId")]
    [System.Text.Json.Serialization.JsonPropertyName("layoutId")]
    public string LayoutId { get; set; }

    [JsonProperty("format")]
    [System.Text.Json.Serialization.JsonPropertyName("format")]
    public string Format { get; set; }

    [JsonProperty("scale")]
    [System.Text.Json.Serialization.JsonPropertyName("scale")]
    public double Scale { get; set; }

    [JsonProperty("quality")]
    [System.Text.Json.Serialization.JsonPropertyName("quality")]
    public int Quality { get; set; }

    [JsonProperty("sketchWKT")]
    [System.Text.Json.Serialization.JsonPropertyName("sketchWKT")]
    public string SketchWKT { get; set; }

    [JsonProperty("sketchSrs")]
    [System.Text.Json.Serialization.JsonPropertyName("sketchSrs")]
    public int SketchSrs { get; set; }
}
