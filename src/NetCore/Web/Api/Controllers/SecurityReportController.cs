#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services;
using Api.Core.AppCode.Services.Security;

using E.Standard.Custom.Core.Abstractions;
using E.Standard.Web.Abstractions;
using E.Standard.WebApp.Reflection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Api.Core.Controllers;

/// <summary>
/// Security report: shows the security-relevant api.config settings, their risk and a traffic light for internet and intranet
/// installations. Independent of the subscriber login (which may be disabled).
/// Access: secure-endpoint credentials (url password / basic / bearer token) or a direct local request (localhost).
/// Without configured credentials the page is only reachable locally (fail-closed).
/// </summary>
[EndpointAuthorization(AuthorizationType = EndpointAuthorizationType.UrlPassword
                                           | EndpointAuthorizationType.Basic
                                           | EndpointAuthorizationType.BearerToken
                                           | EndpointAuthorizationType.Localhost)]
public class SecurityReportController : ApiBaseController
{
    private static readonly string[] ForwardedHeaders =
    {
        "X-Forwarded-For", "X-Forwarded-Host", "X-Forwarded-Proto", "Forwarded", "X-Original-For", "X-Real-IP"
    };

    private readonly SecurityReportService _report;
    private readonly SubscriberSecurityOverviewService _subscribers;
    private readonly ILogger<SecurityReportController> _logger;

    public SecurityReportController(ILogger<SecurityReportController> logger,
                                    SecurityReportService report,
                                    SubscriberSecurityOverviewService subscribers,
                                    UrlHelperService urlHelper,
                                    IHttpService http,
                                    IEnumerable<ICustomApiService>? customServices = null)
        : base(logger, urlHelper, http, customServices)
    {
        _report = report;
        _subscribers = subscribers;
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // The endpoint middleware lets requests pass if no credentials are configured. Never allow that remotely.
        if (!_report.HasEndpointCredentials && !IsDirectLocalRequest())
        {
            context.Result = new ContentResult
            {
                StatusCode = 403,
                ContentType = "text/plain",
                Content = "Security report: configure security:secure-endpoint-url-password (or basicauth) in api.config, or open this page directly on the server (localhost)."
            };
            return;
        }

        base.OnActionExecuting(context);
    }

    public IActionResult Index()
    {
        var report = _report.CreateReport();

        // the subscriber list is only relevant (and only loaded) if the login is enabled
        if (report.LoginEnabled)
        {
            try
            {
                report.Subscribers = _subscribers.GetSubscribers();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Security report: reading subscribers failed");
                report.SubscribersError = true;
            }
        }

        return ViewResult(report);
    }

    private bool IsDirectLocalRequest()
    {
        var remote = HttpContext.Connection.RemoteIpAddress;

        if (remote == null || !(IPAddress.IsLoopback(remote) || remote.Equals(HttpContext.Connection.LocalIpAddress)))
        {
            return false;
        }

        return !ForwardedHeaders.Any(h => Request.Headers.ContainsKey(h));
    }
}
