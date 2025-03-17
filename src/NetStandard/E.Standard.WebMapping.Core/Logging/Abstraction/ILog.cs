using System;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

public interface ILog : IDisposable
{
    bool Success { get; set; }

    bool SuppressLogging { get; set; }

    string Server { get; }
    string Service { get; }
    string Command { get; }

    void AppendToMessage(string message);
}
