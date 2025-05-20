namespace E.Standard.Api.App.Configuration;

public class ApiConfigKeys
{
    static public string ToKey(string key)
    {
        return $"{ConfigurationSectionName}:{key}";
    }

    public const string ConfigurationSectionName = "Api";

    public const string SharedCryptoKeysPath = ConfigurationSectionName + ":shared-crypto-keys-path";
    public const string AppRoles = ConfigurationSectionName + ":app-roles";

    public const string ApiUrl = ConfigurationSectionName + ":api-url";
    public const string PortalUrl = ConfigurationSectionName + ":portal-url";
    public const string PortalInternalUrl = ConfigurationSectionName + ":portal-internal-url";

    public const string OutputUrl = ConfigurationSectionName + ":outputUrl";
    public const string OutputPath = ConfigurationSectionName + ":outputPath";

    public const string TempFolderPath = ConfigurationSectionName + ":tempFolderPath";

    public const string LogPath = ConfigurationSectionName + ":Log_Path";
    public const string LogPerformanceColumns = ConfigurationSectionName + ":Log_Performance_Columns";
    public const string LogUsageColumns = ConfigurationSectionName + ":Log_Performance_Columns";
    public const string Trace = ConfigurationSectionName + ":trace";


    public const string StorageRootPath = ConfigurationSectionName + ":storage-rootpath";
    public const string StorageRootPath2 = ConfigurationSectionName + ":storage-rootpath2";

    public const string P4DefaultValue = ConfigurationSectionName + ":p4_default";
    public const string AllowGeoCodesInput = ConfigurationSectionName + ":allow-geocodes";

    public const string ServerSideConfigurationPath = ConfigurationSectionName + ":server-side-configuration-path";

    public const string InstanceRoles = ConfigurationSectionName + ":instance-roles";
    public const string AllowBranches = ConfigurationSectionName + ":allow-branches";
    //public const string UseAuthExclusives = ConfigurationSectionName + ":use-auth-exclusives";

    public const string SubscriberDbConnectionString = ConfigurationSectionName + ":subscriber-db-connectionstring";
    public const string AllowSubscriberLogin = ConfigurationSectionName + ":allow-subscriber-login";
    public const string AllowRegisterNewSubscribers = ConfigurationSectionName + ":allow-register-new-subscribers";
    public const string AdminSubscribers = ConfigurationSectionName + ":subscriber-admins";
    public const string SubscriberTools = ConfigurationSectionName + ":subscription-tools";

    public const string KeyValueCacheProvider = ConfigurationSectionName + ":cache-provider";
    public const string KeyValueCacheConnectionString = ConfigurationSectionName + ":cache-connectionstring";
    public const string KeyValueAsideCacheProvider = ConfigurationSectionName + ":cache-aside-provider";
    public const string KeyValueAsideCacheConnectionString = ConfigurationSectionName + ":cache-aside-connectionstring";

    public const string ImpersonateUser = ConfigurationSectionName + ":ImpersonateUser";

    public const string Proj4DatabaseConnectionString = ConfigurationSectionName + ":p4database";

    public const string OgcOnlineResource = ConfigurationSectionName + ":ogc-online-resource";
    public const string OgcLoginService = ConfigurationSectionName + ":ogc-login-service";
    public const string OgcLogoutService = ConfigurationSectionName + ":ogc-logout-service";
    public const string OgcDefaultSupportedCrs = ConfigurationSectionName + ":ogc-default-supported-crs";

    public const string GdiSchemeDefault = ConfigurationSectionName + ":cmsgdischema_default";
    public const string GdiScheme = ConfigurationSectionName + ":cmsgdischema";

    public const string DefaultHttpReferer = ConfigurationSectionName + ":default-httpreferer";

    public const string UseConsoleLogging = ConfigurationSectionName + ":console-logging";
    public const string LoggingType = ConfigurationSectionName + ":logging-type";
    public const string LoggingLogPerformance = ConfigurationSectionName + ":logging-log-performance";
    public const string LoggingLogPerformanceAppName = ConfigurationSectionName + ":logging-log-performance-appname";
    public const string LoggingLogExceptions = ConfigurationSectionName + ":logging-log-exceptions";
    public const string LoggingLogExceptionsAppName = ConfigurationSectionName + ":logging-log-exceptions-appname";
    public const string LoggingLogUsage = ConfigurationSectionName + ":logging-log-usage";
    public const string LoggingLogUsageAppName = ConfigurationSectionName + ":logging-log-usage-appname";
    public const string LoggingLogWarnings = ConfigurationSectionName + ":logging-log-warnings";
    public const string LoggingLogWarningAppName = ConfigurationSectionName + ":logging-log-warnings-appname";
    public const string LoggingLogOgcPerformance = ConfigurationSectionName + ":logging-log-ogcperformance";
    public const string LoggingLogOgcPerformanceAppName = ConfigurationSectionName + ":logging-log-warnings-ogcperformance";
    public const string LoggingLogDataLinq = ConfigurationSectionName + ":logging-log-datalinq";
    public const string LoggingLogDataLinqAppName = ConfigurationSectionName + ":logging-log-datalinq-appname";
    public const string LoggingLogInsights = ConfigurationSectionName + ":logging-log-insights";
    public const string LoggingLogInsightsAppName = ConfigurationSectionName + ":logging-log-insights-appname";
    public const string LoggingLogServiceRequests = ConfigurationSectionName + ":logging-log-service-requests";

    public const string ShowWarningsInPrintLayout = ConfigurationSectionName + ":show_warnings_in_print_output";

    public const string DefaultUserLanguage = ConfigurationSectionName + ":default-user-language";

    public const string HttpClientDefaultTimeoutSeconds = ConfigurationSectionName + ":httpclient:default-timeout-seconds";

    public const string GraphicsEninge = ConfigurationSectionName + ":graphics-engine";

    public const string UseDeChunkerMiddleware = ConfigurationSectionName + ":use-dechunker-middleware";

    public const string DefaultMarkerColors = ConfigurationSectionName + ":default-marker-colors";

    public const string DefaultTextDownloadEncoding = ConfigurationSectionName + ":default-text-download-encoding";

    public const string BackgroundTaskClearOuput = ConfigurationSectionName + ":background-task-clear-output";
    public const string BackgroundTaskClearSharedMaps = ConfigurationSectionName + ":background-task-clear-shared-maps";

    public const string SecurityUsermanagementAllowWildcards = ConfigurationSectionName + ":security-usernamangement-allow-wildcards";

    public const string SecuritySetupPassword = ConfigurationSectionName + ":security-setup-pwd";
    public const string SecurityAddCustomServiceHostBlacklist = ConfigurationSectionName + ":security-add-custom-services-host-blacklist";
    public const string AppCacheListPassword = ConfigurationSectionName + ":app-cache-list-pwd";

    public const string SupportedLanguages = ConfigurationSectionName + ":supported-languages";
}
