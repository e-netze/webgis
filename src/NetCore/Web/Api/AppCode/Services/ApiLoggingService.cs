using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services.Logging;
using E.Standard.Api.App.Configuration;
using E.Standard.Api.App.Exceptions;
using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using Microsoft.Extensions.Configuration;
using System;

namespace Api.Core.AppCode.Services;

public class ApiLoggingService
{
    private readonly IRequestContext _requestContext;
    private readonly IConfiguration _configuration;

    public ApiLoggingService(
            IRequestContext requestContext,
            IConfiguration configuration)
    {
        _requestContext = requestContext;
        _configuration = configuration;
    }

    public void LogReportException(ReportWarningException rwe, CmsDocument.UserIdentification ui)
    {
        if (rwe is ReportExceptionException)
        {
            var ree = (ReportExceptionException)rwe;

            _requestContext.GetRequiredService<IExceptionLogger>()
                .LogString(ui, ree.Server, ree.Service, ree.Command, $"Exception: {ree.Message}");
        }
        else
        {
            _requestContext.GetRequiredService<IExceptionLogger>()
                .LogString(ui, rwe.Server, rwe.Service, rwe.Command, $"Warning: {rwe.Message}");
        }
    }

    public ILog UsagePerformaceLogger(ApiBaseController controller, IApiButton button, string eventType, CmsDocument.UserIdentification ui)
    {
        if (_configuration[ApiConfigKeys.LoggingLogUsage]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
        {
            return _requestContext.GetRequiredService<IUsagePerformanceLogger>().Start(
                    ui,
                    controller?.CurrentActionName ?? String.Empty,
                    controller?.CurrentActionName ?? String.Empty,
                    eventType,
                    button?.GetType().ToToolId() ?? String.Empty
                );
        }

        return new NullLog();
    }

    public ILog UsagePerformaceLogger(ApiBaseController controller, string command, string message, CmsDocument.UserIdentification ui)
    {
        if (_configuration[ApiConfigKeys.LoggingLogUsage]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
        {
            return _requestContext.GetRequiredService<IUsagePerformanceLogger>().Start(
                ui,
                controller?.CurrentActionName ?? String.Empty,
                controller?.CurrentActionName ?? String.Empty,
                command,
                message
            );
        }

        return new NullLog();
    }
}
