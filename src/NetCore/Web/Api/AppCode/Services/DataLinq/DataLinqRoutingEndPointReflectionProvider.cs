using System;

using E.DataLinq.Web.Services.Abstraction;
using E.Standard.WebApp.Abstraction;

namespace Api.Core.AppCode.Services.DataLinq;

public class DataLinqRoutingEndPointReflectionProvider : IRoutingEndPointReflectionProvider
{
    private readonly IEndPointReflectionProvider _routing;

    public DataLinqRoutingEndPointReflectionProvider(IEndPointReflectionProvider routing)
    {
        _routing = routing;
    }

    public T GetActionMethodCustomAttribute<T>() where T : Attribute => _routing.GetActionMethodCustomAttribute<T>();

    public T GetControllerCustomAttribute<T>() where T : Attribute => _routing.GetControllerCustomAttribute<T>();
}
