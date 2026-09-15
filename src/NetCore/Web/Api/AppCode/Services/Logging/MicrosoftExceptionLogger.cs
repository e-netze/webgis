using System;

using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;

using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Logging;

public partial class MicrosoftExceptionLogger : IExceptionLogger
{
    private readonly ILogger<MicrosoftExceptionLogger> _logger;

    public MicrosoftExceptionLogger(ILogger<MicrosoftExceptionLogger> logger)
    {
        _logger = logger;
    }

    public void Flush() { }

    public void LogException(CmsDocument.UserIdentification ui, string server, string service, string command, Exception ex)
        => LogExceptionForUser(ex, server, service, command, ui?.Username ?? "");

    public void LogException(IMap map, string server, string service, string command, Exception ex)
        => LogExceptionForMap(ex, server, service, command, map?.Name ?? "");

    public void LogString(CmsDocument.UserIdentification ui, string server, string service, string command, string message, int performaceMilliseconds = 0)
        => LogErrorString(server, service, command, message, ui?.Username ?? "");

    // Passing the Exception itself (instead of ex.Message/ex.StackTrace as plain strings) lets
    // the logging provider capture it natively (full stack trace, exception grouping in
    // Application Insights/Seq/...), instead of just flattened text.
    [LoggerMessage(EventId = LoggingEventIds.ExceptionForUser, Level = LogLevel.Error, Message = "WebGIS.API: {server} {service} {command} - {username}")]
    private partial void LogExceptionForUser(Exception exception, string server, string service, string command, string username);

    [LoggerMessage(EventId = LoggingEventIds.ExceptionForMap, Level = LogLevel.Error, Message = "WebGIS.API: {server} {service} {command} - {map}")]
    private partial void LogExceptionForMap(Exception exception, string server, string service, string command, string map);

    [LoggerMessage(EventId = LoggingEventIds.ExceptionString, Level = LogLevel.Error, Message = "WebGIS.API: {server} {service} {command} {message} - {username}")]
    private partial void LogErrorString(string server, string service, string command, string message, string username);
}
