using System;

using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace E.Standard.WebMapping.Core.Logging;

/// <summary>
/// Combines the <see cref="ILog"/> instances returned by starting several
/// <see cref="IGeoServicePerformanceLogger"/> backends (see <see cref="GeoServicePerformanceLogService"/>)
/// into a single disposable, so callers keep writing a single <c>using var pLogger = ...</c> block
/// no matter how many logging backends ("files,microsoft,sqlserver", ...) are configured.
/// </summary>
internal sealed class CompositeLog : ILog
{
    private readonly ILog[] _logs;

    public CompositeLog(ILog[] logs)
    {
        _logs = logs;
    }

    public bool Success
    {
        get => _logs.Length > 0 && _logs[0].Success;
        set
        {
            foreach (var log in _logs)
            {
                log.Success = value;
            }
        }
    }

    public bool SuppressLogging
    {
        get => _logs.Length > 0 && _logs[0].SuppressLogging;
        set
        {
            foreach (var log in _logs)
            {
                log.SuppressLogging = value;
            }
        }
    }

    public string Server => _logs.Length > 0 ? _logs[0].Server : String.Empty;

    public string Service => _logs.Length > 0 ? _logs[0].Service : String.Empty;

    public string Command => _logs.Length > 0 ? _logs[0].Command : String.Empty;

    public void AppendToMessage(string message)
    {
        foreach (var log in _logs)
        {
            log.AppendToMessage(message);
        }
    }

    public void Dispose()
    {
        foreach (var log in _logs)
        {
            log.Dispose();
        }
    }
}
