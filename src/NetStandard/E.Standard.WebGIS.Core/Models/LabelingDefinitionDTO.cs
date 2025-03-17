using Newtonsoft.Json;

namespace E.Standard.WebGIS.Core.Models;

public class LabelingDefinitionDTO
{
    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }


    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string LayerId { get; set; }

    [JsonProperty(PropertyName = "fields")]
    [System.Text.Json.Serialization.JsonPropertyName("fields")]
    public LabelingField[] Fields { get; set; }



    #region Classes

    public class LabelingField
    {
        [JsonProperty(PropertyName = "name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "alias")]
        [System.Text.Json.Serialization.JsonPropertyName("alias")]
        public string Alias { get; set; }
    }

    #endregion
}
