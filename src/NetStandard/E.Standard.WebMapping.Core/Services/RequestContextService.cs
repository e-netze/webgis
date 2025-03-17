using E.Standard.Web.Abstractions;
using E.Standard.WebMapping.Core.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.WebMapping.Core.Services;

internal class RequestContextService : IRequestContext, IDisposable
{
    private readonly IHttpService _httpService;
    private readonly IServiceProvider _serviceProvider;

    public RequestContextService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            IHttpService httpService)
    {
        _serviceProvider = serviceProvider;
        _httpService = httpService;

        Trace = configuration["Api:trace"] == "true";
    }

    public IHttpService Http { get => _httpService; }

    public bool Trace { get; }

    public void Dispose()
    {
        //Console.WriteLine("RequestContextService: Disposed");
    }

    public T GetRequiredService<T>() => _serviceProvider.GetRequiredService<T>();
}
