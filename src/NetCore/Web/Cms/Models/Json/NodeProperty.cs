using Newtonsoft.Json;

namespace Cms.Models.Json;

public class NodeProperty
{
    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "displayName")]
    [System.Text.Json.Serialization.JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty(PropertyName = "category")]
    [System.Text.Json.Serialization.JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonProperty(PropertyName = "description")]
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonProperty(PropertyName = "value")]
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public object Value { get; set; }

    [JsonProperty(PropertyName = "domainValues", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("domainValues")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] DomainValues { get; set; }

    [JsonProperty(PropertyName = "readonly")]
    [System.Text.Json.Serialization.JsonPropertyName("readonly")]
    public bool ReadOnly { get; set; }

    [JsonProperty(PropertyName = "isPassword", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("isPassword")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsPassword { get; set; }

    [JsonProperty(PropertyName = "isSecret", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("isSecret")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsSecret { get; set; }

    [JsonProperty(PropertyName = "hasEditor")]
    [System.Text.Json.Serialization.JsonPropertyName("hasEditor")]
    public bool HasEditor { get; set; }

    [JsonProperty(PropertyName = "isComplex")]
    [System.Text.Json.Serialization.JsonPropertyName("isComplex")]
    public bool IsComplexProperty { get; set; }

    [JsonProperty(PropertyName = "isHidden")]
    [System.Text.Json.Serialization.JsonPropertyName("isHidden")]
    public bool IsHidden { get; set; }

    [JsonProperty(PropertyName = "obsolete")]
    [System.Text.Json.Serialization.JsonPropertyName("obsolete")]
    public bool? IsObsolote { get; set; }

    [JsonProperty(PropertyName = "onChangeAction", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("onChangeAction")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string OnChangeAction { get; set; }

    [JsonProperty(PropertyName = "authTagName", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("authTagName")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string AuthTagName { get; set; }
}
