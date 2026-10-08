using Newtonsoft.Json;
using System;

namespace Portal.Core.Models.Map;

// a validated (not expired) branch link token: ?branch=enc:...
public class BranchLinkModel
{
    [JsonProperty("token")]
    [System.Text.Json.Serialization.JsonPropertyName("token")]
    public string Token { get; set; }

    [JsonProperty("encoded")]
    [System.Text.Json.Serialization.JsonPropertyName("encoded")]
    public string Encoded { get; set; }

    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("expires")]
    [System.Text.Json.Serialization.JsonPropertyName("expires")]
    public DateTime? Expires { get; set; }
}

public class BranchLinkErrorModel
{
    public string Message { get; set; }
    public string MapUrl { get; set; }
}