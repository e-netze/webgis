using System;

using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIParagraph : UIElement
{
    public UIParagraph(string text)
        : base("paragraph")
        => (Text) = (text ?? String.Empty);

    [JsonProperty(PropertyName = "text", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("text")]
    public string Text { get; set; }
}