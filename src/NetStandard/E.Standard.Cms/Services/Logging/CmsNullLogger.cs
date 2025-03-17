using E.Standard.Cms.Abstraction;

namespace E.Standard.Cms.Services.Logging;

public class CmsNullLogger : ICmsLogger
{
    public void Log(string username,
                    string method,
                    string command,
                    params string[] values)
    {

    }
}
