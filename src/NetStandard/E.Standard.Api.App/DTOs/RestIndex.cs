using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Models.Abstractions;
using E.Standard.Api.App.Services.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace E.Standard.Api.App.DTOs;

public sealed class IndexDTO : VersionDTO, IHtml3
{
    public IndexDTO()
    {

    }

    public string name { get; set; }
    public FolderDTO[] folders { get; set; }

    public CmsItemDTO[] cmsitems { get; set; }

    public bool cms_iscorrupt { get; set; }

    public string cms_errormessage { get; set; }

    public IEnumerable<string> cache_warnings { get; set; }

    #region IHtml Member

    public string ToHtmlString() => ToHtmlString(null);

    public string ToHtmlString(CacheService cache)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(HtmlHelper.ToList(folders, name));

        sb.Append(HtmlHelper.ToHeader("CACHE", HtmlHelper.HeaderType.h2));
        sb.Append(HtmlHelper.Text("Cache: " + (cms_iscorrupt == true ? "corrupt" : "ok")));

        if (cache != null)
        {
            var headers = new[] { "Service", "Status", "Since (UTC)", "Layers", "Diagnostic" };
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

            foreach (var service in cache.GetServices(null)
                                         .Select(s => cache.GetOriginalServiceIfInitialized(s.Url, null))
                                         .Where(s => s != null)
                                         .OrderBy(s => s.Layers == null ? 0 : s.Layers.Count))
            {
                var row = new Dictionary<string, object>();
                row["Service"] = service.Url;
                row["Status"] = service.HasInitialzationErrors() ? "Corrupt" : "Ok";
                row["Since (UTC)"] = cache.ServiceIntializationTimeUtc(service.Url);
                row["Layers"] = service.Layers.Count;
                row["Diagnostic"] = service.ErrorAndDiagnosticMessage();

                rows.Add(row);
            }

            if (rows.Count > 0)
            {
                sb.Append(HtmlHelper.ToTable(headers, rows));
            }
        }

        if (!String.IsNullOrWhiteSpace(this.cms_errormessage))
        {
            sb.Append(HtmlHelper.ErrorMessage($"Cache Errormessage: {HttpUtility.HtmlEncode(this.cms_errormessage)}"));
        }
        sb.Append(HtmlHelper.ToList(cmsitems, "CMS"));

        if (this.cache_warnings != null)
        {
            sb.Append("<string>Warings</string></br>");
            foreach (var warning in this.cache_warnings)
            {
                sb.Append(HtmlHelper.WarningMessage(HttpUtility.HtmlEncode(warning)));
            }
        }

        return sb.ToString();
    }

    #endregion
}