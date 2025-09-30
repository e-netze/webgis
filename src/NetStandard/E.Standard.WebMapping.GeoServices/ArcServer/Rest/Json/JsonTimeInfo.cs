#nullable enable

using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonTimeInfo
{
    [JsonProperty("startTimeField")]
    [System.Text.Json.Serialization.JsonPropertyName("startTimeField")]
    public string StartTimeField { get; set; } = "";

    [JsonProperty("endTimeField")]
    [System.Text.Json.Serialization.JsonPropertyName("endTimeField")]
    public string EndTimeField { get; set; } = "";

    [JsonProperty("trackIdField")]
    [System.Text.Json.Serialization.JsonPropertyName("trackIdField")]
    public string TrackIdField { get; set; } = "";

    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string InfoType { get; set; } = "";

    [JsonProperty("timeExtent")]
    [System.Text.Json.Serialization.JsonPropertyName("timeExtent")]
    public long[]? TimeExtent { get; set; }

    [JsonProperty("timeReference")]
    [System.Text.Json.Serialization.JsonPropertyName("timeReference")]
    public object? TimeReference { get; set; }

    [JsonProperty("timeInterval")]
    [System.Text.Json.Serialization.JsonPropertyName("timeInterval")]
    public float TimeInterval { get; set; }

    [JsonProperty("timeIntervalUnits")]
    [System.Text.Json.Serialization.JsonPropertyName("timeIntervalUnits")]
    public string TimeIntervalUnits { get; set; } = "";
}