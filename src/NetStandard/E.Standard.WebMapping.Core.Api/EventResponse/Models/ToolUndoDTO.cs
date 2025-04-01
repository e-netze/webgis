using E.Standard.Json;
using E.Standard.WebMapping.Core.Editing;
using E.Standard.WebMapping.Core.Geometry;
using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.EventResponse.Models;

public class ToolUndoDTO
{
    public ToolUndoDTO()
    {

    }

    public ToolUndoDTO(EditUndoableDTO data, Shape previewShape)
    {
        this.DataString = JSerializer.Serialize(data);
        this.PreviewShape = previewShape;
    }

    [JsonProperty(PropertyName = "title")]
    [System.Text.Json.Serialization.JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonProperty(PropertyName = "icon")]
    [System.Text.Json.Serialization.JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonProperty(PropertyName = "preview")]
    [System.Text.Json.Serialization.JsonPropertyName("preview")]
    public object Preview { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Shape PreviewShape { get; private set; }

    [JsonProperty(PropertyName = "data")]
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public string DataString { get; set; }

    public T GetData<T>()
    {
        return JSerializer.Deserialize<T>(this.DataString);
    }
}
