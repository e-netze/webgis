using E.Standard.CMS.Core.UI.Abstraction;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace E.Standard.CMS.UI.Controls;

public class Input : ControlLabel, IInputUIControl
{
    public Input(string name)
        : base(name)
    {

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

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsDirty { get; set; }

    [JsonProperty(PropertyName = "isPassword")]
    [System.Text.Json.Serialization.JsonPropertyName("isPassword")]
    public bool IsPassword { get; set; }

    [JsonProperty(PropertyName = "dependsFrom", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("dependsFrom")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string DependsFrom { get; set; }

    [JsonProperty(PropertyName = "regexReplace", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("regexReplace")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<KeyValuePair<string, string>> RegexReplace { get; set; }

    [JsonProperty(PropertyName = "modifyMethods", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("modifyMethods")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] ModifyMethods { get; set; }

    [JsonProperty(PropertyName = "placeholder")]
    [System.Text.Json.Serialization.JsonPropertyName("placeholder")]
    public string Placeholder { get; set; }

    [JsonProperty(PropertyName = "required")]
    [System.Text.Json.Serialization.JsonPropertyName("required")]
    public bool Required { get; set; }

    public void FireChange()
    {
        this.OnChange?.Invoke(this, new EventArgs());
    }
    public event EventHandler OnChange;
}
