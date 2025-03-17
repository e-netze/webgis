using Microsoft.Extensions.DependencyInjection;
using System;

namespace Api.Core.AppCode.Services.Rest;

public class RestServiceFactory : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope _serviceScope = null;

    public RestServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private RestServiceFactory(IServiceScope serviceScope)
    {
        _serviceScope = serviceScope;
        _serviceProvider = _serviceScope.ServiceProvider;
    }

    public RestServiceFactory CreateScope()
    {
        return new RestServiceFactory(_serviceProvider.CreateScope());
    }

    public RestHelperService Helper => _serviceProvider.GetRequiredService<RestHelperService>();
    public RestMappingHelperService Mapping => _serviceProvider.GetRequiredService<RestMappingHelperService>();
    public RestQueryHelperService Query => _serviceProvider.GetRequiredService<RestQueryHelperService>();
    public RestEditingHelperService Editing => _serviceProvider.GetRequiredService<RestEditingHelperService>();
    public RestPrintHelperService Print => _serviceProvider.GetRequiredService<RestPrintHelperService>();
    public RestRequestHmacHelperService RequestHmac => _serviceProvider.GetRequiredService<RestRequestHmacHelperService>();
    public RestSearchHelperService Search => _serviceProvider.GetRequiredService<RestSearchHelperService>();
    public RestToolsHelperService Tools => _serviceProvider.GetRequiredService<RestToolsHelperService>();
    public RestSnappingService Snapping => _serviceProvider.GetRequiredService<RestSnappingService>();

    public BridgeService Bridge => _serviceProvider.GetRequiredService<BridgeService>();

    public RestImagingService Imaging => _serviceProvider.GetRequiredService<RestImagingService>();

    #region IDisposable

    public void Dispose()
    {
        if (_serviceScope != null)
        {
            _serviceScope.Dispose();
        }

        //Console.WriteLine("RestServiceFactory: disposed");
    }

    #endregion
}
