using Newtonsoft.Json;

namespace webgis.deploy.Models;

internal class ModifyCssModel
{
    [JsonProperty(PropertyName = "mode")]
    [System.Text.Json.Serialization.JsonPropertyName("mode")]
    public string Mode { get; set; }

    [JsonProperty(PropertyName = "modifiers")]
    [System.Text.Json.Serialization.JsonPropertyName("modifiers")]
    public IEnumerable<ModifierDefinition> ModifierDefinitions { get; set; }

    #region Classes

    public class ModifierDefinition
    {
        [JsonProperty(PropertyName = "pattern")]
        [System.Text.Json.Serialization.JsonPropertyName("pattern")]
        public string Pattern { get; set; }
        [JsonProperty(PropertyName = "replace")]
        [System.Text.Json.Serialization.JsonPropertyName("replace")]
        public string Replace { get; set; }

        //[JsonProperty(PropertyName = "add")]
        //[System.Text.Json.Serialization.JsonPropertyName("add")]
        //public Dictionary<string, string>? Add { get; set; }
    }

    #endregion
}
