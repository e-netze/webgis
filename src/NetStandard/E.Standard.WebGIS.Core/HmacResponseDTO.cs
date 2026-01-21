using Newtonsoft.Json;
using System.Text;

namespace E.Standard.WebGIS.Core;

public class HmacResponseDTO
{
    static public HmacVersion CurrentVersion = HmacVersion.V1;

    public HmacResponseDTO()
    {
        version = (int)CurrentVersion;
    }

    public bool success { get; set; }
    public bool use { get; set; }
    public string privateKey { get; set; }
    public string publicKey { get; set; }
    public long ticks { get; set; }
    public string username { get; set; }
    public int? favstatus { get; set; }  // null..wird nicht verwendet,0..nachfragen, 1..nimmt bereits teil, 2..möchte nicht teilnehmen
    public int version { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string authEndpoint { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] userroles { get; set; }

    public string ToJs()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("{");
        sb.Append("success:" + success.ToString().ToLower());
        sb.Append(",use:" + use.ToString().ToLower());
        if (use)
        {
            sb.Append(",privateKey:'" + privateKey + "'");
            sb.Append(",publicKey:'" + publicKey + "'");
            sb.Append(",ticks:" + ticks);
            sb.Append(",username:'" + username.Replace(@"\", @"\\") + "'");
            if (favstatus.HasValue)
            {
                sb.Append(",favstatus:" + favstatus.Value.ToString());
            }

            sb.Append($",version:{version}");
        }
        sb.Append("}");

        return sb.ToString();
    }
}
