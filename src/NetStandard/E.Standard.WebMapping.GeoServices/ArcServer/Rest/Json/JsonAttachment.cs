using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

internal class JsonAttachmentResponse
{
    [JsonProperty("fields")]
    [System.Text.Json.Serialization.JsonPropertyName("fields")]
    public JsonField[] Fields { get; set; }

    [JsonProperty("attachmentGroups")]
    [System.Text.Json.Serialization.JsonPropertyName("attachmentGroups")]
    public JsonAttachmentGroup[] AttachmentGroups { get; set; }
}

internal class JsonAttachmentGroup
{
    [JsonProperty("parentObjectId")]
    [System.Text.Json.Serialization.JsonPropertyName("parentObjectId")]
    public int ParentObjectId { get; set; }

    [JsonProperty("parentGlobalId")]
    [System.Text.Json.Serialization.JsonPropertyName("parentGlobalId")]
    public string ParentGlobalId { get; set; }

    [JsonProperty("attachmentInfos")]
    [System.Text.Json.Serialization.JsonPropertyName("attachmentInfos")]
    public JsonAttachmentInfo[] AttachmentInfos { get; set; }
}

internal class JsonAttachmentInfo
{
    [JsonProperty("id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("contentType")]
    [System.Text.Json.Serialization.JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonProperty("size")]
    [System.Text.Json.Serialization.JsonPropertyName("size")]
    public long size { get; set; }

    [JsonProperty("url")]
    [System.Text.Json.Serialization.JsonPropertyName("url")]
    public string Url { get; set; }
}