using Newtonsoft.Json;

namespace Cms.Models.Json;

public class Node
{
    public Node() { }

    public Node(string name, string path)
        : this(name, name, path)
    {

    }
    public Node(string name, string aliasname, string path)
    {
        this.Name = name;
        this.AliasName = aliasname;
        this.Path = path;
    }

    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "aliasname")]
    [System.Text.Json.Serialization.JsonPropertyName("aliasname")]
    public string AliasName { get; set; }

    [JsonProperty(PropertyName = "primaryproperty_value", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("primaryproperty_value")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string PrimaryPropertyValue { get; set; }

    [JsonProperty(PropertyName = "path")]
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonProperty(PropertyName = "hasChildren")]
    [System.Text.Json.Serialization.JsonPropertyName("hasChildren")]
    public bool HasChildren { get; set; }

    [JsonProperty(PropertyName = "hasContent")]
    [System.Text.Json.Serialization.JsonPropertyName("hasContent")]
    public bool HasContent { get; set; }

    [JsonProperty(PropertyName = "hasProperties")]
    [System.Text.Json.Serialization.JsonPropertyName("hasProperties")]
    public bool HasProperties { get; set; }

    [JsonProperty(PropertyName = "isDeletable")]
    [System.Text.Json.Serialization.JsonPropertyName("isDeletable")]
    public bool IsDeletable { get; set; }

    [JsonProperty(PropertyName = "isRefreshable")]
    [System.Text.Json.Serialization.JsonPropertyName("isRefreshable")]
    public bool IsRefreshable { get; set; }

    [JsonProperty(PropertyName = "target", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("target")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Target { get; set; }

    [JsonProperty(PropertyName = "isTargetValid", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("isTargetValid")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsTargetValid { get; set; }

    [JsonProperty(PropertyName = "obsolete", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("obsolete")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? Obsolete { get; set; }

    [JsonProperty(PropertyName = "isRequired", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("isRequired")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsRequired { get; set; }

    [JsonProperty(PropertyName = "isRecommended", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("isRecommended")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsRecommended { get; set; }

    [JsonProperty(PropertyName = "isCopyable")]
    [System.Text.Json.Serialization.JsonPropertyName("isCopyable")]
    public bool IsCopyable { get; set; }

    [JsonProperty(PropertyName = "hasSecurityRestrictions")]
    [System.Text.Json.Serialization.JsonPropertyName("hasSecurityRestrictions")]
    public bool HasSecurityRestrictions { get; set; }

    [JsonProperty(PropertyName = "hasSecurityExclusiveRestrictions")]
    [System.Text.Json.Serialization.JsonPropertyName("hasSecurityExclusiveRestrictions")]
    public bool HasSecurityExclusiveRestrictions { get; set; }

    [JsonProperty(PropertyName = "hasSecurity")]
    [System.Text.Json.Serialization.JsonPropertyName("hasSecurity")]
    public bool HasSecurity { get; set; }
}
