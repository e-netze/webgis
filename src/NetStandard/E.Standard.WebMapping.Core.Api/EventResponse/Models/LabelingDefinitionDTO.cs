using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.EventResponse.Models;

public class LabelingDefinitionDTO
{
    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "field", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("field")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Field { get; set; }

    [JsonProperty(PropertyName = "howmanylabels", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("howmanylabels")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string HowManyLabels { get; set; }

    [JsonProperty(PropertyName = "size", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("size")]
    public int FontSize { get; set; }

    [JsonProperty(PropertyName = "style", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("style")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string FontStyle { get; set; }

    [JsonProperty(PropertyName = "col", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("col")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string FontColor { get; set; }

    [JsonProperty(PropertyName = "bcol", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("bcol")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string BorderColor { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string ServiceId { get; set; }

    public void CalcServiceId()
    {
        if (Id != null && Id.Contains("~"))
        {
            this.ServiceId = Id.Split('~')[0];
            this.Id = Id.Split('~')[1];
        }
    }
}
