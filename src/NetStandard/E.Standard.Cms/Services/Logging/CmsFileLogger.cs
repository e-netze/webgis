using Microsoft.Extensions.Options;
using System;

namespace E.Standard.Cms.Services.Logging;

public class CmsFileLogger : CmsFileSystemLogger
{
    public CmsFileLogger(IOptions<CmsLoggerOptions> options)
        : base(options)
    {
    }

    protected override string CalcLine(string username, string method, string command, params string[] values)
    {
        return $"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()} ({username}): {method}.{command} {String.Join(" ", values)}";
    }
}
