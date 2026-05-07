using System;

using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

public interface IExceptionLogger
{
    void LogString(CmsDocument.UserIdentification ui, string server, string service, string cmd, string msg, int performaceMilliseconds = 0);
    void LogException(CmsDocument.UserIdentification ui, string server, string service, string command, Exception ex);
    void LogException(IMap map, string server, string service, string command, Exception ex);
    void Flush();
}
