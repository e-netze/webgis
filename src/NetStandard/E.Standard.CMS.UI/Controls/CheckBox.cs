using E.Standard.CMS.Core.UI.Abstraction;
using Newtonsoft.Json;
using System;

namespace E.Standard.CMS.UI.Controls;

public class CheckBox : ControlLabel, IInputUIControl
{
    public CheckBox(string name = "") : base(name) { }

    #region IInputUIControl

    public string Value { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsDirty { get; set; }

    [JsonProperty(PropertyName = "required")]
    [System.Text.Json.Serialization.JsonPropertyName("required")]
    public bool Required { get; set; }

    public event EventHandler OnChange;

    public void FireChange()
    {
        OnChange?.Invoke(this, new EventArgs());
    }

    #endregion
}
