using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

public interface IGeoServicePerformanceLogger
{
    ILog Start(IMap map, string server, string service, string cmd, string message);
    void Flush();
}
