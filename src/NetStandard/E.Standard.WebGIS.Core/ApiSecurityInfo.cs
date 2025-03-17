using Newtonsoft.Json;

namespace E.Standard.WebGIS.Core;

public class ApiSecurityInfo
{
    [JsonProperty("instanceRoles")]
    [System.Text.Json.Serialization.JsonPropertyName("instanceRoles")]
    public string[] InstanceRoles { get; set; }
}
