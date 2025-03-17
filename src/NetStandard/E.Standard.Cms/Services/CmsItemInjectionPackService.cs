using E.Standard.CMS.Core;
using E.Standard.Web.Abstractions;

namespace E.Standard.Cms.Services;
public class CmsItemInjectionPackService
{
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public CmsItemInjectionPackService(IHttpService http)
    {
        _servicePack = new CmsItemTransistantInjectionServicePack(http);
    }

    public CmsItemTransistantInjectionServicePack ServicePack => _servicePack;
}
