using E.DataLinq.Core.Services.Abstraction;
using E.Standard.Cms.Configuration.Services;
using E.Standard.Extensions.Compare;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Cms.AppCode.Services;

public class UrlHelperService : IHostUrlHelper
{
    private readonly HttpRequest _request;
    private readonly CmsConfigurationService _cmsConfig;

    public UrlHelperService(IHttpContextAccessor httpContextAccessor,
                            CmsConfigurationService cmsConfig)
    {
        if (httpContextAccessor?.HttpContext != null)
        {
            _request = httpContextAccessor.HttpContext.Request;
        }
        _cmsConfig = cmsConfig;
    }

    public string AppRootUrl(ControllerBase controller, bool forceHttps = true)
    {
        string url =
            _cmsConfig?.Instance?.CmsDisplayUrl.OrTake(
            String.Format("{0}{1}{2}", UrlScheme(controller.Request, forceHttps), DisplayUrl(controller.Request).Authority, controller.Url.Content("~")));

        if (url.EndsWith("/"))
        {
            return url.Substring(0, url.Length - 1);
        }

        return url;
    }

    private string UrlScheme(HttpRequest request, bool forceHttps = true)
    {
        return (forceHttps ? "https" : DisplayUrl(request).Scheme) + "://";
    }

    private Uri DisplayUrl(HttpRequest request)
    {
        return new Uri(Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(request));
    }

    #region IHostUrlHelper

    public string HostAppRootUrl()
    {
        string diplayUrl = _cmsConfig?.Instance?.CmsDisplayUrl;

        if (!String.IsNullOrEmpty(diplayUrl))
        {
            return diplayUrl;
        }

        var host = _request?.Host.ToUriComponent();
        var pathBase = _request?.PathBase.ToUriComponent();

        return $"{_request?.Scheme}://{host}{pathBase}";
    }

    #endregion
}
