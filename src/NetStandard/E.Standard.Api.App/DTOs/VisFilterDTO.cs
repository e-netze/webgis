using E.Standard.Api.App.Models.Abstractions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core.Api.Bridge;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class VisFilterDTO : VersionDTO, IHtml, IVisFilterBridge, ILookup
{
    public VisFilterDTO()
    {
        Lookups = new List<LookupDef>();
    }

    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "visible")]
    [System.Text.Json.Serialization.JsonPropertyName("visible")]
    public bool Visible { get; set; }

    //[JsonProperty(PropertyName = "layernames")]
    [System.Text.Json.Serialization.JsonPropertyName("layernames")]
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string[] LayerNames => this.LayerNamesString?.Split(';').ToArray() ?? new string[0];

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool SetLayersVisible { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Dictionary<string, string> Parameters
    {
        get
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();

            string[] keyParameters = WebGIS.CMS.Globals.KeyParameters(this.Filter)?
                                                       .Distinct()
                                                       .ToArray();
            if (keyParameters != null)
            {
                foreach (var keyParameter in keyParameters)
                {
                    ret.Add(keyParameter, keyParameter);
                }
            }

            return ret;
        }
    }

    public WebMapping.Core.Api.LookupType LookupType(string parameter)
    {
        if (this.Lookups == null || this.Lookups.Count() == 0)
        {
            return WebMapping.Core.Api.LookupType.None;
        }

        var lookup = this.Lookups.Where(l => l.Parameter == parameter).FirstOrDefault();
        if (lookup != null)
        {
            switch (lookup.Type)
            {
                case Lookuptype.Autocomplete:
                    return WebMapping.Core.Api.LookupType.Autocomplete;
                case Lookuptype.ComboBox:
                    return WebMapping.Core.Api.LookupType.ComboBox;
            }
        }

        return WebMapping.Core.Api.LookupType.None;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string LayerNamesString { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string Filter { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public VisFilterType FilterType { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public IEnumerable<LookupDef> Lookups { get; }

    public void AddLookup(LookupDef lookup)
    {
        ((List<LookupDef>)this.Lookups).Add(lookup);
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string LookupLayerNameString { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string LookupLayerName
    {
        get
        {
            if (!String.IsNullOrEmpty(this.LookupLayerNameString))
            {
                return this.LookupLayerNameString;
            }

            if (this.LayerNames.Count() == 1)
            {
                return this.LayerNames[0];
            }

            return null;
        }

    }

    #region IHtml Member

    public string ToHtmlString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(HtmlHelper.ToTable(
            new string[] { "Name", "ThemeId", "Layer Names" },
            new object[] { this.Name, this.Id, this.LayerNamesString }
        ));

        return sb.ToString();
    }

    #region ILookup

    public ILookupConnection GetLookupConnection(string parameter)
    {
        if (this.Lookups == null)
        {
            return null;
        }

        return this.Lookups.Where(l => l.Parameter == parameter).FirstOrDefault();
    }

    #endregion

    #endregion

    #region Classes

    public class LookupDef : ILookupConnection
    {
        public Lookuptype Type { get; set; }
        public string Parameter { get; set; }
        public string ConnectionString { get; set; }
        public string SqlClause { get; set; }
    }

    #endregion
}