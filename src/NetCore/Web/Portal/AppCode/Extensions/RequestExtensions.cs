using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Portal.Core.AppCode.Extensions;

static public class RequestExtensions
{
    static public Uri Uri(this HttpRequest request)
    {
        return new Uri(Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(request));
    }

    static public bool HasFormData(this HttpRequest request)
    {
        return request.HasFormContentType;
    }

    static public TAttribute GetCustomAttribute<TAttribute>(this Endpoint endpoint)
        where TAttribute : Attribute
    {
        return endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>().GetCustomAttribute<TAttribute>();
    }

    static public TAttribute GetCustomAttribute<TAttribute>(this ControllerActionDescriptor controllerActionDescriptor)
        where TAttribute : Attribute
    {
        if (controllerActionDescriptor != null)
        {
            var attribute =
                controllerActionDescriptor.ControllerTypeInfo.GetCustomAttribute<TAttribute>() ??
                controllerActionDescriptor.MethodInfo.GetCustomAttribute<TAttribute>();

            return attribute;
        }

        return null;
    }

    public static bool IsIOSDevice(this HttpRequest request)
    {
        if (!request?.Headers?.ContainsKey("User-Agent") == true)
        {
            return false;
        }

        var userAgent = request.Headers["User-Agent"].ToString() ?? "";

        return userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("iPod", StringComparison.OrdinalIgnoreCase);
    }

    static public HttpResponse AddNoCacheHeaders(this HttpResponse response)
    {
        response.Headers.TryAdd("Pragma", "no-cache");
        response.Headers.TryAdd("Cache-Control", "no-cache, no-store, max-age=0, must-revalidate");

        return response;
    }
    static public HttpResponse AddPortalCorsHeaders(this HttpResponse response, HttpRequest request = null)
    {
        response.Headers.TryAdd("Access-Control-Allow-Headers", "*");
        response.Headers.TryAdd("Access-Control-Allow-Origin",
            (string)request?.Headers["Origin"] != null
                ? (string)request.Headers["Origin"]
                : "*"
             );
        response.Headers.TryAdd("Access-Control-Allow-Credentials", "true");

        return response;
    }
}
