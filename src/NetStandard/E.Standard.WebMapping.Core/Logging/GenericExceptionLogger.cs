using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using System;

namespace E.Standard.WebMapping.Core.Logging;

abstract public class GenericExceptionLogger<TLogger> : IExceptionLogger
    where TLogger : IWebGISLogger
{
    protected readonly TLogger _logger;

    public GenericExceptionLogger(TLogger logger)
    {
        _logger = logger;
    }

    virtual public void Flush() { }

    abstract public void LogException(CmsDocument.UserIdentification ui, string server, string service, string command, Exception ex);

    abstract public void LogException(IMap map, string server, string service, string command, Exception ex);

    public void LogString(CmsDocument.UserIdentification ui, string server, string service, string cmd, string msg, int performaceMilliseconds = 0)
        => _logger.Clone(ui).LogString(server, service, cmd, msg, performaceMilliseconds);
}