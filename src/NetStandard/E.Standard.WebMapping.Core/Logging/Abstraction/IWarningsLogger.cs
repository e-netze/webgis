using E.Standard.CMS.Core;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

public interface IWarningsLogger
{
    void LogString(CmsDocument.UserIdentification ui, string server, string service, string cmd, string msg, int performaceMilliseconds = 0);
    void Flush();
}
