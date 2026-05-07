using System;

using E.DataLinq.Web.Services.Abstraction;

namespace Api.Core.AppCode.Services.DataLinq;

public class DataLinqRoutingEndPointReflectionProvider : IRoutingEndPointReflectionProvider
{
    private readonly RoutingEndPointReflectionService _routing;

    public DataLinqRoutingEndPointReflectionProvider(RoutingEndPointReflectionService routing)
    {
        _routing = routing;
    }

    public T GetActionMethodCustomAttribute<T>() where T : Attribute => _routing.GetActionMethodCustomAttribute<T>();

    public T GetControllerCustomAttribute<T>() where T : Attribute => _routing.GetControllerCustomAttribute<T>();
}
