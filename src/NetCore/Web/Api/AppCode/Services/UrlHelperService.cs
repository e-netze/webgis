using Api.Core.AppCode.Extensions;
using E.DataLinq.Core.Services.Abstraction;
using E.Standard.Api.App.Configuration;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Models;
using E.Standard.Configuration.Services;
using E.Standard.Platform;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.Api.Abstractions;
using E.Standard.WebGIS.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.IO;
using System.Text;

namespace Api.Core.AppCode.Services;

public class UrlHelperService : IUrlHelperService,
                                IHostUrlHelper
{
    private readonly ConfigurationService _config;
    private readonly ApiConfigurationService _apiConfig;
    private readonly IWebHostEnvironment _environment;
    private readonly HttpRequest _request;

    public UrlHelperService(IHttpContextAccessor httpContextAccessor,
                            ConfigurationService config,
                            ApiConfigurationService apiConfig,
                            IWebHostEnvironment environment,
                            LinkGenerator linkGenerator)
    {
        _config = config;
        _apiConfig = apiConfig;
        _environment = environment; ;

        if (httpContextAccessor?.HttpContext != null)
        {
            string absolutePath = new Uri(linkGenerator.GetUriByAction(httpContextAccessor.HttpContext, "index", "home")).AbsolutePath;
            this.VirtualPath = absolutePath;

            _request = httpContextAccessor.HttpContext.Request;
        }
    }

    public string VirtualPath { get; }

    public string UrlScheme(HttpSchema httpSchema = HttpSchema.Default)
    {
        switch (httpSchema)
        {
            case HttpSchema.Https:
                return "https://";
            case HttpSchema.Empty:
                return "//";
            case HttpSchema.Default:
            case HttpSchema.Current:
            default:
                return _request.Uri().Scheme + "://";
        }
    }

    //public string AppRootUrl(ApiBaseController urlHelper, HttpSchema httpSchema = HttpSchema.Default)
    //{
    //    string url = AppRootUrlFromConfig(httpSchema == HttpSchema.Https);
    //    if (!String.IsNullOrWhiteSpace(url))
    //    {
    //        return url;
    //    }

    //    url = String.Format("{0}{1}{2}", UrlScheme(httpSchema), request.Uri().Authority, urlHelper.UrlContent("~"));
    //    if (url.EndsWith("/"))
    //    {
    //        return url.Substring(0, url.Length - 1);
    //    }

    //    return url;
    //}

    public string AppRootUrl(HttpSchema httpSchema = HttpSchema.Default)
    {
        string url = AppRootUrlFromConfig(httpSchema == HttpSchema.Https);
        if (!String.IsNullOrWhiteSpace(url))
        {
            return url.RemoveEndingSlashes();
        }

        url = String.Format("{0}{1}{2}", UrlScheme(httpSchema), _request.Uri().Authority, VirtualPath);
        if (url.EndsWith("/"))
        {
            return url.Substring(0, url.Length - 1);
        }

        return url.RemoveEndingSlashes();
    }

    private string AppRootUrlFromConfig(bool forceHttps)
    {
        string url = _config.ApiUrl();

        if (!String.IsNullOrWhiteSpace(url))
        {
            //url = ReplaceUrlPlaceholders(url);
            url = url.ReplaceUrlPlaceholders(_request, (placeholder) =>
                                                       _config[ApiConfigKeys.ToKey(placeholder.ToLower().Split('/')[0])]);

            if (forceHttps && url.ToLower().StartsWith("http://"))
            {
                url = "https://" + url.Substring("http://".Length);
            }
            if (!url.EndsWith("/"))
            {
                url += "/";
            }
        }

        return url;
    }

    #region IHostUrlHelper (DataLinq)

    public string HostAppRootUrl()
    {
        string url = AppRootUrlFromConfig(false);
        if (!String.IsNullOrWhiteSpace(url))
        {
            return url.RemoveEndingSlashes();
        }

        var host = _request.Host.ToUriComponent();
        var pathBase = _request.PathBase.ToUriComponent();

        url = $"{_request.Scheme}://{host}{pathBase}";

        return url.RemoveEndingSlashes();
    }

    #endregion

    #region Portal

    public string PortalInternalUrl()
    {
        string url = _config.PortalInternalUrl();

        return String.IsNullOrWhiteSpace(url) ? PortalUrl() : url;
    }

    public string PortalUrl(HttpSchema httpSchema = HttpSchema.Default)
    {
        var url = _config.PortalUrl()
                         .ReplaceUrlPlaceholders(_request, (placeholder) =>
                                                            _config[ApiConfigKeys.ToKey(placeholder.ToLower().Split('/')[0])]);

        switch (httpSchema)
        {
            case HttpSchema.Https:
                if (url.ToLower().StartsWith("http://"))
                {
                    url = "https" + url.Substring(4, url.Length - 4);
                }

                break;
            case HttpSchema.Current:
                if (url.IndexOf("//") > 0)
                {
                    url = url.Substring(url.IndexOf("//"));
                }

                break;
        }

        return url;
    }

    public string OutputUrl()
    {
        return _config.OutputUrl()
                      .ReplaceUrlPlaceholders(_request, (placeholder) =>
                                                        _config[ApiConfigKeys.ToKey(placeholder.ToLower().Split('/')[0])]);
    }

    #endregion

    #region Paths

    public string WWWRootPath()
    {
        return _environment.WebRootPath;
    }

    public string AppRootPath()
    {
        return _environment.ContentRootPath;
    }

    public string ServerSideConfigurationPath()
    {
        var serverSideConfigurationPath = _apiConfig.ServiceSideConfigurationPath;
        if (String.IsNullOrWhiteSpace(serverSideConfigurationPath))
        {
            serverSideConfigurationPath = new DirectoryInfo(AppRootPath()).Parent.FullName;
        }

        return serverSideConfigurationPath;
    }

    public string AppEtcPath()
    {
        return $"{ServerSideConfigurationPath()}/etc";
    }

    public string AppConfigPath()
    {
        return $"{ServerSideConfigurationPath()}/config";
    }

    public bool EndpointDatalinqCssExists(string endpointId)
    {
        return File.Exists($"{WWWRootPath()}/content/datalinq/{endpointId}/datalinq.css".ToPlatformPath());
    }

    public string OutputPath()
    {
        return _apiConfig.OutputPath;
    }

    #endregion

    #region MapBuilder

    public string MapBuilderUrl(string portalId = "")
    {
        if (String.IsNullOrWhiteSpace(portalId))
        {
            portalId = "~";
        }

        return $"{PortalUrl()}/{portalId}/mapbuilder";
    }

    #endregion

    #region Gdi

    #region GDI

    const string GdiSchemeParameter = "__gdi";
    public string GetCustomGdiScheme()
    {
        if (_request != null)
        {
            if (!String.IsNullOrWhiteSpace(_request.Query[GdiSchemeParameter]))
            {
                return _request.Query[GdiSchemeParameter];
            }

            if (_request.HasFormContentType && _request.Form != null &&
                !String.IsNullOrWhiteSpace(_request.Form[GdiSchemeParameter]))
            {
                return _request.Form[GdiSchemeParameter];
            }
        }

        return _config.GdiSchemeDefault();
    }

    #endregion

    #endregion

    #region Hmac

    public string HMacUrl(HttpContext httpContext)
    {
        StringBuilder url = new StringBuilder(), urlParameters = new StringBuilder();
        url.Append($"{PortalUrl(HttpSchema.Current)}/hmac");

        if (!String.IsNullOrWhiteSpace(httpContext.Request.Query["_portal_security"]))
        {
            urlParameters.Append($"security={httpContext.Request.Query["_portal_security"].ToString()}");
        }
        if (!String.IsNullOrWhiteSpace(httpContext.Request.Query["_portal_security_token"]))
        {

            urlParameters.Append($"{(urlParameters.Length > 0 ? "&" : "")}token={httpContext.Request.Query["_portal_security_token"].ToString()}");
        }

        if (urlParameters.Length > 0)
        {
            url.Append("?");
            url.Append(urlParameters);
        }

        return url.ToString();
    }

    #endregion

    #region Urls / Paths

    public ApiPathAndUrlCollection GetApiPathsAndUrlCollection()
    {
        return new ApiPathAndUrlCollection()
        {
            AppRootPath = this.AppRootPath(),
            WWWRootPath = this.WWWRootPath(),
            AppConfigPath = this.AppConfigPath(),
            AppEtcPath = this.AppEtcPath(),

            OutputUrl = this.OutputUrl(),
            OutputPath = this.OutputPath(),

            WebgisPortalUrl = this.PortalUrl(HttpSchema.Default),
            AppRootUrl = this.AppRootUrl(HttpSchema.Default)
        };
    }

    #endregion
}
