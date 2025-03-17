using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIConditionDiv : UICollapsableElement
{
    public UIConditionDiv()
        : base("condition_div")
    {
        ConditionResult = true;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public ConditionTypes ConditionType { get; set; }

    [JsonProperty("condition_type")]
    [System.Text.Json.Serialization.JsonPropertyName("condition_type")]
    public string ConditionTypeValue => ConditionType.ToString().ToLower();

    [JsonProperty("condition_arguments")]
    [System.Text.Json.Serialization.JsonPropertyName("condition_arguments")]
    public string[] ConditionArguments { get; set; }

    [JsonProperty("condition_result")]
    [System.Text.Json.Serialization.JsonPropertyName("condition_result")]
    public bool ConditionResult { get; set; }

    [JsonProperty("contition_element_id")]
    [System.Text.Json.Serialization.JsonPropertyName("contition_element_id")]
    public string ContitionElementId { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public ContitionLogicalOperators ConditionLogicalOperator { get; set; }

    [JsonProperty("condition_logical_operator")]
    [System.Text.Json.Serialization.JsonPropertyName("condition_logical_operator")]
    public string ConditionLogicalOperatorValue => ConditionLogicalOperator.ToString().ToLower();

    public enum ContitionLogicalOperators
    {
        And = 0,
        Or = 1
    }

    public enum ConditionTypes
    {
        LayersVisible,
        LayersInScale,
        ElementValue
    }
}
