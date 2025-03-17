using Newtonsoft.Json;
using System;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIValidation : UIElement
{
    public UIValidation(string elementType)
        : base(elementType)
    {

    }

    [JsonProperty("validation_required", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("validation_required")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsRequired { get; set; }

    [JsonProperty("validation_minlen", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("validation_minlen")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? MinLength { get; set; }

    [JsonProperty("validation_regex", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("validation_regex")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Regex { get; set; }

    [JsonProperty("validation_errormsg", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("validation_errormsg")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string ValidationErrorMessage { get; set; }

    [JsonProperty("has_validation")]
    [System.Text.Json.Serialization.JsonPropertyName("has_validation")]
    public bool? HasValidation
    {
        get
        {
            if (IsRequired != true &&
                (MinLength.HasValue == false || MinLength.Value <= 0) &&
                String.IsNullOrEmpty(Regex))
            {
                return null;
            }

            return true;
        }
    }
}
