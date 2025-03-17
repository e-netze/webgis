using E.Standard.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Core.Models;

public class MapDefinitionUiDTO : MapDefinitionDTO
{
    [JsonProperty("ui")]
    [System.Text.Json.Serialization.JsonPropertyName("ui")]
    public UIClass UI { get; set; }

    #region Methods

    public bool HasPrintLayout(string layoutId)
    {
        var options = this.UI?.Options?.Where(o => "tabs".Equals(o.Element)).FirstOrDefault();

        if (!String.IsNullOrEmpty(options?.Options?.ToString()))
        {
            var tabsOptions = JSerializer.Deserialize<TabsOptions>(options.Options.ToString());

            var printLayoutContainer = tabsOptions?.OptionsTools?.Containers?.Where(c => "webgis.tools.io.print".Equals(c.Name)).FirstOrDefault();
            if (printLayoutContainer?.Options != null)
            {
                return printLayoutContainer.Options.Contains(layoutId);
            }
        }

        return false;
    }

    #endregion

    #region Classes

    public class UIClass
    {
        [JsonProperty("options")]
        [System.Text.Json.Serialization.JsonPropertyName("options")]
        public IEnumerable<UIOptions> Options { get; set; }
    }

    public class UIOptions
    {
        [JsonProperty("element")]
        [System.Text.Json.Serialization.JsonPropertyName("element")]
        public string Element { get; set; }

        [JsonProperty("selector")]
        [System.Text.Json.Serialization.JsonPropertyName("selector")]
        public string Selector { get; set; }

        [JsonProperty("options")]
        [System.Text.Json.Serialization.JsonPropertyName("options")]
        public object Options { get; set; }
    }

    public class TabsOptions
    {
        [JsonProperty("options_tools")]
        [System.Text.Json.Serialization.JsonPropertyName("options_tools")]
        public TabOptionsTools OptionsTools { get; set; }
    }

    public class TabOptionsTools
    {
        [JsonProperty("containers")]
        [System.Text.Json.Serialization.JsonPropertyName("containers")]
        public IEnumerable<TabOptionsToolsContainer> Containers { get; set; }
    }

    public class TabOptionsToolsContainer
    {
        [JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("tools")]
        [System.Text.Json.Serialization.JsonPropertyName("tools")]
        public string[] Tools { get; set; }

        [JsonProperty("options")]
        [System.Text.Json.Serialization.JsonPropertyName("options")]
        public string[] Options { get; set; }
    }

    #endregion
}
