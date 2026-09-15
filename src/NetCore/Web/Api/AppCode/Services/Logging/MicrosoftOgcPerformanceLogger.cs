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

    // Warning (not Information) - a failed request is logged at Warning by MicrosoftLog, so the
    // message must still be built whenever Warning is enabled, even if Information is not.
    public bool IsEnabled => _logger.IsEnabled(LogLevel.Warning);

    public void Flush() { }

    public ILog Start(GeoServiceCommand cmd, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("")] GeoServicePerformanceLogMessage message = default)
    {
        return new MicrosoftLog(
            _logger,
            map, null,
            "ogc", cmd.ToEventId(),
            "WebGIS.API OGC Performance", server, service, cmd.ToString(), message.ToString()
            );
    }

    public ILog Start(string cmd, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("")] GeoServicePerformanceLogMessage message = default)
    {
        return new MicrosoftLog(
            _logger,
            map, null,
            "ogc", cmd.ToOgcEventId(),
            "WebGIS.API OGC Performance", server, service, cmd, message.ToString()
            );
    }
}
