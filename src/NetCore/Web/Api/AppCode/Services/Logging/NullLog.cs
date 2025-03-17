using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace Api.Core.AppCode.Services.Logging;

public class NullLog : ILog
{
    public bool Success { get; set; }
    public bool SuppressLogging { get; set; }

    public string Server => "";

    public string Service => "";

    public string Command => "";

    public void AppendToMessage(string message)
    {

    }

    public void Dispose()
    {

    }
}