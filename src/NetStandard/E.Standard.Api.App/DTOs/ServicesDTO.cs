using E.Standard.Api.App.Models.Abstractions;
using E.Standard.Api.App.Services.Cache;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class ServicesDTO : VersionDTO, IHtml3
{
    public ServiceDTO[] services { get; set; }

    #region IHtml Member

    public string ToHtmlString() => ToHtmlString(null);

    public string ToHtmlString(CacheService cache)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<h2>Services</h2>");

        if (services != null)
        {
            Dictionary<string, List<ServiceDTO>> dict = new Dictionary<string, List<ServiceDTO>>();
            foreach (var service in services)
            {
                string cmsName = "CMS";
                if (service.id.Contains("@"))
                {
                    cmsName = service.id.Split('@')[1];
                }

                if (!dict.ContainsKey(cmsName))
                {
                    dict.Add(cmsName, new List<ServiceDTO>());
                }

                dict[cmsName].Add(service);
            }
            foreach (string cmsName in dict.Keys)
            {
                sb.Append("<h3>" + cmsName + "</h3>");
                sb.Append(HtmlHelper.ToList(dict[cmsName].ToArray(), cache: cache));
            }
        }

        return sb.ToString();
    }

    #endregion
}