using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using Microsoft.Extensions.Logging;
using System;

namespace Api.Core.AppCode.Services.Logging;

public class MicrosoftExceptionLogger : IExceptionLogger
{
    private ILogger<MicrosoftExceptionLogger> _logger;

    public MicrosoftExceptionLogger(ILogger<MicrosoftExceptionLogger> logger)
    {
        _logger = logger;
    }

    public void Flush() { }

    public void LogException(CmsDocument.UserIdentification ui, string server, string service, string command, Exception ex)
    {
        _logger.LogError("WebGIS.API: {server} {service} {command} {message} {stacktrace} - {username}", service, service, command, ex.Message, ex.StackTrace, ui?.Username ?? "");
    }

    public void LogException(IMap map, string server, string service, string command, Exception ex)
    {
        _logger.LogError("WebGIS.API: {server} {service} {command} {message} {stacktrace} - {map}", service, service, command, ex.Message, ex.StackTrace, map?.Name);
    }

    public void LogString(CmsDocument.UserIdentification ui, string server, string service, string command, string message, int performaceMilliseconds = 0)
    {
        _logger.LogError("WebGIS.API: {server} {service} {command} {message} - {username}", service, service, command, message, ui?.Username ?? "");
    }
}
