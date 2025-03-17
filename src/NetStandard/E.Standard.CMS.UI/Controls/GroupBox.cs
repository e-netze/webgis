using Newtonsoft.Json;

namespace E.Standard.CMS.UI.Controls;

public class GroupBox : ControlLabel
{
    public GroupBox(string name = "") : base(name) { }

    [JsonProperty(PropertyName = "collapsed")]
    [System.Text.Json.Serialization.JsonPropertyName("collapsed")]
    public bool Collapsed { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string ItemUrl { get; set; }

    public void Add(Control control)
    {
        base.AddControl(control);
    }
}
