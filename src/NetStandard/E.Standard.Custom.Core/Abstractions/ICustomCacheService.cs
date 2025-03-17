namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomCacheService
{
    void CacheClear(string cmsName = "");
}
