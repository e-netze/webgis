#nullable enable

using E.Standard.Security.Cryptography.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace E.Standard.WebMapping.Core.Api.EventResponse.Models;

[System.Text.Json.Serialization.JsonPolymorphic()]
[System.Text.Json.Serialization.JsonDerivedType(typeof(FilterDefinitionDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(VisFilterDefinitionDTO))]
public class FilterDefinitionDTO
{
    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = "";
}

public class VisFilterDefinitionDTO : FilterDefinitionDTO
{
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string ServiceId { get; set; } = "";

    [JsonProperty("sp_id", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("sp_id")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? SpanId { get; set; }

    [JsonProperty("sp_n")]
    [System.Text.Json.Serialization.JsonPropertyName("sp_m")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? SpanName { get; set; }    

    [JsonProperty(PropertyName = "args")]
    [System.Text.Json.Serialization.JsonPropertyName("args")]
    public VisFilterDefinitionArgument[]? Arguments { get; set; }

    [JsonProperty(PropertyName = "sig")]
    [System.Text.Json.Serialization.JsonPropertyName("sig")]
    public string? Signature { get; set; }

    #region Classes

    public class VisFilterDefinitionArgument
    {
        [JsonProperty(PropertyName = "n")]
        [System.Text.Json.Serialization.JsonPropertyName("n")]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "v")]
        [System.Text.Json.Serialization.JsonPropertyName("v")]
        public string? Value { get; set; }
    }

    #endregion
}
