using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

public interface IWebGISLogger :
    IClone<IWebGISLogger, IMap>,
    IClone<IWebGISLogger, CmsDocument.UserIdentification>
{
    void LogString(string server, string service, string cmd, string msg, int performaceMilliseconds = 0);

    ILog PerformanceLogger(string server, string service, string cmd, string message);
}
