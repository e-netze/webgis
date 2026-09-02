using E.Standard.CMS.Core.Extensions;
using E.Standard.Localization.Abstractions;
using E.Standard.Web.Abstractions;
using Microsoft.Extensions.Localization;
using System;

namespace E.Standard.CMS.Core;

public class CmsItemTransistantInjectionServicePack
{
    private readonly IStringLocalizerFactory _stringLocalizerFactory;
    public CmsItemTransistantInjectionServicePack(IHttpService httpService, IStringLocalizerFactory stringLocalizerFactory)
    {
        HttpService = httpService;
        _stringLocalizerFactory = stringLocalizerFactory;
    }

    public IHttpService HttpService { get; }

    public ILocalizer GetLocalizer(Type type)
    {
        return _stringLocalizerFactory.CreateCmsLocalizer(type);
    }
}
