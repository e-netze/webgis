using System;
using System.Collections.Specialized;
using System.Reflection;

using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Models;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.IO;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Api;

public class PortalMapAuthorAuthorizationService
{
    private readonly ILogger<PortalMapAuthorAuthorizationService> _logger;
    private readonly ApiToolsService _apiTools;

    public PortalMapAuthorAuthorizationService(ILogger<PortalMapAuthorAuthorizationService> logger,
                                               ApiToolsService apiTools)
    {
        _logger = logger;
        _apiTools = apiTools;
    }

    public void EnsureAuthorized(HttpContext httpContext, MethodInfo methodInfo, object serverCommandInstance, IBridge bridge, string method)
    {
        if (methodInfo.GetCustomAttribute<PortalMapAuthorRequiredAttribute>(true) == null)
        {
            return;
        }

        string pageId = (serverCommandInstance as IStorageInteractions)?.StoragePathFormatParameter(bridge, 0);
        string username = bridge?.CurrentUser?.Username;

        if (!IsMapAuthor(httpContext, pageId, username, bridge?.CurrentUser?.UserRoles))
        {
            _logger.LogWarning("Unauthorized portal map author command: user={username} page={pageId} tool={tool} method={method}",
                username, pageId, serverCommandInstance?.GetType().FullName, method);

            throw new Exception("Not authorized");
        }
    }

    private bool IsMapAuthor(HttpContext httpContext, string pageId, string username, string[] userRoles)
    {
        if (String.IsNullOrWhiteSpace(pageId) ||
            String.IsNullOrWhiteSpace(username) ||
            pageId.IndexOfAny(new[] { '/', '\\' }) >= 0 ||
            pageId.Contains(".."))
        {
            return false;
        }

        var portalPage = _apiTools.ExecuteToolCommand<E.Standard.WebGIS.Tools.Portal.Portal, ApiPortalPageDTO>(
            httpContext,
            "page",
            new NameValueCollection() { { "page-id", pageId } },
            username);

        if (portalPage?.MapAuthors == null || portalPage.MapAuthors.Length == 0)
        {
            return false;
        }

        return UserManagement.IsAllowed(username, portalPage.MapAuthors) ||
               UserManagement.IsAllowed(userRoles, portalPage.MapAuthors);
    }
}
