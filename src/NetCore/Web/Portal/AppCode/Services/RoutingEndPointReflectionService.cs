using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Portal.Core.AppCode.Services;

public class RoutingEndPointReflectionService
{
    private readonly IEnumerable<Attribute> _controllerAttributes;
    private readonly IEnumerable<Attribute> _actionMethodAttributes;

    public RoutingEndPointReflectionService(IHttpContextAccessor context)
    {
        var controllerActionDescriptor = context.HttpContext?.GetEndpoint()?.Metadata?.GetMetadata<ControllerActionDescriptor>();

        if (controllerActionDescriptor != null)
        {
            _controllerAttributes = controllerActionDescriptor.ControllerTypeInfo?.GetCustomAttributes();
            _actionMethodAttributes = controllerActionDescriptor.MethodInfo?.GetCustomAttributes();
        }
    }

    public T GetCustomAttribute<T>()
        where T : Attribute
    {
        return GetActionMethodCustomAttribute<T>() ?? GetControllerCustomAttribute<T>();
    }

    public T GetControllerCustomAttribute<T>()
        where T : Attribute
    {
        var type = typeof(T);

        return (T)_controllerAttributes?.Where(a => a.GetType().Equals(type)).FirstOrDefault();
    }

    public T GetActionMethodCustomAttribute<T>()
        where T : Attribute
    {
        var type = typeof(T);

        return (T)_actionMethodAttributes?.Where(a => a.GetType().Equals(type)).FirstOrDefault();
    }
}
