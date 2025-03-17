using Newtonsoft.Json;

namespace E.Standard.WebGIS.Core.Models;

public class ApiAppDTO
{
    [JsonProperty(PropertyName = "template")]
    [System.Text.Json.Serialization.JsonPropertyName("template")]
    public string Template { get; set; }

    [JsonProperty(PropertyName = "creator")]
    [System.Text.Json.Serialization.JsonPropertyName("creator")]
    public string Creator { get; set; }

    [JsonProperty(PropertyName = "parameters")]
    [System.Text.Json.Serialization.JsonPropertyName("parameters")]
    public Parameter[] TemplateParameters { get; set; }

    #region Classes

    public class Parameter
    {
        [JsonProperty(PropertyName = "title")]
        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "value")]
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonProperty(PropertyName = "type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "setters")]
        [System.Text.Json.Serialization.JsonPropertyName("setters")]
        public ParameterSetter[] Setters { get; set; }

        [JsonProperty(PropertyName = "inputType")]
        [System.Text.Json.Serialization.JsonPropertyName("inputType")]
        public string InputType { get; set; }

        [JsonProperty(PropertyName = "jsonExample")]
        [System.Text.Json.Serialization.JsonPropertyName("jsonExample")]
        public object JsonExample { get; set; }

        public class ParameterSetter
        {
            [JsonProperty(PropertyName = "parameter")]
            [System.Text.Json.Serialization.JsonPropertyName("parameter")]
            public string Parameter { get; set; }

            [JsonProperty(PropertyName = "value")]
            [System.Text.Json.Serialization.JsonPropertyName("value")]
            public string Value { get; set; }
        }
    }



    #endregion
}
