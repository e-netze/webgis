using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace E.Standard.Configuration.Providers;

public class HostingEnvironmentJsonConfigurationProvider : ConfigurationProvider
{
    public override void Load()
    {
        var appConfiguration = new JsonAppConfiguration($"env-{Environment.MachineName}.config");

        if (appConfiguration.Exists)
        {
            var jsonConfig = appConfiguration.Deserialize<JsonConfig>();

            if (jsonConfig.Names != null)
            {
                this.Data = new Dictionary<string, string>();

                foreach (string dictionaryKey in jsonConfig.Names.Keys)
                {
                    string key = $"hostingenvironment-{Environment.MachineName}:{dictionaryKey}";
                    string val = jsonConfig.Names[dictionaryKey];

                    if (!Data.ContainsKey(key))
                    {
                        Data.Add(key, val);
                    }
                }
            }
        }
    }

    #region JsonClass

    private class JsonConfig
    {
        [JsonProperty("names")]
        [System.Text.Json.Serialization.JsonPropertyName("names")]
        public Dictionary<string, string> Names { get; set; }
    }

    #endregion
}
