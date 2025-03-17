using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace E.Standard.WebMapping.Core.Logging;

public class GenericUsagePerformanceLogger<TLogger> : IUsagePerformanceLogger
    where TLogger : IWebGISLogger
{
    protected readonly TLogger _logger;

    public GenericUsagePerformanceLogger(TLogger logger)
    {
        _logger = logger;
    }

    virtual public void Flush() { }

    public ILog Start(CmsDocument.UserIdentification ui, string server, string service, string cmd, string message)
        => _logger.Clone(ui).PerformanceLogger(server, service, cmd, message);
}
