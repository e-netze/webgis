using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace E.Standard.WebMapping.Core.Logging;

/// <summary>
/// Single entry point GeoServices use to start performance logging, e.g.
/// <c>requestContext.GetRequiredService&lt;GeoServicePerformanceLogService&gt;().StartGetMap(...)</c>.
/// Fans the call out to every configured <see cref="IGeoServicePerformanceLogger"/> backend (one
/// per comma-separated <c>Api:logging-type</c> entry, e.g. "files,microsoft,sqlserver") - callers
/// no longer need to know or care how many/which backends are active. Implements
/// <see cref="IGeoServicePerformanceLogger"/> itself purely so the existing
/// <c>StartInit()</c>/<c>StartGetMap()</c>/... extension methods
/// (<see cref="GeoServicePerformanceLoggerExtensions"/>) keep working unchanged.
/// </summary>
public sealed class GeoServicePerformanceLogService : IGeoServicePerformanceLogger
{
    private readonly IGeoServicePerformanceLogger[] _loggers;

    public GeoServicePerformanceLogService(IEnumerable<IGeoServicePerformanceLogger> loggers)
    {
        _loggers = loggers?.ToArray() ?? [];
    }

    public bool IsEnabled => _loggers.Any(static logger => logger.IsEnabled);

    public ILog Start(GeoServiceCommand cmd, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("")] GeoServicePerformanceLogMessage message = default)
    {
        if (_loggers.Length == 1)
        {
            // Common case (a single configured backend) - start it directly without the
            // CompositeLog wrapper allocation.
            return _loggers[0].Start(cmd, map, server, service, message);
        }

        // The message was already built once above (gated on this.IsEnabled, i.e. "does at least
        // one backend care") - forward the rendered text via AppendToMessage() instead of trying
        // to re-run the interpolated string handler per backend.
        string renderedMessage = message.ToString();

        var logs = new ILog[_loggers.Length];
        for (int i = 0; i < _loggers.Length; i++)
        {
            logs[i] = _loggers[i].Start(cmd, map, server, service);

            if (renderedMessage.Length > 0)
            {
                logs[i].AppendToMessage(renderedMessage);
            }
        }

        return new CompositeLog(logs);
    }

    public void Flush()
    {
        foreach (var logger in _loggers)
        {
            logger.Flush();
        }
    }
}
