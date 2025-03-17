namespace E.Standard.Caching.Services;

public class KeyValueCacheServiceOptions
{
    public string CacheProviderConfigValue { get; set; }
    public string CacheConnectionStringConfigValue { get; set; }
    public double CacheExpireSecondsDefault { get; set; }

    public string CacheAsideProviderConfigValue { get; set; }
    public string CacheAsideConnectionStringConfigValue { get; set; }
    public double CacheAsideExpireSecondsDefault { get; set; }

    public string ImpersonateUserConfigValue { get; set; }
}
