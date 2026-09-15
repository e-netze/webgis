#nullable enable

using System.Runtime.CompilerServices;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;

using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Services.Logging;

public class MicrosoftOgcPerformanceLogger : IOgcPerformanceLogger
{
    private ILogger<MicrosoftOgcPerformanceLogger> _logger;

    public MicrosoftOgcPerformanceLogger(ILogger<MicrosoftOgcPerformanceLogger> logger)
    {
        _logger = logger;
    }

    public bool IsEnabled => _logger.IsEnabled(LogLevel.Information);

    public void Flush() { }

    public ILog Start(GeoServiceCommand cmd, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("")] GeoServicePerformanceLogMessage message = default)
    {
        if (!this.IsEnabled)
        {
            return NullLog.Instance;
        }

        return new MicrosoftLog(
            _logger,
            map, null,
            "WebGIS.API OGC Performance", server, service, cmd.ToString(), message.ToString()
            );
    }

    public ILog Start(string cmd, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("")] GeoServicePerformanceLogMessage message = default)
    {
        if (!this.IsEnabled)
        {
            return NullLog.Instance;
        }

        return new MicrosoftLog(
            _logger,
            map, null,
            "WebGIS.API OGC Performance", server, service, cmd, message.ToString()
            );
    }
}
