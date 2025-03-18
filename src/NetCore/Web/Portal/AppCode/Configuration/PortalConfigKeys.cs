using System;

namespace Portal.Core.AppCode.Configuration;

public class PortalConfigKeys
{
    static public string ToKey(string key)
    {
        return $"{ConfigurationSectionName}:{key}";
    }

    public const string ConfigurationSectionName = "Portal";

    public const string SharedCryptoKeysPath = ConfigurationSectionName + ":shared-crypto-keys-path";

    public const string Company = ConfigurationSectionName + ":company";
    public const string CompanyUrl = ConfigurationSectionName + ":company-url";
    public const string PortalName = ConfigurationSectionName + ":portal-name";
    public const string PortalNameUrl = ConfigurationSectionName + ":portal-name-url";

    public const string PortalHomeViewName = ConfigurationSectionName + ":portal-home-view-name";
    public const string DefaultPortalPageId = ConfigurationSectionName + ":default-page-id";

    public const string PortalCustomContentRootPath = ConfigurationSectionName + ":portal-custom-content-rootpath";

    public const string MapCalcCrs = ConfigurationSectionName + ":map-calc-crs";

    public const string DefaultSecurityMethod = ConfigurationSectionName + ":security";
    public const string AllowedSecurityMethods = ConfigurationSectionName + ":security_allowed_methods";
    public const string SecurityClientId = ConfigurationSectionName + ":security_clientid";
    public const string SecuritySuppressXFrameOptionsHeader = ConfigurationSectionName + ":security_allow_iframe_embedding";

    [Obsolete]
    public const string SecurityTicketDbConnectionString = ConfigurationSectionName + ":security_ticket_db";
    public const string Proj4DatabaseConnectionString = ConfigurationSectionName + ":p4database";

    public const string AllowSubscriberLogin = ConfigurationSectionName + ":allow-subscriber-login";

    public const string RegisterServiceWorker = ConfigurationSectionName + ":register-serviceworker";
    public const string ManifestRootUrl = ConfigurationSectionName + ":manifest-root-url";

    public const string UseLocalUrlScheme = ConfigurationSectionName + ":use-local-url-scheme";
    public const string PortalUrl = ConfigurationSectionName + ":portal-url";
    public const string PortalUrlOldKey = ConfigurationSectionName + ":portal";
    public const string ApiUrl = ConfigurationSectionName + ":api";
    public const string ApiUrlInternal = ConfigurationSectionName + ":api-internal-url";

    public const string SecurityWindowsGetGroupDirectoryEntry = ConfigurationSectionName + ":security_windows_getgroup_directoryentry";
    public const string SecurityWindowsGetGroupRecursiv = ConfigurationSectionName + ":security_windows_getgroup_recursiv";
    public const string SecurityWindowsDomainSubstitute = ConfigurationSectionName + ":security_windows_domain_substitute";
    public const string SecurityWindowsAuthenticationLdapFormat = ConfigurationSectionName + ":portal-windows-authentication-ldap-format";
    public const string SecurityWindowsAuthenticationLdapDirectory = ConfigurationSectionName + ":portal-windows-authentication-ldap-directory";

    public const string AllowSubscriberAccessPageSettings = ConfigurationSectionName + ":allow-subscriber-user-access-page-settings";
    public const string SubscriberDbConnectionString = ConfigurationSectionName + ":subscriber-db-connectionstring";

    public const string UseFavoriteDetection = ConfigurationSectionName + ":use-favorite-detection";
    public const string UseCustomRecommendationJs = ConfigurationSectionName + ":use-custom-recommendation-js";
    public const string QueryCustomMapLayout = ConfigurationSectionName + ":query-custom-map-layout";
    public const string AllowMapUIMaster = ConfigurationSectionName + ":allow-map-ui-master";

    public const string AppCacheListPassword = ConfigurationSectionName + ":app-cache-list-pwd";

    public const string KeyValueCacheProvider = ConfigurationSectionName + ":cache-provider";
    public const string KeyValueCacheConnectionString = ConfigurationSectionName + ":cache-connectionstring";
    public const string KeyValueAsideCacheProvider = ConfigurationSectionName + ":cache-aside-provider";
    public const string KeyValueAsideCacheConnectionString = ConfigurationSectionName + ":cache-aside-connectionstring";

    public const string ImpersonateUser = ConfigurationSectionName + ":ImpersonateUser";

    public const string MapViewHelpUrl = ConfigurationSectionName + ":map-viewer-help-url";

    public const string UseDeChunkerMiddleware = ConfigurationSectionName + ":use-dechunker-middleware";

    public const string PortalPageShowOptimizationFilter = ConfigurationSectionName + ":portal-page-show-optimization-filter";

    public const string ShowNewsTippsSince = ConfigurationSectionName + ":show-news-tips-since-version";

    public const string HeaderAuthenticationUse = ConfigurationSectionName + ":header-authentication:use";
    public const string HeaderAuthenticationUsernameVariable = ConfigurationSectionName + ":header-authentication:username-variable";
    public const string HeaderAuthenticationRolesVariable = ConfigurationSectionName + ":header-authentication:roles-variable";
    public const string HeaderAuthenticationExtractRoleParameters = ConfigurationSectionName + ":header-authentication:extract-role-parameters";
    public const string HeaderAuthenticationUserPrefix = ConfigurationSectionName + ":header-authentication:user-prefix";
    public const string HeaderAuthenticationRolePrefix = ConfigurationSectionName + ":header-authentication:role-prefix";
    public const string HeaderAuthenticationRoleSeparator = ConfigurationSectionName + ":header-authentication:role-separator";
    public const string HeaderAuthenticationRoleParametersSeparator = ConfigurationSectionName + ":header-authentication:role-parameters-separator";

    public const string ExtendedRoleParametersSource = ConfigurationSectionName + ":portal_extended_role_parameters_source";
    public const string ExtendedRoleParametersStatement = ConfigurationSectionName + ":portal_extended_role_parameters_statement";
    public const string ExtendedRoleParametersHeader = ConfigurationSectionName + ":portal_extended_role_parameters_header";

    public const string SupportedLanguages = ConfigurationSectionName + ":supported-languages";
}
