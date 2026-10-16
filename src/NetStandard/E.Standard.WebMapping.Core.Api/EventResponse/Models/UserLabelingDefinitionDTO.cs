#nullable enable

using E.Standard.WebMapping.Core.Renderer.UserLabels;
using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.EventResponse.Models;

/// <summary>
/// User defined labeling for a layer, sent by the viewer with GetMap/Print requests
/// (request parameter "userlabels")
/// </summary>
public class UserLabelingDefinitionDTO
{
    /// <summary>
    /// serviceId~layerId
    /// </summary>
    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonProperty(PropertyName = "labeling")]
    [System.Text.Json.Serialization.JsonPropertyName("labeling")]
    public UserLabeling? Labeling { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string ServiceId { get; set; } = "";

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string LayerId { get; set; } = "";

    public void CalcServiceId()
    {
        if (Id != null && Id.Contains("~"))
        {
            var pos = Id.IndexOf('~');
            this.ServiceId = Id.Substring(0, pos);
            this.LayerId = Id.Substring(pos + 1);
        }
        else
        {
            this.LayerId = Id ?? "";
        }
    }
}
