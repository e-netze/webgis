using Newtonsoft.Json;

namespace E.Standard.CMS.UI.Controls;

public class LazyNavTree : Control
{
    public LazyNavTree(string name = "")
        : base(name)
    {

    }

    [JsonProperty(PropertyName = "singleSelect")]
    [System.Text.Json.Serialization.JsonPropertyName("singleSelect")]
    public bool SingleSelect { get; set; }
}
