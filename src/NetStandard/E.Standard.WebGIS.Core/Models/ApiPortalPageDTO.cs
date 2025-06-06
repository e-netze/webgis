using E.Standard.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;

namespace E.Standard.WebGIS.Core.Models;

public class ApiPortalPageDTO
{
    [JsonProperty("id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
	[System.Text.Json.Serialization.JsonConverter(typeof(StringConverter))]
	public string Id { get; set; }
    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonProperty("description")]
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonProperty("users")]
    [System.Text.Json.Serialization.JsonPropertyName("users")]
    public string[] Users { get; set; }
    [JsonProperty("mapauthors")]
    [System.Text.Json.Serialization.JsonPropertyName("mapauthors")]
    public string[] MapAuthors { get; set; }
    [JsonProperty("contentauthors")]
    [System.Text.Json.Serialization.JsonPropertyName("contentauthors")]
    public string[] ContentAuthors { get; set; }

    [JsonProperty("bannerId")]
    [System.Text.Json.Serialization.JsonPropertyName("bannerId")]
	[System.Text.Json.Serialization.JsonConverter(typeof(StringConverter))]
	public string BannerId { get; set; }

    [JsonProperty("created")]
    [System.Text.Json.Serialization.JsonPropertyName("created")]
    public DateTime Created { get; set; }
    [JsonProperty("subscriber-name")]
    [System.Text.Json.Serialization.JsonPropertyName("subscriber-name")]
    public string Subscriber { get; set; }
    [JsonProperty("subscriber-id")]
    [System.Text.Json.Serialization.JsonPropertyName("subscriber-id")]
	[System.Text.Json.Serialization.JsonConverter(typeof(StringConverter))]
	public string SubscriberId { get; set; }

    [JsonProperty("subscriber-clientid")]
    [System.Text.Json.Serialization.JsonPropertyName("subscriber-clientid")]
	[System.Text.Json.Serialization.JsonConverter(typeof(StringConverter))]
	public string SubscriberClientId { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string ErrorMessage { get; set; }

    [JsonProperty("resourceGuid", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("resourceGuid")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string ResourceGuid { get; set; }

    [JsonProperty("html-meta-tags", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("html-meta-tags")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string HtmlMetaTags { get; set; }

    #region Methods

    public NameValueCollection ToParameterCollection()
    {
        NameValueCollection nvc = new NameValueCollection();

        nvc.Add("id", this.Id);
        nvc.Add("name", this.Name);
        nvc.Add("description", this.Description);

        return nvc;
    }

    #endregion
}
