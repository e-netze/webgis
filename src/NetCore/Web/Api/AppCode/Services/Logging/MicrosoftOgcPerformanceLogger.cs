#nullable enable

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

    public void Flush() { }

    public ILog Start(IMap map, string server, string service, string cmd, string message)
    {
        return new MicrosoftLog(
            _logger,
            map, null,
            "WebGIS.API OGC Performance", server, service, cmd, message
            );
    }
}
