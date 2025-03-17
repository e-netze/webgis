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

    virtual public void Flush() { }

    public ILog Start(IMap map, string server, string service, string cmd, string message)
        => _logger.Clone(map).PerformanceLogger(server, service, cmd, message);
}
