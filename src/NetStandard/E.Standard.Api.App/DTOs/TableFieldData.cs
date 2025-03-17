using E.Standard.Extensions;
using E.Standard.Extensions.IO;
using E.Standard.Json;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.CMS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace E.Standard.Api.App.DTOs;

public sealed class TableFieldData : TableFieldDTO
{
    public string FieldName { get; set; }

    private string _simpleDomainValue = null;
    private DateTime _simpleDomainNextRefresh = DateTime.UtcNow;
    private const double SimpleDomainRefreshSeconds = 30.0;

    private ConcurrentDictionary<string, string> _simpleDomains = null;

    public string SimpleDomains
    {
        set
        {
            if (!value.IsValidHttpUrl() &&
                value != null && value.Contains("="))
            {
                _simpleDomains = new ConcurrentDictionary<string, string>();

                int pos = value.IndexOf("=");
                string[] left = value.Substring(0, pos).Trim().Split(',');
                string[] right = value.Substring(pos + 1).Trim().Split(',');

                for (int i = 0; i < left.Length; i++)
                {
                    _simpleDomains.TryAdd(left[i].Trim(),
                                    right.Length > i ? right[i].Trim() : left[i].Trim());
                }
            }
            else
            {
                _simpleDomainValue = value;
            }
        }
    }

    async public override Task InitRendering(IHttpService httpService)
    {
        if (httpService != null &&
            _simpleDomainValue.IsValidHttpUrl() &&
            _simpleDomainNextRefresh < DateTime.UtcNow)
        {
            try
            {
                var httpResponse = await httpService.GetStringAsync(_simpleDomainValue);

                var jsonObjectArray = JSerializer.Deserialize<object[]>(httpResponse);
                string valueProperty = "value";
                string labelProperty = "name";

                var domains = new ConcurrentDictionary<string, string>();

                foreach (var domain in jsonObjectArray)
                {
                    if (JSerializer.IsJsonElement(domain))
                    {
                        domains.TryAdd(JSerializer.GetJsonElementValue(domain, valueProperty).ToStringOrEmpty(),
                                       JSerializer.GetJsonElementValue(domain, labelProperty).ToStringOrEmpty());
                    }
                }

                _simpleDomains = domains;
            }
            catch { }
            finally { _simpleDomainNextRefresh = DateTime.UtcNow.AddSeconds(SimpleDomainRefreshSeconds); }
        }
    }

    public bool RawHtml { get; set; }

    public ColumnType ColType { get; set; }

    public string SortingAlgorithm { get; set; }

    public FieldAutoSortMethod AutoSort { get; set; }

    public override string RenderField(WebMapping.Core.Feature feature, NameValueCollection requestHeaders)
    {
        string val = feature[this.FieldName];
        if (String.IsNullOrEmpty(val))
        {
            return String.Empty;
        }

        if (ColType == ColumnType.EmailAddress)
        {
            return "<a href='mailto:" + val + "'>" + val + "</a>";
        }
        else if (ColType == ColumnType.PhoneNumber)
        {
            return "<a href='tel:" + val + "'>" + val + "</a>";
        }

        if (_simpleDomains != null && _simpleDomains.ContainsKey(val))
        {
            val = _simpleDomains[val];
        }

        if (RawHtml)
        {
            return val;
        }

        return val.Replace(">", "&gt;").Replace("<", "&lt;");
    }

    public override IEnumerable<string> FeatureFieldNames
    {
        get
        {
            return new string[] { this.FieldName };
        }
    }
}
