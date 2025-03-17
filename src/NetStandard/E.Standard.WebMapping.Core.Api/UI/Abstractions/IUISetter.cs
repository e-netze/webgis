using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI;

public interface IUISetter
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    string name { get; set; }
    string id { get; set; }
    string val { get; set; }
}
