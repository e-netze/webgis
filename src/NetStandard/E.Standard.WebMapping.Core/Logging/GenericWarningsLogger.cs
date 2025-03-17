using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace E.Standard.WebMapping.Core.Logging;

public class GenericWarningsLogger<TLogger> : IWarningsLogger
    where TLogger : IWebGISLogger
{
    protected readonly TLogger _logger;

    public GenericWarningsLogger(TLogger logger)
    {
        _logger = logger;
    }

    virtual public void Flush() { }

    public void LogString(CmsDocument.UserIdentification ui, string server, string service, string cmd, string msg, int performaceMilliseconds = 0)
        => _logger.Clone(ui).LogString(server, service, cmd, msg, performaceMilliseconds);
}
