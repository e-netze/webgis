using E.Standard.CMS.Core.UI.Abstraction;
using Newtonsoft.Json;
using System;

namespace E.Standard.CMS.UI.Controls;

public class ImageButton : ControlLabel, IClickUIControl
{
    public ImageButton(string name = "")
        : base(name)
    {

    }

    [JsonProperty(PropertyName = "width")]
    [System.Text.Json.Serialization.JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonProperty(PropertyName = "height")]
    [System.Text.Json.Serialization.JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonProperty(PropertyName = "image")]
    [System.Text.Json.Serialization.JsonPropertyName("image")]
    public string Image { get; set; }

    public event EventHandler OnClick;

    public void FireClick()
    {
        OnClick?.Invoke(this, new EventArgs());
    }
}
