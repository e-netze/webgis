using System;
using System.Collections.Generic;
using System.Linq;

using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace E.Standard.WebMapping.Core.Logging;

/// <summary>
/// Single entry point GeoServices/host code use for exception/error logging, e.g.
/// <c>requestContext.GetRequiredService&lt;ExceptionLogService&gt;().LogException(...)</c>.
/// Fans the call out to every configured <see cref="IExceptionLogger"/> backend (one per
/// comma-separated <c>Api:logging-type</c> entry, e.g. "files,microsoft,sqlserver") - callers no
/// longer need to know or care how many/which backends are active. Mirrors
/// <see cref="GeoServicePerformanceLogService"/>.
/// </summary>
public sealed class ExceptionLogService : IExceptionLogger
{
    private readonly IExceptionLogger[] _loggers;

    public ExceptionLogService(IEnumerable<IExceptionLogger> loggers)
    {
        _loggers = loggers?.ToArray() ?? [];
    }

    public void Flush()
    {
        foreach (var logger in _loggers)
        {
            logger.Flush();
        }
    }

    public void LogException(CmsDocument.UserIdentification ui, string server, string service, string command, Exception ex)
    {
        foreach (var logger in _loggers)
        {
            logger.LogException(ui, server, service, command, ex);
        }
    }

    public void LogException(IMap map, string server, string service, string command, Exception ex)
    {
        foreach (var logger in _loggers)
        {
            logger.LogException(map, server, service, command, ex);
        }
    }

    public void LogString(CmsDocument.UserIdentification ui, string server, string service, string cmd, string msg, int performaceMilliseconds = 0)
    {
        foreach (var logger in _loggers)
        {
            logger.LogString(ui, server, service, cmd, msg, performaceMilliseconds);
        }
    }
}
