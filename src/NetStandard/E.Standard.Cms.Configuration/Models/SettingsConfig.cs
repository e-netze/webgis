using Newtonsoft.Json;

namespace E.Standard.Cms.Configuration.Models;

public class SettingsConfig
{
    [JsonProperty("proxy")]
    [System.Text.Json.Serialization.JsonPropertyName("proxy")]
    public ProxyConfig Proxy { get; set; }

    [JsonProperty("logging_connection_string")]
    [System.Text.Json.Serialization.JsonPropertyName("logging_connection_string")]
    public string LoggingConnectionString { get; set; }

    public SettingsConfig AddDefaults()
    {
        if (this.Proxy == null)
        {
            this.Proxy = new ProxyConfig() { Use = false };
        }

        return this;
    }

    #region Static Members

    public static SettingsConfig Defaults => new SettingsConfig()
    {
        Proxy = new ProxyConfig() { Use = false }
    };

    #endregion

    #region Classes

    public class ProxyConfig
    {
        [JsonProperty("use")]
        [System.Text.Json.Serialization.JsonPropertyName("use")]
        public bool Use { get; set; }

        [JsonProperty("server")]
        [System.Text.Json.Serialization.JsonPropertyName("server")]
        public string Server { get; set; }

        [JsonProperty("port")]
        [System.Text.Json.Serialization.JsonPropertyName("port")]
        public int Port { get; set; }

        [JsonProperty("user")]
        [System.Text.Json.Serialization.JsonPropertyName("user")]
        public string User { get; set; }

        [JsonProperty("password")]
        [System.Text.Json.Serialization.JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonProperty("domain")]
        [System.Text.Json.Serialization.JsonPropertyName("domain")]
        public string Domain { get; set; }

        [JsonProperty("ignore")]
        [System.Text.Json.Serialization.JsonPropertyName("ignore")]
        public string Ignore { get; set; }
    }

    #endregion
}
