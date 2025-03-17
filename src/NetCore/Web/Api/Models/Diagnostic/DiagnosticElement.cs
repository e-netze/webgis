using Newtonsoft.Json;
using System.Collections.Generic;

namespace Api.Core.Models.Diagnostic;

public class DiagnosticElement
{
    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "text", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("text")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Text { get; set; }

    [JsonProperty(PropertyName = "elements", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("elements")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public DiagnosticElement[] Elements { get { return _elements.Count != 0 ? _elements.ToArray() : null; } }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public DiagnosticElementStatus Status { get; set; }

    [JsonProperty(PropertyName = "status")]
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string StatusMessage { get { return Status.ToString().ToLower(); } }

    private List<DiagnosticElement> _elements = new List<DiagnosticElement>();

    public void AddElement(DiagnosticElement element)
    {
        _elements.Add(element);
    }

    public void RemoveElements(DiagnosticElementStatus status)
    {
        foreach (var element in _elements.ToArray())
        {
            element.RemoveElements(status);

            if (element.Status == status)
            {
                _elements.Remove(element);
            }
        }
    }
}

public class DiagnosticTreeElement
{
    [JsonProperty("nodes", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("nodes")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, Dictionary<string, DiagnosticTreeElement>> Elements { get; set; } = null;

    public Dictionary<string, Dictionary<string, DiagnosticTreeElement>> GetElements()
    {
        if (Elements == null)
        {
            Elements = new Dictionary<string, Dictionary<string, DiagnosticTreeElement>>();
        }

        return Elements;
    }
}
