using E.Standard.CMS.Core;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

public interface IUsagePerformanceLogger
{
    ILog Start(CmsDocument.UserIdentification ui, string server, string service, string cmd, string message);
    void Flush();
}
