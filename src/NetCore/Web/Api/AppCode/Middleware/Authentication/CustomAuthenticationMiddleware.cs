using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Services;
using Api.Core.AppCode.Services.Authentication;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Exceptions;
using E.Standard.Extensions.Compare;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Middleware.Authentication;

public class CustomAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiConfigurationService _apiConfig;

    public CustomAuthenticationMiddleware(RequestDelegate next,
                                          ApiConfigurationService config)
    {
        _next = next;
        _apiConfig = config;
    }

    async public Task Invoke(HttpContext httpContext,
                             UrlHelperService urlHelper,
                             RoutingEndPointReflectionService endpointReflection,
                             ApiCookieAuthenticationService cookies,
                             IEnumerable<ICustomApiAuthenticationMiddlewareService> customAuthentications = null)
    {
        if (customAuthentications != null)
        {
            foreach (var customAuthentication in customAuthentications)
            {
                if (httpContext.User.ApplyAuthenticationMiddleware(endpointReflection, customAuthentication.AuthTypes))
                {
                    try
                    {
                        var customAuthUser = await customAuthentication.InvokeFromMiddleware(httpContext);

                        if (customAuthUser != null)
                        {
                            var ui = new CmsDocument.UserIdentification(customAuthUser.Username,
                                                                        customAuthUser.Roles,
                                                                        customAuthUser.RoleParameters,
                                                                        _apiConfig.InstanceRoles,
                                                                        userId: customAuthUser.UserId ?? String.Empty);

                            cookies.SetAuthCookie(httpContext, customAuthUser.CookieValue.OrTake(customAuthUser.Username));

                            httpContext.User = ui.ToClaimsPrincipal(customAuthentication.AuthTypes);

                            break;
                        }
                    }
                    catch (CustomAuthenticationException cae)
                    {
                        httpContext.User = CmsDocument.UserIdentification.Anonymous.ToClaimsPrincipal(customAuthentication.AuthTypes, exceptionMessage: cae.Message);
                        break;
                    }
                    catch { }
                }
            }
        }

        await _next(httpContext);
    }
}
