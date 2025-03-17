using E.Standard.Json.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.Api.App.DTOs;

public sealed class DomainCodedValuesDTO
{
    [JsonProperty("fieldname")]
    [System.Text.Json.Serialization.JsonPropertyName("fieldname")]
    public string Fieldname { get; set; }

    [JsonProperty("codedvalues")]
    [System.Text.Json.Serialization.JsonPropertyName("codedvalues")]
    public IEnumerable<CodedValue> CodedValues { get; set; }

    #region Classes

    public sealed class CodedValue
    {
        [JsonProperty("code")]
        [System.Text.Json.Serialization.JsonPropertyName("code")]
        [System.Text.Json.Serialization.JsonConverter(typeof(StringConverter))]
        public string Code { get; set; }

        [JsonProperty("value")]
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        [System.Text.Json.Serialization.JsonConverter(typeof(StringConverter))]
        public string Value { get; set; }
    }

    #endregion
}
