using Newtonsoft.Json;
using System;

namespace E.Standard.WebGIS.Core.Models;

public class ApiInfoDTO
{
    public ApiInfoDTO()
    {

    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string JsVersion
    {
        get; set;
    }

    [JsonProperty(PropertyName = "version")]
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public string Version
    {
        get
        {
            return JsVersion != null ? JsVersion.ToString() : "0.0";
        }
        set
        {
            try
            {
                this.JsVersion = new Version(value).ToString();
            }
            catch { this.JsVersion = null; }
        }
    }

    [JsonProperty(PropertyName = "cache")]
    [System.Text.Json.Serialization.JsonPropertyName("cache")]
    public CacheInfo Cache { get; set; }

    [JsonProperty(PropertyName = "cc_hash")]
    [System.Text.Json.Serialization.JsonPropertyName("cc_hash")]
    public string CryptoCompatibilityHash { get; set; }

    #region Classes

    public class CacheInfo
    {
        [JsonProperty(PropertyName = "cms_count")]
        [System.Text.Json.Serialization.JsonPropertyName("cms_count")]
        public int CmsCount { get; set; }

        [JsonProperty(PropertyName = "services_count")]
        [System.Text.Json.Serialization.JsonPropertyName("services_count")]
        public int ServicesCount { get; set; }

        [JsonProperty(PropertyName = "extents_count")]
        [System.Text.Json.Serialization.JsonPropertyName("extents_count")]
        public int ExtentsCount { get; set; }

        [JsonProperty(PropertyName = "presentations_count")]
        [System.Text.Json.Serialization.JsonPropertyName("presentations_count")]
        public int PresentationsCount { get; set; }

        [JsonProperty(PropertyName = "queries_count")]
        [System.Text.Json.Serialization.JsonPropertyName("queries_count")]
        public int QueriesCount { get; set; }

        [JsonProperty(PropertyName = "tools_count")]
        [System.Text.Json.Serialization.JsonPropertyName("tools_count")]
        public int ToolsCount { get; set; }

        [JsonProperty(PropertyName = "searchservice_count")]
        [System.Text.Json.Serialization.JsonPropertyName("searchservice_count")]
        public int SearchServicCount { get; set; }

        [JsonProperty(PropertyName = "editthemes_count")]
        [System.Text.Json.Serialization.JsonPropertyName("editthemes_count")]
        public int EditThemesCount { get; set; }

        [JsonProperty(PropertyName = "visfilter_count")]
        [System.Text.Json.Serialization.JsonPropertyName("visfilter_count")]
        public int VisFiltersCount { get; set; }

        [JsonProperty(PropertyName = "init_time")]
        [System.Text.Json.Serialization.JsonPropertyName("init_time")]
        public DateTime InitialTime { get; set; }
    }

    #endregion
}
