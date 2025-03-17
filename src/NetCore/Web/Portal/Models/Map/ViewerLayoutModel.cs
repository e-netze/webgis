using Newtonsoft.Json;
using System.Collections.Generic;

namespace Portal.Core.Models.Map;

public class ViewerLayoutModel
{
    //[JsonPropertyName("width")]
    [JsonProperty("width")]
    [System.Text.Json.Serialization.JsonPropertyName("width")]
    public int Width { get; set; }

    //[JsonPropertyName("templates")]
    [JsonProperty("templates")]
    [System.Text.Json.Serialization.JsonPropertyName("templates")]
    public IEnumerable<Template> Templates { get; set; }

    #region Classes

    public class Template
    {
        //[JsonPropertyName("id")]
        [JsonProperty("id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }
        //[JsonPropertyName("name")]
        [JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }
        //[JsonPropertyName("file")]
        [JsonProperty("file")]
        [System.Text.Json.Serialization.JsonPropertyName("file")]
        public string File { get; set; }
    }

    #endregion
}
