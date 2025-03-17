using Newtonsoft.Json;
using System;

namespace E.Standard.WebGIS.Core.Serialization;

public class SharedMapMeta
{
    public SharedMapMeta()
    {
        this.Created = DateTime.UtcNow;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public const string SharedMapsCategory = "_sharedmaps"; // Never change!!

    [JsonProperty("map-name")]
    [System.Text.Json.Serialization.JsonPropertyName("map-name")]
    public string MapName { get; set; }

    [JsonProperty("map-category")]
    [System.Text.Json.Serialization.JsonPropertyName("map-category")]
    public string MapCategory { get; set; }

    [JsonProperty("expires-ticks")]
    [System.Text.Json.Serialization.JsonPropertyName("expires-ticks")]
    public long ExpiresTicksUtc { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime Expires
    {
        get
        {
            return new DateTime(this.ExpiresTicksUtc, DateTimeKind.Utc);
        }
        set
        {
            this.ExpiresTicksUtc = value.ToUniversalTime().Ticks;
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsExpired
    {
        get
        {
            return DateTime.UtcNow > new DateTime(this.ExpiresTicksUtc, DateTimeKind.Utc);
        }
    }

    [JsonProperty("created-ticks")]
    [System.Text.Json.Serialization.JsonPropertyName("created-ticks")]
    public long CreatedTicksUtc { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime Created
    {
        get
        {
            return new DateTime(this.CreatedTicksUtc, DateTimeKind.Utc);
        }
        set
        {
            this.CreatedTicksUtc = value.ToUniversalTime().Ticks;
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool UserMessageRecommended
    {
        get
        {
            DateTime halfTime = new DateTime(this.CreatedTicksUtc / 2 + this.ExpiresTicksUtc / 2, DateTimeKind.Utc);
            var timeSpan = this.Expires - DateTime.UtcNow;

            // only alarm, if expires in less than 7 days and half time is reached
            return timeSpan.Days <= 7 && DateTime.UtcNow > halfTime;
        }
    }
}
