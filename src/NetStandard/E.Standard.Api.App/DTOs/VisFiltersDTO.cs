using E.Standard.Api.App.Models.Abstractions;
using Newtonsoft.Json;
using System.Linq;

namespace E.Standard.Api.App.DTOs;

public sealed class VisFiltersDTO : VersionDTO, IHtml
{
    public VisFilterDTO[] filters { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasLockedFilters
    {
        get
        {
            return filters != null && filters.Where(f => f.FilterType == WebGIS.CMS.VisFilterType.locked).Count() > 0;
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public VisFilterDTO[] LockedFilters
    {
        get
        {
            return filters != null ? filters.Where(f => f.FilterType == WebGIS.CMS.VisFilterType.locked).ToArray() : new VisFilterDTO[0];
        }
    }

    #region IHtml Member

    public string ToHtmlString()
    {
        return HtmlHelper.ToList(this.filters, "Filters");
    }

    #endregion
}