using E.Standard.Web.Abstractions;

namespace E.Standard.CMS.Core;

public class CmsItemTransistantInjectionServicePack
{
    public CmsItemTransistantInjectionServicePack(IHttpService httpService)
    {
        HttpService = httpService;
    }

    public IHttpService HttpService { get; }
}
