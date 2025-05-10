using E.Standard.Api.App.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using RazorEngine.Compilation.ImpromptuInterface.Dynamic;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Extensions;

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

    static public string FormOrQuery(this HttpRequest request, string key)
    {
        return request.HasFormContentType &&
               request.Form != null &&
               !String.IsNullOrWhiteSpace(request.Form[key]) ? request.Form[key] : request.Query[key];
    }

    static public string QueryOrForm(this HttpRequest request, string key, string defaultValue = "")
    {
        // Sollte nie "null" zurück geben

        if (!String.IsNullOrEmpty(request.Query[key]))
        {
            return request.Query[key];
        }

        return request.HasFormContentType &&
               request.Form != null &&
               !String.IsNullOrWhiteSpace(request.Form[key]) ? request.Form[key].ToString() : String.Empty;
    }

    static public NameValueCollection FormAndQueryParameters(this HttpRequest request)
    {
        NameValueCollection nvc = null;

        if (request.HasFormContentType)
        {
            nvc = new NameValueCollection(request.Form.ToCollection());
            foreach (string key in request.Query.Keys)
            {
                if (nvc.AllKeys.Contains(key))
                {
                    continue;
                }

                nvc.Add(key, request.Query[key]);
            }
        }
        else
        {
            nvc = new NameValueCollection(request.Query.ToCollection());
        }

        return nvc;
    }

    static public NameValueCollection FormOrQueryParameters(this HttpRequest request)
    {
        NameValueCollection nvc = null;

        if (request.HasFormContentType && request.Form.Keys.Count() > 0)
        {
            nvc = new NameValueCollection(request.Form.ToCollection());
        }
        else
        {
            nvc = new NameValueCollection(request.Query.ToCollection());
        }

        return nvc;
    }

    static public string MapName(this HttpRequest request)
    {
        if (request.HasFormContentType &&
            request.Form != null &&
            !String.IsNullOrEmpty(request.Form["mapname"]))
        {
            return request.Form["mapname"];
        }

        if (!String.IsNullOrEmpty(request.Query["mapname"]))
        {
            return request.Query["mapname"];
        }

        return "request";
    }

    static public Uri UrlReferrer(this HttpRequest request)
    {
        if (String.IsNullOrWhiteSpace(request.Headers["Referer"]))
        {
            return null;
        }

        return new Uri(request.Headers["Referer"].ToString());
    }

    static public Task<IActionResult> ToTask(this IActionResult actionResult)
    {
        return Task.FromResult<IActionResult>(actionResult);
    }

    static public HttpResponse AddNoCacheHeaders(this HttpResponse response)
    {
        response.Headers.TryAdd("Pragma", "no-cache");
        response.Headers.TryAdd("Cache-Control", "no-cache, no-store, max-age=0, must-revalidate");

        return response;
    }
    static public HttpResponse AddApiCorsHeaders(this HttpResponse response, HttpRequest request = null)
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
