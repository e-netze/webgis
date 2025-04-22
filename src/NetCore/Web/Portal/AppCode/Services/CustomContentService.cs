using E.Standard.Configuration.Services;
using E.Standard.Platform;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.AspNetCore.Http;
using Portal.Core.AppCode.Configuration;
using Portal.Core.AppCode.Extensions;
using System;
using System.IO;

namespace Portal.Core.AppCode.Services;

public class CustomContentService
{
    private readonly ConfigurationService _config;
    private readonly UrlHelperService _urlHelper;
    private readonly ICryptoService _crypto;
    private readonly HttpRequest _request;

    public CustomContentService(ConfigurationService config,
                                UrlHelperService urlHelper,
                                ICryptoService crypto,
                                IHttpContextAccessor httpContextAccessor)
    {
        _config = config;
        _urlHelper = urlHelper;
        _crypto = crypto;
        _request = httpContextAccessor?.HttpContext?.Request;

        PortalCustomContentRootPath = config[PortalConfigKeys.PortalCustomContentRootPath];
    }

    public string PortalCustomContentRootPath { get; }

    public string CustomScriptContent(string pageId, string username, string contentName, string version = "")
    {
        if (String.IsNullOrWhiteSpace(version))
        {
            version = WebGISVersion.CssVersion;
        }

        string baseUri = _urlHelper.AppRootUrlFromConfig(_request, false),
               path = $"customcontent/{pageId}/load?c={contentName}&t={TempPortalToken(pageId, username)}&v={version}";

        if (String.IsNullOrEmpty(baseUri))
        {
            return $"/{path}";
        }

        return new Uri(new Uri(baseUri), path).ToString();
    }

    public string TempPortalToken(string pageId, string username)
    {
        return _crypto.StaticDefaultEncrypt($"{pageId};{username}", resultStringType: CryptoResultStringType.Hex);
    }

    public bool PageMapDefaultCssExists(string pageId)
    {
        return File.Exists((_urlHelper.WWWRootPath() + "/content/portals/" + pageId + "/map-default.css").ToPlatformPath());
    }

    public string PageCustomJsVersion(string pageId)
    {
        var path = String.IsNullOrWhiteSpace(PortalCustomContentRootPath) ?
            ($"{_urlHelper.WWWRootPath()}/scripts/portals/{pageId}/custom.js").ToPlatformPath() :
            ($"{PortalCustomContentRootPath}/{pageId}/custom.js").ToPlatformPath();

        var fi = new FileInfo(path);
        if (fi.Exists)
        {
            return fi.LastWriteTimeUtc.Ticks.ToString();
        }

        return String.Empty;
    }

    public bool ApiPageDefaultCssExists(string pageId)
    {
        if (String.IsNullOrWhiteSpace(PortalCustomContentRootPath))
        {
            return true;
        }

        string path = ($"{PortalCustomContentRootPath}/{pageId}/default.css").ToPlatformPath();

        return File.Exists(path);
    }

    public string PagePortalCssVersion(string pageId)
    {
        string path = String.IsNullOrWhiteSpace(PortalCustomContentRootPath) ?
            ($"{_urlHelper.WWWRootPath()}/content/portals/{pageId}/portal.css").ToPlatformPath() :
            ($"{PortalCustomContentRootPath}/{pageId}/portal.css").ToPlatformPath();

        var fi = new FileInfo(path);
        if (fi.Exists)
        {
            return fi.LastWriteTimeUtc.Ticks.ToString();
        }

        return String.Empty;
    }

    public bool PagePortalCssExists(string pageId)
    {
        string path = String.IsNullOrWhiteSpace(PortalCustomContentRootPath) ?
            ($"{_urlHelper.WWWRootPath()}/content/portals/{pageId}/portal.css").ToPlatformPath() :
            ($"{PortalCustomContentRootPath}/{pageId}/portal.css").ToPlatformPath();

        return File.Exists(path);
    }

    public bool CompanyPortalCssExists()
    {
        return File.Exists(($"{_urlHelper.WWWRootPath()}/content/companies/{_config.Company()}/portal.css").ToPlatformPath()) ||
               File.Exists(($"{_urlHelper.WWWRootPath()}/content/companies/__default/portal.css").ToPlatformPath());
    }

    public string CompanyPortalCssFolder()
    {
        if (File.Exists(($"{_urlHelper.WWWRootPath()}/content/companies/{_config.Company()}/portal.css").ToPlatformPath()))
        {
            return _config.Company();
        }

        return "__default";
    }

    public bool PageMapBuilderCssExists(string pageId)
    {
        return File.Exists(($"{_urlHelper.WWWRootPath()}/content/portals/{pageId}/mapbuilder.css").ToPlatformPath());
    }

    public (string pageId, string username) FromTempPortalToken(string token)
    {
        var param = _crypto.StaticDefaultDecrypt(token).Split(';');

        if (param.Length != 2)
        {
            //Console.WriteLine($"Invalid Token: { new Crypto().StaticDefaultDecrypt(token) }");
            throw new Exception("Invalid token");
        }

        return (pageId: param[0], username: param[1]);
    }

    #region Metadata

    public string HtmlMetaTags()
    {
        FileInfo fi = new FileInfo($"{_urlHelper.WWWRootPath()}/_config/meta.config");

        if (fi.Exists)
        {
            return File.ReadAllText(fi.FullName);
        }

        return String.Empty;
    }

    #endregion

}
