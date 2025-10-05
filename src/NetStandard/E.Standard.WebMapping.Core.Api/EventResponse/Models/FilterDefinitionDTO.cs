using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.EventResponse.Models;

[System.Text.Json.Serialization.JsonPolymorphic()]
[System.Text.Json.Serialization.JsonDerivedType(typeof(FilterDefinitionDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(VisFilterDefinitionDTO))]
public class FilterDefinitionDTO
{
    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }
}

public class VisFilterDefinitionDTO : FilterDefinitionDTO
{
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string ServiceId { get; set; }

    [JsonProperty(PropertyName = "args")]
    [System.Text.Json.Serialization.JsonPropertyName("args")]
    public VisFilterDefinitionArgument[] Arguments { get; set; }

    public void AddArgument(string name, string value)
    {
        List<VisFilterDefinitionArgument> args = Arguments != null ? new List<VisFilterDefinitionArgument>(Arguments) : new List<VisFilterDefinitionArgument>();

        args.Add(new VisFilterDefinitionArgument()
        {
            Name = name,
            Value = value
        });
        this.Arguments = args.ToArray();
    }

    public void CalcServiceId()
    {
        if (Id != null && Id.Contains("~"))
        {
            this.ServiceId = Id.Split('~')[0];
            this.Id = Id.Split('~')[1];
        }
    }

    #region Classes

    public class VisFilterDefinitionArgument
    {
        [JsonProperty(PropertyName = "n")]
        [System.Text.Json.Serialization.JsonPropertyName("n")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "v")]
        [System.Text.Json.Serialization.JsonPropertyName("v")]
        public string Value { get; set; }
    }

    #endregion
}
