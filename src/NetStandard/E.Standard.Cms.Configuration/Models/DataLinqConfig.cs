using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.Cms.Configuration.Models;

public class DataLinqConfig
{
    [JsonProperty("instances")]
    [System.Text.Json.Serialization.JsonPropertyName("instances")]
    public IEnumerable<Instance> Instances { get; set; }

    [JsonProperty("useAppPrefixFilters")]
    [System.Text.Json.Serialization.JsonPropertyName("useAppPrefixFilters")]
    public bool UseAppPrefixFilters { get; set; }

    #region Classes

    public class Instance
    {
        [JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonProperty("url")]
        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string Url { get; set; }
    }

    #endregion
}
