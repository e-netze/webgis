using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonServices
{
    [JsonProperty(PropertyName = "currentVersion")]
    [System.Text.Json.Serialization.JsonPropertyName("currentVersion")]
    public double CurrentVersion { get; set; }

    [JsonProperty(PropertyName = "folders")]
    [System.Text.Json.Serialization.JsonPropertyName("folders")]
    public string[] Folders { get; set; }

    [JsonProperty(PropertyName = "services")]
    [System.Text.Json.Serialization.JsonPropertyName("services")]
    public AgsServices[] Services { get; set; }

    #region Classes

    public class AgsServices
    {
        [JsonProperty(PropertyName = "name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; }
    }

    #endregion
}
