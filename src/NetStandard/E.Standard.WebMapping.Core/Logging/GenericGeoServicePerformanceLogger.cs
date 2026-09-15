using System.Runtime.CompilerServices;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace E.Standard.WebMapping.Core.Logging;

public class GenericGeoServicePerformanceLogger<TLogger> : IGeoServicePerformanceLogger
    where TLogger : IWebGISLogger
{
    protected readonly TLogger _logger;

    public GenericGeoServicePerformanceLogger(TLogger logger)
    {
        _logger = logger;
    }

    virtual public bool IsEnabled => true;

    virtual public void Flush() { }

    public ILog Start(GeoServiceCommand cmd, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("")] GeoServicePerformanceLogMessage message = default)
        => _logger.Clone(map).PerformanceLogger(server, service, cmd.ToString(), message.ToString());

    /// <summary>
    /// Used by <see cref="IOgcPerformanceLogger"/> implementations for the dynamic
    /// "service/request" command strings of OGC requests.
    /// </summary>
    public ILog Start(string cmd, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("")] GeoServicePerformanceLogMessage message = default)
        => _logger.Clone(map).PerformanceLogger(server, service, cmd, message.ToString());
}
