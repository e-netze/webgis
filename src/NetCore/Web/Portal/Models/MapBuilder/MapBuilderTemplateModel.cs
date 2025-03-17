using E.Standard.Json;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Portal.Core.Models.MapBuilder;

public class MapBuilderTemplateModel
{
    public string ClientId { get; set; }
    public string Extent { get; set; }
    public string Services { get; set; }
    public string UI { get; set; }
    public string Queries { get; set; }
    public string Tools { get; set; }
    public string ToolsQuickAccess { get; set; }
    public string ApiUrl { get; set; }
    public string ApiUrlHttps { get; set; }
    public string DynamicContent { get; set; }

    public string Graphics { get; set; }

    public double MapScale { get; set; }
    public double[] MapCenter { get; set; }

    public ServiceLayerVisibility[] Visibilities { get; set; }

    private bool HasUIItem(string item)
    {
        return this.UI.ToLower().Split(',').Contains(item.ToLower());
    }
    private bool HasUIItem(string[] items)
    {
        foreach (var item in items)
        {
            if (HasUIItem(item))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasUISearch
    {
        get
        {
            return HasUIItem("search");
        }
    }

    public string UISearchService
    {
        get
        {
            if (!HasUISearch)
            {
                return String.Empty;
            }

            foreach (var item in this.UI.ToLower().Split(','))
            {
                if (item.StartsWith("search-service-"))
                {
                    return item.Substring(15, item.Length - 15);
                }
            }

            return String.Empty;
        }
    }

    public bool HasUIDetailSearch
    {
        get
        {
            return HasUIItem("detailsearch");
        }
    }

    public bool HasUIAppMenu
    {
        get
        {
            return HasUIItem("appmenu");
        }
    }

    public bool HasUITabs
    {
        get
        {
            return HasUIItem(new string[] { "tabs-presentation", "tabs-presentation-addservices", "tabs-queryresults", "tabs-tools", "tabs-settings", "tabs-settings-addservice" });
        }
    }

    public bool HasUIPresentationsTab
    {
        get { return HasUIItem(new string[] { "tabs-presentation", "tabs-presentation-addservices" }); }
    }
    public bool HasUIPresentationsTabAddService
    {
        get { return HasUIItem("tabs-presentation-addservices"); }
    }
    public bool HasUIQueryResultTab
    {
        get { return HasUIItem("tabs-queryresults"); }
    }
    public bool HasUIToolsTab
    {
        get { return HasUIItem("tabs-tools"); }
    }
    public bool HasUISettingsTab
    {
        get { return HasUIItem(new string[] { "tabs-settings", "tabs-settings-addservice" }); }
    }
    public bool HasUISettingsTabAddService
    {
        get { return HasUIItem("tabs-settings-addservice"); }
    }

    public bool HasUISettingsTabThemes
    {
        get { return HasUIItem("tabs-settings-themes"); }
    }

    #region Classes

    public class ServiceLayerVisibility
    {
        [JsonProperty("id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string ServiceId { get; set; }

        [JsonProperty("layers")]
        [System.Text.Json.Serialization.JsonPropertyName("layers")]
        public string[] VisibleLayers { get; set; }

        [JsonProperty("isbasemap")]
        [System.Text.Json.Serialization.JsonPropertyName("isbasemap")]
        public bool IsBasemap { get; set; }

        [JsonProperty("isoverlaybasemap")]
        [System.Text.Json.Serialization.JsonPropertyName("isoverlaybasemap")]
        public bool IsOverlayBasemap { get; set; }

        [JsonProperty("opacity")]
        [System.Text.Json.Serialization.JsonPropertyName("opacity")]
        public double Opacity { get; set; }

        public string VisibleLayersToJson()
        {
            if (this.VisibleLayers == null)
            {
                return "null";
            }

            return JSerializer.Serialize(this.VisibleLayers);
        }
    }

    #endregion
}
