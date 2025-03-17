using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Core.Models;

public class PortalInfoDTO
{
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Version PortalVersion
    {
        get; set;
    }

    [JsonProperty(PropertyName = "version")]
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public string Version
    {
        get
        {
            return PortalVersion != null ? PortalVersion.ToString() : "0.0";
        }
        set
        {
            try
            {
                this.PortalVersion = new Version(value);
            }
            catch { this.PortalVersion = null; }
        }
    }

    [JsonProperty(PropertyName = "api-url")]
    [System.Text.Json.Serialization.JsonPropertyName("api-url")]
    public string ApiUrl { get; set; }

    [JsonProperty(PropertyName = "api-info")]
    [System.Text.Json.Serialization.JsonPropertyName("api-info")]
    public ApiInfoDTO ApiInfo { get; set; }

    public string CacheType { get; set; }
    public string CacheAsideType { get; set; }

    [JsonProperty(PropertyName = "cc_hash")]
    [System.Text.Json.Serialization.JsonPropertyName("cc_hash")]
    public string CryptoCompatibilityHash { get; set; }

    [JsonProperty(PropertyName = "c_is_compabile")]
    [System.Text.Json.Serialization.JsonPropertyName("c_is_compabile")]
    public bool CryptoIsCompatible => this.CryptoCompatibilityHash == this.ApiInfo?.CryptoCompatibilityHash;

    [JsonProperty(PropertyName = "headers", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("headers")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string> Headers { get; set; }
}
