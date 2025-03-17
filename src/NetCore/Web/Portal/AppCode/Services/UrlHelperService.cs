using E.Standard.Configuration.Services;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Portal.Core.AppCode.Configuration;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Reflection;
using System;
using System.Reflection;

namespace Portal.Core.AppCode.Services;

public class UrlHelperService
{
    private readonly ConfigurationService _config;
    private readonly IWebHostEnvironment _environment;

    public UrlHelperService(ConfigurationService config, IWebHostEnvironment environment)
    {
        _config = config;
        _environment = environment;
    }

    public string ApiUrl(HttpRequest request, HttpSchema httpSchema = HttpSchema.Default)
    {
        var url = _config.ApiUrl()
                         .ReplaceUrlPlaceholders(request,
                                                 (placeholder) =>
                                                 _config[PortalConfigKeys.ToKey(placeholder.ToLower().Split('/')[0])]);

        if (url.ToLower().StartsWith("http://"))
        {

            if ((httpSchema == HttpSchema.Https ||
                (httpSchema == HttpSchema.Current && request.Uri().Scheme == "https")))
            {
                url = "https" + url.Substring(4, url.Length - 4);
            }
        }

        return url;
    }

    public string ApiInternalUrl(HttpRequest request)
    {
        string url = _config.ApiUrlInternal();

        return String.IsNullOrWhiteSpace(url) ? ApiUrl(request, HttpSchema.Default) : url;
    }

    public string PortalUrl()
    {
        string url = _config.PortalUrl();

        return url;
    }

    public string UrlScheme(HttpRequest request, bool forceHttps = true)
    {
        if (forceHttps && _config.UseLocalUrlSchema())
        {
            forceHttps = false;
        }

        return (forceHttps ? "https" : request.Uri().Scheme) + "://";
    }

    public string AppRootUrl(HttpRequest request, PortalBaseController urlHelper, bool forceHttps = true)
    {
        string url = AppRootUrlFromConfig(request, forceHttps);
        if (!String.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        url = String.Format("{0}{1}{2}", UrlScheme(request, forceHttps), request.Uri().Authority, urlHelper.UrlContent("~"));
        if (url.EndsWith("/"))
        {
            return url.Substring(0, url.Length - 1);
        }

        return url;
    }

    public string AppRootUrlFromConfig(HttpRequest request, bool forceHttps)
    {
        if (forceHttps && _config.UseLocalUrlSchema())
        {
            forceHttps = false;
        }

        string url = _config.PortalUrl()
                            .ReplaceUrlPlaceholders(request,
                                                    (placeholder) =>
                                                    _config[PortalConfigKeys.ToKey(placeholder.ToLower().Split('/')[0])]);

        if (!String.IsNullOrWhiteSpace(url))
        {
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

    public string AppRootPath()
    {
        return _environment.ContentRootPath.WithoutEndingSlashesAndBackSlasches();
    }

    public (string controllerName, string actionName) GetEndpointInfo(HttpContext context)
    {
        var displayUrl = context.Request.Path;

        string controllerName = String.Empty,
               actionName = String.Empty;

        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var controllerActionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (controllerActionDescriptor != null)
            {
                controllerName = controllerActionDescriptor.ControllerName;
                actionName = controllerActionDescriptor.ActionName;
            }
        }

        return (controllerName, actionName);
    }

    public bool TargetsAuthorizedEndPoint(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var controllerActionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (controllerActionDescriptor != null)
            {
                // zB AuthController.LoginAD
                var authorizeAttribute = controllerActionDescriptor.GetCustomAttribute<AuthorizeAttribute>();

                if (authorizeAttribute != null)
                {
                    return true;
                }

                // zB HMAC.Index
                var authorizeEndpointAttribute = controllerActionDescriptor.GetCustomAttribute<AuthorizeEndpointAttribute>();

                if (authorizeEndpointAttribute != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public string WWWRootPath()
    {
        return _environment.WebRootPath;
    }
}
