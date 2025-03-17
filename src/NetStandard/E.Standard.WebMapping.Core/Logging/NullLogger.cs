using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using System;

namespace E.Standard.WebMapping.Core.Logging;

public class NullLogger : IWebGISLogger, IGeoServiceRequestLogger
{
    #region IWebGISLogger Member

    public void LogString(string server, string service, string command, string msg, int performaceMilliseconds = 0)
    {
    }

    public ILog PerformanceLogger(String server, string service, string cmd, string message)
    {
        return new PerformanceNullLogger();
    }

    public IWebGISLogger Clone(IMap parent) => this;

    public IWebGISLogger Clone(CmsDocument.UserIdentification parent) => this;

    public void Flush()
    {

    }

    #endregion

    #region Classes

    private class PerformanceNullLogger : ILog
    {
        #region IDisposable Member

        public void Dispose()
        {
        }

        #endregion

        #region IPerformanceLogger Member

        public string Server { get; set; }
        public string Service { get; set; }
        public string Command { get; set; }
        public bool Success
        {
            get
            {
                return true;
            }
            set
            {

            }
        }

        public bool SuppressLogging { get; set; }

        public void AppendToMessage(string str) { }

        #endregion
    }

    #endregion
}
