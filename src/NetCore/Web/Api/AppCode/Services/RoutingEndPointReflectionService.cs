using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Api.Core.AppCode.Exceptions;
using Api.Core.AppCode.Reflection;

using E.Standard.Api.App;
using E.Standard.Api.App.Endpoints.Metadata;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

namespace Api.Core.AppCode.Services;

public class RoutingEndPointReflectionService
{
    private readonly IEnumerable<Attribute> _controllerAttributes;
    private readonly IEnumerable<Attribute> _actionMethodAttributes;
    private readonly RoutingEndPointReflectionServiceOptions _options;

    public RoutingEndPointReflectionService(IHttpContextAccessor context,
                                            IOptionsMonitor<RoutingEndPointReflectionServiceOptions> options)
    {
        _options = options.CurrentValue;

        // Controllers/Action
        var controllerActionDescriptor = context.HttpContext?.GetEndpoint()?.Metadata?.GetMetadata<ControllerActionDescriptor>();
        if (controllerActionDescriptor != null)
        {
            _controllerAttributes = controllerActionDescriptor.ControllerTypeInfo?.GetCustomAttributes();
            _actionMethodAttributes = controllerActionDescriptor.MethodInfo?.GetCustomAttributes();

            var appRoleAttribute = GetCustomAttribute<AppRoleAttribute>();
            if (appRoleAttribute != null)
            {
                switch (appRoleAttribute.AppRole)
                {
                    case AppRoles.None:
                        throw new AppRoleNotAllowedException(appRoleAttribute.AppRole);
                    case AppRoles.All:
                        break;
                    default:
                        if (!AppRoleIsAllowed(appRoleAttribute.AppRole))
                        {
                            throw new AppRoleNotAllowedException(appRoleAttribute.AppRole);
                        }
                        break;
                }
            }

            return;
        }

        // Minimal API Endpoints
        var reflectionMetadata = context.HttpContext?.GetEndpoint()?.Metadata?.GetMetadata<IApiEndpointReflectionMetadata>();
        if (reflectionMetadata != null)
        {
            _actionMethodAttributes = reflectionMetadata.GetAllAttributes();
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

    private AppRoles AppRoles => _options.AppRoles;

    private bool AppRoleIsAllowed(AppRoles appRole)
    {
        return _options.AppRoles.HasFlag(AppRoles.All) || _options.AppRoles.HasFlag(appRole);
    }
}
