using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Models;
using E.Standard.WebGIS.Core.Services;
using Microsoft.AspNetCore.Http;
using Portal.Core.AppCode.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services.Authentication;

public class ExtendedRoleParametersFromHeaderCustomAuthenticationMiddlewareService : ICustomPortalAuthenticationMiddlewareService
{
    private readonly IEnumerable<string> _headers;
    private readonly ITracerService _tracer;

    public ExtendedRoleParametersFromHeaderCustomAuthenticationMiddlewareService(ConfigurationService config,
                                                                                 ITracerService tracer = null)
    {
        _headers = config.ExtendedRoleParametersHeaders();
        _tracer = tracer;
    }

    public bool ForceInvoke(HttpContext httpContext) => true;

    public Task<CustomAuthenticationUser> InvokeFromMiddleware(HttpContext httpContext)
    {
        try
        {
            if (httpContext?.Request?.Headers == null)
            {
                return Task.FromResult<CustomAuthenticationUser>(null);
            }

            if (httpContext.User?.Identity != null && httpContext.User.Identity.IsAuthenticated)
            {
                List<string> roleParameters = new List<string>();

                if (_tracer != null && _tracer.Trace)
                {
                    foreach (var key in httpContext.Request.Headers.Keys)
                    {
                        var keyValue = httpContext.Request.Headers[key].ToString();
                        _tracer.Log(this, $"Header: {key}={keyValue?.Substring(0, Math.Min(keyValue.Length, 24))}");
                    }
                }

                foreach (var header in _headers)
                {
                    var requestHeader = httpContext.Request.Headers[header];

                    if (!String.IsNullOrEmpty(requestHeader))
                    {
                        roleParameters.Add($"{header}={requestHeader}");
                    }
                }

                if (roleParameters.Count > 0)
                {
                    return Task.FromResult(new CustomAuthenticationUser()
                    {
                        RoleParameters = roleParameters.ToArray(),
                        AppendRolesAndParameters = true
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception:{Environment.NewLine}{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }

        return Task.FromResult<CustomAuthenticationUser>(null);
    }
}
