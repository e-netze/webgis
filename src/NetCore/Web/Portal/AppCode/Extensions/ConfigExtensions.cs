using E.Standard.Configuration;
using E.Standard.Configuration.Services;
using E.Standard.Extensions.Compare;
using Microsoft.Extensions.Configuration;
using Portal.Core.AppCode.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Portal.Core.AppCode.Extensions;

static public class ConfigExtensions
{
    static public bool RegisterServiceWorker(this ConfigurationService config)
    {
        return config.Get<bool>(PortalConfigKeys.RegisterServiceWorker, false);
    }

    static public string ManifestRootUrl(this ConfigurationService config)
    {
        return config[PortalConfigKeys.ManifestRootUrl];
    }

    static public bool UseLocalUrlSchema(this ConfigurationService config)
    {
        return config.Get<bool>(PortalConfigKeys.UseLocalUrlScheme, false);
    }

    static public int ConfigCalcCrs(this ConfigurationService config)
    {
        return config.Get<int>(PortalConfigKeys.MapCalcCrs, 0);
    }

    static public string Company(this ConfigurationService config)
    {
        return config[PortalConfigKeys.Company];
    }

    static public string CompanyUrl(this ConfigurationService config)
    {
        return config[PortalConfigKeys.CompanyUrl];
    }

    static public bool UseCustomRecommendationJs(this ConfigurationService config)
    {
        return config.Get<bool>(PortalConfigKeys.UseCustomRecommendationJs, true);
    }

    static public bool UseFavoriteDetection(this ConfigurationService config)
    {
        return config.Get<bool>(PortalConfigKeys.UseFavoriteDetection, false);
    }

    static public string PortalName(this ConfigurationService config)
    {
        string portalName = config[PortalConfigKeys.PortalName];

        if (String.IsNullOrWhiteSpace(portalName))
        {
            return "webGIS Portal";
        }

        return portalName;
    }

    static public string PortalNameUrl(this ConfigurationService config)
    {
        return config[PortalConfigKeys.PortalNameUrl];
    }

    static public string DefaultPortalPageId(this ConfigurationService config)
    {
        return config[PortalConfigKeys.DefaultPortalPageId];
    }

    static public bool QueryCustomMapLayout(this ConfigurationService config)
    {
        return config.Get<bool>(PortalConfigKeys.QueryCustomMapLayout, true);
    }

    static public bool AllowSubscriberLogin(this ConfigurationService config)
    {
        return config.Get<bool>(PortalConfigKeys.AllowSubscriberLogin, false);
    }

    static public bool AllowMapUIMaster(this ConfigurationService config)
    {
        return config.Get<bool>(PortalConfigKeys.AllowMapUIMaster, false);
    }

    static public string AppCacheListPassword(this ConfigurationService config)
    {
        return config[PortalConfigKeys.AppCacheListPassword];
    }

    static public string[] SupportedLanguages(this IConfiguration config)
    {
        var supportedLanguagesString = config[PortalConfigKeys.SupportedLanguages];
        string[] supportedLanguages = null;

        if (String.IsNullOrEmpty(supportedLanguagesString))
        {
            var l10n = new DirectoryInfo("l10n");
            supportedLanguages = l10n.GetDirectories()
                                            .Select(d => d.Name)
                                            .ToArray();
        }
        else
        {
            supportedLanguages = supportedLanguagesString
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();
        }

        if (supportedLanguages?.Any() == false)
        {
            supportedLanguages = ["de"];
        }

        return supportedLanguages;
    }

    #region Security Methods/Default

    static public string DefaultSecurityMethod(this ConfigurationService config)
    {
        return config[PortalConfigKeys.DefaultSecurityMethod]?.ToLower();
    }

    static public IEnumerable<string> AllowedSecurityMethods(this ConfigurationService config)
    {
        string allowedMethodsString = config[PortalConfigKeys.AllowedSecurityMethods];

        if (String.IsNullOrWhiteSpace(allowedMethodsString))
        {
            allowedMethodsString = config.DefaultSecurityMethod();
        }

        return allowedMethodsString
                        .Split(',')
                        .Select(s => s.Trim().ToLower());
    }

    static public bool AllowAnonymousSecurityMethod(this ConfigurationService config)
    {
        if ("anonym".Equals(config.DefaultSecurityMethod(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return config.AllowedSecurityMethods().Any(m => "anonym".Equals(m, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Urls

    static public string ApiUrl(this ConfigurationService config)
    {
        return config[PortalConfigKeys.ApiUrl];
    }

    static public string ApiUrlInternal(this ConfigurationService config)
    {
        return config[PortalConfigKeys.ApiUrlInternal];
    }

    static public string PortalUrl(this ConfigurationService config)
    {
        var url = config[PortalConfigKeys.PortalUrl];
        if (String.IsNullOrEmpty(url))
        {
            url = config[PortalConfigKeys.PortalUrlOldKey];  // Compatibility to older config version...
        }

        return url;
    }

    #endregion

    #region LDAP

    static public string SecurityWindowsAuthenticationLdapDirectory(this ConfigurationService config)
    {
        return config[PortalConfigKeys.SecurityWindowsAuthenticationLdapDirectory];
    }

    static public string SecurityWindowsAuthenticationLdapFormat(this ConfigurationService config)
    {
        return config[PortalConfigKeys.SecurityWindowsAuthenticationLdapFormat];
    }

    #endregion

    #region Key Value Cache

    static public string KeyValueCacheProvider(this ConfigurationService config)
    {
        return config[PortalConfigKeys.KeyValueCacheProvider];
    }

    static public string KeyValueCacheConnectionString(this ConfigurationService config)
    {
        return config[PortalConfigKeys.KeyValueCacheConnectionString];
    }

    static public string KeyValueAsideCacheProvider(this ConfigurationService config)
    {
        return config[PortalConfigKeys.KeyValueAsideCacheProvider];
    }

    static public string KeyValueAsideCacheConnectionString(this ConfigurationService config)
    {
        return config[PortalConfigKeys.KeyValueAsideCacheConnectionString];
    }

    #endregion

    static public string SecurityClientId(this ConfigurationService config)
    {
        return config[PortalConfigKeys.SecurityClientId];
    }

    static public Version ShowNewsTipsSinceVersion(this ConfigurationService config)
    {
        try
        {
            string versionString = config[PortalConfigKeys.ShowNewsTippsSince];
            if (!String.IsNullOrEmpty(versionString))
            {
                return new Version(versionString);
            }
        }
        catch
        {

        }

        return null;
    }

    static public bool PortalPageShowOptimizationFilter(this ConfigurationService config)
    {
        return config[PortalConfigKeys.PortalPageShowOptimizationFilter]?.ToString()?.ToLower() != "false";
    }

    #region Config Files

    static public bool HasCompatibilitiesConfig(this ConfigurationService config)
    {
        string appRootPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        return new ConfigFileInfo($"{appRootPath}/_config/compatibility.config").Exists;
    }

    #endregion

    #region Help

    static public string MapViewerHelpUrl(this ConfigurationService config)
    {
        return config[PortalConfigKeys.MapViewHelpUrl].OrTake("https://docs.webgiscloud.com/manual/mapviewer/index.html");
    }

    static public string MapViewerHelpRootUrl(this ConfigurationService config)
    {
        var url = config.MapViewerHelpUrl();

        if (url.Contains("/") && (url.EndsWith(".html") || url.EndsWith(".htm")))
        {
            url = url.Substring(0, url.LastIndexOf("/") + 1);
        }

        return url;
    }

    #endregion

    #region Middleware

    static public bool UseDeChunkerMiddleware(this ConfigurationService config)
    {
        return config[PortalConfigKeys.UseDeChunkerMiddleware] == "true";
    }

    #endregion

    #region Section Header-Authentication

    static public bool UseHeaderAuthentication(this ConfigurationService config)
    {
        return config[PortalConfigKeys.HeaderAuthenticationUse] == "true";
    }

    static public string HeaderAuthenticationUsernameVariable(this ConfigurationService config)
    {
        return config[PortalConfigKeys.HeaderAuthenticationUsernameVariable];
    }

    static public string HeaderAuthenticationRolesVariable(this ConfigurationService config)
    {
        return config[PortalConfigKeys.HeaderAuthenticationRolesVariable];
    }

    static public string HeaderAuthenticationExtractRoleParameters(this ConfigurationService config)
    {
        return config[PortalConfigKeys.HeaderAuthenticationExtractRoleParameters];
    }

    static public string HeaderAuthenticationUserPrefix(this ConfigurationService config)
    {
        return config[PortalConfigKeys.HeaderAuthenticationUserPrefix];
    }

    static public string HeaderAuthenticationRolePrefix(this ConfigurationService config)
    {
        return config[PortalConfigKeys.HeaderAuthenticationRolePrefix];
    }

    static public char HeaderAuthenticationRoleSeparator(this ConfigurationService config)
    {
        string separator = config[PortalConfigKeys.HeaderAuthenticationRoleSeparator]?.Trim();

        if (String.IsNullOrEmpty(separator) || separator.Length != 1)
        {
            return ',';
        }

        return separator[0];
    }

    static public char HeaderAuthenticationRoleParametersSeparator(this ConfigurationService config)
    {
        string separator = config[PortalConfigKeys.HeaderAuthenticationRoleParametersSeparator]?.Trim();

        if (String.IsNullOrEmpty(separator) || separator.Length != 1)
        {
            return ',';
        }

        return separator[0];
    }

    static public string[] HeaderAuthenticationExtendedRoleParametersFromHeaders(this ConfigurationService config)
    {
        string roleParameters = config[PortalConfigKeys.HeaderAuthenticationExtendedRoleParametersFromHeaders];
        
        if (String.IsNullOrWhiteSpace(roleParameters))
        {
            return Array.Empty<string>();
        }
        
        return roleParameters.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(s => s.Trim())
                             .ToArray();
    }

    static public string HeaderAuthenticationExtendedRoleParametersFromHeadersPrefix(this ConfigurationService config)
    {
        return config[PortalConfigKeys.HeaderAuthenticationExtendedRoleParametersFromHeadersPrefix] ?? String.Empty;
    }

    #endregion

    #region Extended Role Parameters

    static public string ExtendedRoleParametersSource(this ConfigurationService config)
    {
        return config[PortalConfigKeys.ExtendedRoleParametersSource];
    }

    static public string ExtendedRoleParametersStatement(this ConfigurationService config)
    {
        return config[PortalConfigKeys.ExtendedRoleParametersStatement];
    }

    static public IEnumerable<string> ExtendedRoleParametersHeaders(this ConfigurationService config)
    {
        return config.GetValues<string>(PortalConfigKeys.ExtendedRoleParametersHeader);
    }

    static public bool HasExtendedRoleParametersSourceAndStatement(this IConfiguration config)
    {
        return !String.IsNullOrWhiteSpace(config[PortalConfigKeys.ExtendedRoleParametersSource]) &&
               !String.IsNullOrWhiteSpace(config[PortalConfigKeys.ExtendedRoleParametersStatement]);
    }

    static public bool HasExtendedRoleParametersHeaders(this IConfiguration config)
    {
        return !String.IsNullOrWhiteSpace(config[PortalConfigKeys.ExtendedRoleParametersHeader]);
    }

    #endregion

    static public IEnumerable<string> AddCss(this ConfigurationService config, string mapName)
    {
        var addCssValue = config.FirstValue(new string[]
        {
            $"map:{mapName}:add-css",
            "map:_all:add-css"
        });

        if (!String.IsNullOrEmpty(addCssValue))
        {
            return addCssValue.Split(',').Select(s => s.Trim());
        }

        return Array.Empty<string>();
    }

    static public IEnumerable<string> AddJs(this ConfigurationService config, string mapName)
    {
        var addJsValue = config.FirstValue(new string[]
        {
            $"map:{mapName}:add-js",
            "map:_all:add-js"
        });

        if (!String.IsNullOrEmpty(addJsValue))
        {
            return addJsValue.Split(',').Select(s => s.Trim());
        }

        return Array.Empty<string>();
    }

    #region General

    static public string FirstValue(this ConfigurationService config, IEnumerable<string> keys)
    {
        return keys
            .Select(keys => config[$"{PortalConfigKeys.ConfigurationSectionName}:{keys}"])
            .Where(value => !String.IsNullOrEmpty(value))
            .FirstOrDefault() ?? String.Empty;
    }

    #endregion

    #region Message Queue

    static public bool UseMessageQueue(this IConfiguration config)
    {
        return config[$"{PortalConfigKeys.ConfigurationSectionName}:message-queue:use"] == "true";
    }

    static public string MessageQueueConnection(this IConfiguration config)
    {
        return config[$"{PortalConfigKeys.ConfigurationSectionName}:message-queue:connection"];
    }

    static public string MessageQueueNamespace(this IConfiguration config)
    {
        return config[$"{PortalConfigKeys.ConfigurationSectionName}:message-queue:namespace"];
    }

    static public string MessageQueueClient(this IConfiguration config)
    {
        return config[$"{PortalConfigKeys.ConfigurationSectionName}:message-queue:client"];
    }

    static public string MessageQueueClientSecret(this IConfiguration config)
    {
        return config[$"{PortalConfigKeys.ConfigurationSectionName}:message-queue:secret"];
    }

    #endregion
}
