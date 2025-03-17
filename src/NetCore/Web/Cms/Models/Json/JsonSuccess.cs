using Newtonsoft.Json;

namespace Cms.Models.Json;

internal class JsonSuccess
{
    [JsonProperty(PropertyName = "success")]
    [System.Text.Json.Serialization.JsonPropertyName("success")]
    public bool Success => true;
}

internal class JsonException
{
    public JsonException(string messsage)
    {
        this.Exception = messsage;
    }

    [JsonProperty(PropertyName = "success")]
    [System.Text.Json.Serialization.JsonPropertyName("success")]
    public bool Success => false;

    [JsonProperty(PropertyName = "exception")]
    [System.Text.Json.Serialization.JsonPropertyName("exception")]
    public string Exception { get; set; }
}
