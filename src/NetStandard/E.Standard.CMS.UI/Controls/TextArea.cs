using E.Standard.CMS.Core.UI.Abstraction;
using Newtonsoft.Json;
using System;

namespace E.Standard.CMS.UI.Controls;

public class TextArea : ControlLabel, IInputUIControl
{
    public TextArea(string name)
        : base(name)
    {
        this.Rows = 8;
    }

    private string _value = null;
    [JsonProperty(PropertyName = "value")]
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public string Value
    {
        get { return _value; }
        set
        {
            this.IsDirty = true;
            _value = value;
            FireChange();
        }
    }

    [JsonProperty(PropertyName = "rows")]
    [System.Text.Json.Serialization.JsonPropertyName("rows")]
    public int Rows { get; set; }

    [JsonProperty(PropertyName = "required")]
    [System.Text.Json.Serialization.JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsDirty { get; set; }

    public event EventHandler OnChange;

    public void FireChange()
    {
        this.OnChange?.Invoke(this, new EventArgs());
    }
}
