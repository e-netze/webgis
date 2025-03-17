using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonSpatialReference
{
    public JsonSpatialReference() { }

    public JsonSpatialReference(int id)
    {
        this.Wkid = id;
    }

    [JsonProperty("wkt")]
    [System.Text.Json.Serialization.JsonPropertyName("wkt")]
    public string Wkt { get; set; }

    [JsonProperty("wkid")]
    [System.Text.Json.Serialization.JsonPropertyName("wkid")]
    public int Wkid { get; set; }

    [JsonProperty("latestWkid")]
    [System.Text.Json.Serialization.JsonPropertyName("latestWkid")]
    public int LatestWkid { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public int EpsgCode
    {
        get
        {
            if ((this.Wkid >= 100000 || this.Wkid <= 0) && this.LatestWkid > 0)
            {
                return LatestWkid;
            }

            return this.Wkid;
        }
    }
}
