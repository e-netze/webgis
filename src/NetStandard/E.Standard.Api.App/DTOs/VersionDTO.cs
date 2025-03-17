using E.Standard.WebGIS.Core.Models.Abstraction;
using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs;

public class VersionDTO : IWatchable
{
    public string version { get { return "1.0"; } }
    public int milliseconds { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string WatchId { get; set; }
}