using E.Standard.Security.App.Json;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.Core.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Specialized;

namespace Portal.Core.AppCode.Extensions;

static public class TraceExtensions
{
    static public void TraceRequest(this ITracerService tracer, object source, HttpContext httpContext, string headerPrefix)
    {
        if (tracer != null && tracer.Trace == true)
        {
            NameValueCollection headers = httpContext.Request.HeadersCollection();
            if (tracer != null && tracer.Trace)
            {
                tracer.Log(source, $"Request HttpHeaders ({headerPrefix}*):");
                foreach (string key in headers.Keys)
                {
                    if (key.StartsWith(headerPrefix, StringComparison.CurrentCultureIgnoreCase))
                    {
                        tracer.Log(source, $">> {key}: {headers[key]}");
                    }
                }
            }
        }
    }

    static public void TracePortalUser(this ITracerService tracer, object source, HttpContext httpContext, ApplicationSecurityConfig applicationSecurityConfig)
    {
        if (tracer != null && tracer.Trace == true)
        {
            var portalUser = httpContext.User.ToPortalUser(applicationSecurityConfig);

            tracer.Log(source, "PortalUser:");
            tracer.Log(source, $">> Username: {portalUser.Username}");
            if (portalUser.UserRoles != null)
            {
                tracer.Log(source, $">> Roles: {String.Join(", ", portalUser.UserRoles)}");
            }
            if (portalUser.RoleParameters != null)
            {
                tracer.Log(source, $">> RoleParameters: {String.Join(", ", portalUser.RoleParameters)}");
            }
        }
    }

    static public void TraceMessage(this ITracerService tracer, object source, string message)
    {
        if (tracer != null && tracer.Trace == true)
        {
            tracer.Log(source, message);
        }
    }
}
