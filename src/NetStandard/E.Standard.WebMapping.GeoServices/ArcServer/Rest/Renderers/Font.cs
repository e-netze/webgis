using E.Standard.WebMapping.Core.Renderer;
using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Renderers;

class Font
{
    [JsonProperty("family")]
    [System.Text.Json.Serialization.JsonPropertyName("family")]
    public string Family { get; set; }

    [JsonProperty("size")]
    [System.Text.Json.Serialization.JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonProperty("style")]
    [System.Text.Json.Serialization.JsonPropertyName("style")]
    public string Style { get; set; }

    [JsonProperty("weight")]
    [System.Text.Json.Serialization.JsonPropertyName("weight")]
    public string Weight { get; set; }

    [JsonProperty("decoration")]
    [System.Text.Json.Serialization.JsonPropertyName("decoration")]
    public string Decoration { get; set; }

    public static string FontStyle(LabelRenderer.LabelStyleEnum style)
    {
        //"<italic | normal | oblique>"

        switch (style)
        {
            case LabelRenderer.LabelStyleEnum.italic:
            case LabelRenderer.LabelStyleEnum.bolditalic:
                return "italic";
                /*case Renderer.LabelRenderer.LabelStyleEnum.outline:
                    return "oblique";*/
        }

        return "normal";
    }

    public static string FontWeight(LabelRenderer.LabelStyleEnum style)
    {
        //"<bold | bolder | lighter | normal>"

        switch (style)
        {
            case LabelRenderer.LabelStyleEnum.bold:
            case LabelRenderer.LabelStyleEnum.bolditalic:
                return "bold";
        }

        return "normal";
    }

    public static string FontDecoration(LabelRenderer.LabelStyleEnum style)
    {
        //"<line-through | underline | none>"

        switch (style)
        {
            case LabelRenderer.LabelStyleEnum.underline:
                return "underline";
        }

        return "none";
    }
}
