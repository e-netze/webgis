using E.Standard.CMS.Core;
using E.Standard.Web.Abstractions;
using Microsoft.Extensions.Localization;

namespace E.Standard.Cms.Services;
public class CmsItemInjectionPackService
{
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public CmsItemInjectionPackService(IHttpService http, IStringLocalizerFactory stringLocalizerFactory)
    {
        _servicePack = new CmsItemTransistantInjectionServicePack(http, stringLocalizerFactory);
    }

    public CmsItemTransistantInjectionServicePack ServicePack => _servicePack;
}
