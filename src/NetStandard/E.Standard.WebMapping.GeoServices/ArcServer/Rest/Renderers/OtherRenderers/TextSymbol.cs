using E.Standard.Platform;
using E.Standard.WebMapping.Core.Renderer;
using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Renderers;

class TextSymbol
{
    public TextSymbol()
    {
        this.Type = "esriTS";
    }
    public TextSymbol(LabelRenderer labelRenderer)
        : this()
    {
        if (labelRenderer == null)
        {
            return;
        }

        SetHalo(labelRenderer);
        this.Color = new int[] { labelRenderer.FontColor.R, labelRenderer.FontColor.G, labelRenderer.FontColor.B, 255 };
        this.Font = new Renderers.Font()
        {
            Family = SystemInfo.DefaultFontName,
            Size = (int)labelRenderer.FontSize,
            Style = Renderers.Font.FontStyle(labelRenderer.LabelStyle),
            Weight = Renderers.Font.FontWeight(labelRenderer.LabelStyle),
            Decoration = Renderers.Font.FontDecoration(labelRenderer.LabelStyle)
        };
    }
    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("color")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int[] Color { get; set; }

    [JsonProperty("backgroundColor", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("backgroundColor")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int[] BackgroundColor { get; set; }

    [JsonProperty("borderLineSize", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("borderLineSize")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? BorderLineSize { get; set; }

    [JsonProperty("borderLineColor", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("borderLineColor")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int[] BorderLineColor { get; set; }

    [JsonProperty("haloSize", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("haloSize")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? HaloSize { get; set; }

    [JsonProperty("haloColor", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("haloColor")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int[] HaloColor { get; set; }

    [JsonProperty("verticalAlignment")]
    [System.Text.Json.Serialization.JsonPropertyName("verticalAlignment")]
    public string VerticalAlignment { get; set; }

    [JsonProperty("horizontalAlignment")]
    [System.Text.Json.Serialization.JsonPropertyName("horizontalAlignment")]
    public string HorizontalAlignment { get; set; }

    [JsonProperty("rightToLeft")]
    [System.Text.Json.Serialization.JsonPropertyName("rightToLeft")]
    public bool RightToLeft { get; set; }

    [JsonProperty("angle")]
    [System.Text.Json.Serialization.JsonPropertyName("angle")]
    public int Angle { get; set; }

    [JsonProperty("xoffset")]
    [System.Text.Json.Serialization.JsonPropertyName("xoffset")]
    public int Xoffset { get; set; }

    [JsonProperty("yoffset")]
    [System.Text.Json.Serialization.JsonPropertyName("yoffset")]
    public int Yoffset { get; set; }

    [JsonProperty("kerning")]
    [System.Text.Json.Serialization.JsonPropertyName("kerning")]
    public bool Kerning { get; set; }

    [JsonProperty("font", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("font")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Font Font { get; set; }

    public void SetHalo(LabelRenderer renderer)
    {
        if (renderer == null || renderer.LabelBorderStyle == LabelRenderer.LabelBorderStyleEnum.none)
        {
            return;
        }

        switch (renderer.LabelBorderStyle)
        {
            case LabelRenderer.LabelBorderStyleEnum.blockout:
                this.BackgroundColor = new int[] { renderer.BorderColor.R, renderer.BorderColor.G, renderer.BorderColor.B, 255 };
                break;
            case LabelRenderer.LabelBorderStyleEnum.glowing:
                this.HaloColor = new int[] { renderer.BorderColor.R, renderer.BorderColor.G, renderer.BorderColor.B, 255 };
                this.HaloSize = 3;
                break;
            case LabelRenderer.LabelBorderStyleEnum.shadow:
                // ???
                break;
        }

        /*
        if (renderer.LabelStyle == Renderer.LabelRenderer.LabelStyleEnum.outline)
        {
            this.BorderLineColor = new int[] { renderer.BorderColor.R, renderer.BorderColor.G, renderer.BorderColor.B, 255 };
            this.BorderLineSize = 10;
        }
         * */
    }
}

/*
 * 

{
"type" : "esriTS",
"color" : <color>,
"backgroundColor" : <color>,
"borderLineColor" : <color>,
"verticalAlignment" : "<baseline | top | middle | bottom>",
"horizontalAlignment" : "<left | right | center | justify>",
"rightToLeft" : <true | false>,
"angle" : <angle>,
"xoffset" : <xoffset>,
"yoffset" : <yoffset>,
"kerning" : <true | false>,
"font" : {
"family" : "<fontFamily>",
"size" : <fontSize>,
"style" : "<italic | normal | oblique>",
"weight" : "<bold | bolder | lighter | normal>",
"decoration" : "<line-through | underline | none>"
}
}
 * 
 * Example
 * 
{
 "type": "esriTS",
 "color": [78,78,78,255],
 "backgroundColor": null,
 "borderLineColor": null,
 "verticalAlignment": "bottom",
 "horizontalAlignment": "left",
 "rightToLeft": false,
 "angle": 0,
 "xoffset": 0,
 "yoffset": 0,
 "font": {
  "family": "Arial",
  "size": 12,
  "style": "normal",
  "weight": "bold",
  "decoration": "none"
	}
}
 * 
 * */
