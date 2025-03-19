using E.Standard.Api.App.Configuration;
using E.Standard.Configuration.Services;
using E.Standard.Extensions.Compare;
using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace E.Standard.Api.App.Extensions;

static public class ConfigurationServiceExtensions
{
    static public string StorageRootPath(this ConfigurationService config)
    {
        return config[ApiConfigKeys.StorageRootPath];
    }

    #region P4 / Coordinates

    static public int DefaultQuerySrefId(this ConfigurationService config)
    {
        try
        {
            return config.Get<int>(ApiConfigKeys.P4DefaultValue, 4326);
        }
        catch { }
        return 4326;
    }

    static public int DefaultQuerySrefId(this IConfiguration config)
    {
        if (int.TryParse(config[$"{ApiConfigKeys.P4DefaultValue}"], out int srefId))
        {
            return srefId;
        }

        return 4326;
    }

    static public bool AllowGeoCodesInput(this IConfiguration config)
    {
        if (bool.TryParse(config[$"{ApiConfigKeys.AllowGeoCodesInput}"], out bool allow))
        {
            return allow;
        }

        return false;

        //return config.Get<bool>(ApiConfigKeys.AllowGeoCodesInput, false);
    }

    static public string Pro4DatabaseConnectionString(this ConfigurationService config)
    {
        return config[ApiConfigKeys.Proj4DatabaseConnectionString];
    }

    #endregion

    #region Urls

    static public string ApiUrl(this ConfigurationService config)
    {
        return config[ApiConfigKeys.ApiUrl];
    }

    static public string PortalUrl(this ConfigurationService config)
    {
        return config[ApiConfigKeys.PortalUrl];
    }

    static public string PortalInternalUrl(this ConfigurationService config)
    {
        return config[ApiConfigKeys.PortalInternalUrl];
    }

    static public string OutputUrl(this ConfigurationService config)
    {
        return config[ApiConfigKeys.OutputUrl];
    }

    static public bool UseConsoleLogging(this ConfigurationService config)
    {
        return config.Get<bool>(ApiConfigKeys.UseConsoleLogging, false);
    }

    static public string DefaultHttpReferer(this ConfigurationService config)
    {
        return config[ApiConfigKeys.DefaultHttpReferer];
    }

    #endregion

    #region Logging

    static public string LogPath(this ConfigurationService config)
    {
        return config[ApiConfigKeys.LogPath];
    }

    static public string LogPerformanceColumns(this ConfigurationService config)
    {
        return config[ApiConfigKeys.LogPerformanceColumns];
    }

    static public bool ShowWarningsInPrintLayout(this ConfigurationService config)
    {
        return config.Get<bool>(ApiConfigKeys.ShowWarningsInPrintLayout, true); // default = true!!
    }

    #endregion

    #region Localization

    static public string[] SupportedLanguages(this IConfiguration config)
    {
        var supportedLanguagesString = config[ApiConfigKeys.SupportedLanguages];
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

    #endregion

    #region System

    static public string GraphicsEngine(this ConfigurationService config)
    {
        return config.Get<string>(ApiConfigKeys.GraphicsEninge, "");
    }

    #endregion

    #region Subscribers

    static public bool AllowSubscriberLogin(this ConfigurationService config)
    {
        return config.Get<bool>(ApiConfigKeys.AllowSubscriberLogin, false);
    }

    static public bool AllowRegisterNewSubscribers(this ConfigurationService config)
    {
        return config.Get<bool>(ApiConfigKeys.AllowRegisterNewSubscribers, false);
    }

    static public IEnumerable<string> AdminSubscribers(this ConfigurationService config)
    {
        return config.GetValues<string>(ApiConfigKeys.AdminSubscribers);
    }

    static public bool IsAdminSubscriber(this ConfigurationService config, string username)
    {
        if (String.IsNullOrEmpty(username))
        {
            return false;
        }

        return config.AdminSubscribers()
                     .Where(a => a.Equals(username, StringComparison.InvariantCultureIgnoreCase))
                     .Count() > 0;
    }

    static public bool SubscriberToolClientsAvailable(this ConfigurationService config)
    {
        return config.HasValue(ApiConfigKeys.SubscriberTools, "clients");
    }
    static public bool SubscriberToolPortalsAvailable(this ConfigurationService config)
    {
        return config.HasValue(ApiConfigKeys.SubscriberTools, "portal-pages");
    }

    #endregion

    #region Ogc

    static public string OgcOnlineResource(this ConfigurationService config)
    {
        return config[ApiConfigKeys.OgcOnlineResource];
    }

    static public string OgcLoginService(this ConfigurationService config)
    {
        return config[ApiConfigKeys.OgcLoginService];
    }

    static public string OgcLogoutService(this ConfigurationService config)
    {
        return config[ApiConfigKeys.OgcLogoutService];
    }

    static public int[] OgcDefaultSupportedCrs(this ConfigurationService config)
    {
        var crs = config.GetValues<int>(ApiConfigKeys.OgcDefaultSupportedCrs);
        if (crs == null || crs.Count() == 0)
        {
            return new[] { 4326 };
        }

        return crs.ToArray();
    }

    #endregion

    #region GDI

    static public string GdiSchemeDefault(this ConfigurationService config)
    {
        var result = config[ApiConfigKeys.GdiSchemeDefault];

        if (String.IsNullOrWhiteSpace(result))
        {
            result = config[ApiConfigKeys.GdiScheme];
        }

        return result;
    }

    #endregion

    #region Middleware

    static public bool UseDeChunkerMiddleware(this ConfigurationService config)
    {
        return config[ApiConfigKeys.UseDeChunkerMiddleware] == "true";
    }

    #endregion

    #region Marker

    static public string[] DefaultMarkerColors(this ConfigurationService config)
    {
        var colors = config[ApiConfigKeys.DefaultMarkerColors];

        if (!string.IsNullOrEmpty(colors))
        {
            return colors.Split(',').Select(c => c.Trim()).ToArray();
        }

        return null;
    }

    #endregion

    #region Encoding

    static public System.Text.Encoding DefaultTextDownloadEncoding(this ConfigurationService config)
    {
        var name = config[ApiConfigKeys.DefaultTextDownloadEncoding].OrTake("iso-8859-1");

        foreach (System.Text.EncodingInfo ei in System.Text.Encoding.GetEncodings())
        {
            if (name.Equals(ei.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                return ei.GetEncoding();
            }
        }

        return System.Text.Encoding.Unicode;
    }

    #endregion

    #region Security

    static public bool SecurtyUsermanagementAllowWildcards(this ConfigurationService config)
    {
        return config[ApiConfigKeys.SecurityUsermanagementAllowWildcards] == "true";
    }

    static public IEnumerable<string> SecurityAddCustomServiceHostBlacklist(this ConfigurationService config)
    {
        return config[ApiConfigKeys.SecurityAddCustomServiceHostBlacklist]?
                    .Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !String.IsNullOrEmpty(s))
                    ?? Array.Empty<string>();
    }

    static public string AppCacheListPassword(this ConfigurationService config)
    {
        return config[ApiConfigKeys.AppCacheListPassword];
    }

    #endregion

    #region AppRoles

    static public AppRoles AppRoles(this ConfigurationService config)
    {
        var appRolesString = config[ApiConfigKeys.AppRoles];

        if (!string.IsNullOrEmpty(appRolesString))
        {
            var result = E.Standard.Api.App.AppRoles.None;

            foreach (var appRoleString in appRolesString.Split(','))
            {
                if (Enum.TryParse<AppRoles>(appRoleString.Trim(), true, out AppRoles appRole))
                {
                    result |= appRole;
                }
            }

            return result;
        }
        else
        {
            return E.Standard.Api.App.AppRoles.All;
        }
    }

    #endregion

    #region WebProxy, Rediretions, Legacy

    static public WebProxy GetWebProxy(this IConfiguration config)
    {
        foreach (var section in new[] { "proxy", "legacy-proxy" })
        {
            string sectionName = $"{ApiConfigKeys.ConfigurationSectionName}:{section}";
            if (config[$"{sectionName}:use"] == "true")
            {
                int port;
                if (!int.TryParse(config[$"{sectionName}:port"], out port))
                {
                    port = 80;
                }

                var webProxy = new WebProxy(config[$"{sectionName}:server"], port);

                if (!String.IsNullOrEmpty(config[$"{sectionName}:user"]))
                {
                    System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(
                        config[$"{sectionName}:user"],
                        config[$"{sectionName}:pwd"],
                        config[$"{sectionName}:domain"]); //z.B. ("mustermann","butterbrot","musterdomain") ;

                    webProxy.Credentials = credentials;
                }

                return webProxy;
            }
        }

        return null;
    }

    static public string[] WebProxyIgnoresOrNull(this IConfiguration config)
    {
        foreach (var section in new[] { "proxy", "legacy-proxy" })
        {
            string sectionName = $"{ApiConfigKeys.ConfigurationSectionName}:{section}";

            if (!String.IsNullOrEmpty(config[$"{sectionName}:ignore"]))
            {
                return config[$"{sectionName}:ignore"].Replace(",", ";")
                                                       .Split(';')
                                                       .Select(s => s.Trim())
                                                       .ToArray();
            }
        }

        return null;
    }

    static public Dictionary<string, string> UrlOutputRedirectionsOrNull(this IConfiguration config)
        => UrlRedirectionsOrNull(config, " => ");

    static public Dictionary<string, string> UrlInputRedirectionsOrNull(this IConfiguration config)
        => UrlRedirectionsOrNull(config, " >= ");

    static private Dictionary<string, string> UrlRedirectionsOrNull(this IConfiguration config, string separator)
    {
        var result = new Dictionary<string, string>();

        foreach (var sectionName in new[] { "legacy-url-redirections", "url-redirections" })
        {
            var section = config.GetSection($"{ApiConfigKeys.ConfigurationSectionName}:{sectionName}");

            foreach (var redirection in section.GetChildren())
            {
                string val = redirection.Value;

                if (!String.IsNullOrEmpty(val) && val.Contains(separator))
                {
                    int pos = val.IndexOf(separator);
                    string from = val.Substring(0, pos).Trim().ToLower();
                    string to = val.Substring(pos + 4).Trim();

                    result[from] = to;
                }
            }
        }

        return result.Count > 0
            ? result
            : null;
    }

    static public string[] LegacyAlwaysDownloadFromOrNull(this IConfiguration config)
    {
        if (!String.IsNullOrEmpty(config[$"{ApiConfigKeys.ConfigurationSectionName}:legacy:alwaysdownloadfrom"]))
        {
            return config[$"{ApiConfigKeys.ConfigurationSectionName}:legacy:alwaysdownloadfrom"].Split(';')
                                                                                                .Select(s => s.Trim())
                                                                                                .ToArray();
        }

        return null;
    }

    #endregion

    #region Message Queue

    static public bool UseMessageQueue(this IConfiguration config)
    {
        return config[$"{ApiConfigKeys.ConfigurationSectionName}:message-queue:use"] == "true";
    }

    static public string MessageQueueConnection(this IConfiguration config)
    {
        return config[$"{ApiConfigKeys.ConfigurationSectionName}:message-queue:connection"];
    }

    static public string MessageQueueNamespace(this IConfiguration config)
    {
        return config[$"{ApiConfigKeys.ConfigurationSectionName}:message-queue:namespace"];
    }

    static public string MessageQueueClient(this IConfiguration config)
    {
        return config[$"{ApiConfigKeys.ConfigurationSectionName}:message-queue:client"];
    }

    static public string MessageQueueClientSecret(this IConfiguration config)
    {
        return config[$"{ApiConfigKeys.ConfigurationSectionName}:message-queue:secret"];
    }

    #endregion

    #region DataLinq

    static public bool IncludeDataLinqServices(this IConfiguration config)
    {
        return "true".Equals(config[$"{ApiConfigKeys.ConfigurationSectionName}:datalinq:include"], StringComparison.OrdinalIgnoreCase);
    }

    static public string DataLinqEngineId(this IConfiguration config)
    {
        return config[$"{ApiConfigKeys.ConfigurationSectionName}:datalinq:razor-engine"] ?? "default";
    }

    static public bool AllowDataLingCodeEditing(this IConfiguration config)
    {
        return "true".Equals(config[$"{ApiConfigKeys.ConfigurationSectionName}:datalinq:allow-code-editing"], StringComparison.OrdinalIgnoreCase);
    }

    public static string DataLinqEnvrionment(this IConfiguration config)
    {
        return config[$"{ApiConfigKeys.ConfigurationSectionName}:datalinq:environment"].OrTake("default");
    }

    public static IEnumerable<string> DataLinqAddRazorNamesapces(this IConfiguration config)
    {
        var namespaces = config[$"{ApiConfigKeys.ConfigurationSectionName}:datalinq:add-namespaces"];
        if (String.IsNullOrEmpty(namespaces))
        {
            return new string[0];
        }

        return namespaces.Split(',')
                         .Select(n => n.Trim())
                         .Where(n => !string.IsNullOrEmpty(n))
                         .ToArray();
    }

    public static IEnumerable<string> DataLinqCustomCssUrls(this IConfiguration config, string version)
    {
        var cssUrls = config[$"{ApiConfigKeys.ConfigurationSectionName}:datalinq:add-css"];
        if (String.IsNullOrEmpty(cssUrls))
        {
            return new string[0];
        }

        return cssUrls.Split(',')
                         .Select(n => n.Trim().Replace("{version}", version))
                         .Where(n => !string.IsNullOrEmpty(n))
                         .ToArray();
    }

    public static IEnumerable<string> DataLinqCustomJavaScriptUrls(this IConfiguration config, string version)
    {
        var jsUrls = config[$"{ApiConfigKeys.ConfigurationSectionName}:datalinq:add-js"];
        if (String.IsNullOrEmpty(jsUrls))
        {
            return new string[0];
        }

        return jsUrls.Split(',')
                         .Select(n => n.Trim().Replace("{version}", version))
                         .Where(n => !string.IsNullOrEmpty(n))
                         .ToArray();
    }

    public static IEnumerable<string> DataLinqRazorWhiteListItems(this IConfiguration config)
    {
        var whitlelist = config[$"{ApiConfigKeys.ConfigurationSectionName}:datalinq:add-razor-whitelist"];
        if (String.IsNullOrEmpty(whitlelist))
        {
            return new string[0];
        }

        return whitlelist.Split(',')
                         .Select(n => n.Trim())
                         .Where(n => !string.IsNullOrEmpty(n))
                         .ToArray();
    }

    public static IEnumerable<string> DataLinqRazorBlackListItems(this IConfiguration config)
    {
        var blackList = config[$"{ApiConfigKeys.ConfigurationSectionName}:datalinq:add-razor-blacklist"];
        if (String.IsNullOrEmpty(blackList))
        {
            return new string[0];
        }

        return blackList.Split(',')
                         .Select(n => n.Trim())
                         .Where(n => !string.IsNullOrEmpty(n))
                         .ToArray();
    }

    public static IEnumerable<string> DataLinqCodeApiClients(this IConfiguration config)
    {
        var clients = config[$"{ApiConfigKeys.ConfigurationSectionName}:datalinq:allowed-code-api-clients"];
        if (String.IsNullOrEmpty(clients))
        {
            return new string[0];
        }

        return clients.Split(',')
                         .Select(n => n.Trim())
                         .Where(n => !string.IsNullOrEmpty(n))
                         .ToArray();
    }

    public static string DataLinqApiEngineConnectionReplace(this IConfiguration config, string url)
    {
        var replacements = config[$"{ApiConfigKeys.ConfigurationSectionName}:datalinq:api-engine-connection-replacements"];

        if (!String.IsNullOrEmpty(replacements))
        {
            foreach (var replacement in replacements.Split(';'))
            {
                var parts = replacement.Split(' ');
                if (parts.Length == 2)
                {
                    url = url.Replace(parts[0], parts[1]);
                }
            }
        }

        return url;
    }

    public static string DataLinqApiEncryptionLevel(this IConfiguration config)
    {
        return config[$"{ApiConfigKeys.ConfigurationSectionName}:datalinq:api-encryption-level"];
    }

    #endregion

    #region Folders

    static public string TempFolderPath(this IConfiguration config)
    {
        return config[ApiConfigKeys.TempFolderPath];
    }

    #endregion

    #region ConfigServiceInstance

    static public ConfigurationService ToServiceInstanceWithHostEnvironmentParser(this IConfiguration configuration)
    {
        var configService = new ConfigurationService(configuration, new IConfigValueParser[]
        {
            new E.Standard.Configuration.Services.Parser.HostingEnvironmentConfigValueParser(configuration)
        });

        return configService;
    }

    #endregion

    #region Timeouts

    static public int BufferResultsTimeoutSeconds(this IConfiguration config)
    {
        if (int.TryParse(config[$"{ApiConfigKeys.ConfigurationSectionName}:timeouts:buffer-results"], out int seconds))
        {
            return seconds;
        }

        return 20;
    }

    #endregion

    #region QuickSearch

    static public int QuickSearchMaxResultItems(this IConfiguration config)
    {
        if (int.TryParse(config[$"{ApiConfigKeys.ConfigurationSectionName}:quick-search:max-result-items"], out int maxResultItems))
        {
            return maxResultItems;
        }

        return 5;
    }

    #endregion

    #region Setup

    static public string SecuritySetupPassword(this ConfigurationService config)
    {
        return config[ApiConfigKeys.SecuritySetupPassword];
    }

    #endregion

    #region Section secured-tiles-redirect

    static private string[] _allowedSecuredTilesRedirectReferers = null;
    static public string[] AllowedSecuredTilesRedirectReferers(this ConfigurationService config)
    {
        if (_allowedSecuredTilesRedirectReferers is null)
        {
            var _allowedSecuredTilesRedirectReferers = config[ApiConfigKeys.ToKey("secured-tiles-redirect:referers")]?
                        .Split(',')
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => s.Trim().ToLowerInvariant())
                        .ToArray() ?? [];
        }

        return _allowedSecuredTilesRedirectReferers;
    }

    #endregion

    #region Section Cms Upload

    static public bool IsCmsUploadAllowed(this IConfiguration config, string cmsName)
        => "true".Equals(config[ApiConfigKeys.ToKey($"cms-upload-{cmsName}:allow")], StringComparison.OrdinalIgnoreCase);

    static public string CmsUploadClient(this IConfiguration config, string cmsName)
        => config[ApiConfigKeys.ToKey($"cms-upload-{cmsName}:client")];

    static public string CmsUploadSecret(this IConfiguration config, string cmsName)
        => config[ApiConfigKeys.ToKey($"cms-upload-{cmsName}:secret")];

    static public IEnumerable<string> AllCmsNames(this IConfiguration config)
    {
        foreach (var configSection in config.GetSection(ApiConfigKeys.ConfigurationSectionName)
                                           .GetChildren()
                                           .Where(c => c.Key.StartsWith("cmspath")))
        {
            if (configSection.Key == "cmspath")
            {
                yield return String.Empty;
            }
            else
            {
                yield return configSection.Key.Substring("cmspath_".Length);
            }
        }
    }

    #endregion
}
