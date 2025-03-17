using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using System;
using System.IO;

namespace E.Standard.WebMapping.Core.Logging;

public class SimpleServiceRequestLogger : IGeoServiceRequestLogger
{
    private static object thisLock = new object();
    private string _path;
    private int _maxMB;

    public SimpleServiceRequestLogger(string path, int maxMB)
    {
        _path = path;
        _maxMB = maxMB;
    }

    public SimpleServiceRequestLogger(SimpleServiceRequestLogger logger)
    {
        _path = logger._path;
        _maxMB = logger._maxMB;
    }

    #region IWebGISLogger Member

    public void LogString(string server, string service, string command, string msg, int performaceMilliseconds = 0)
    {
        lock (thisLock)
        {
            StreamWriter sw = null;
            try
            {
                string fileName = $"{_path}/{service}/{command}.log";
                FileInfo fi = new FileInfo(fileName);

                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }

                if (fi.Exists && fi.Length > _maxMB * 1024 * 1024)
                {
                    try { fi.Delete(); }
                    catch { }
                }

                using (sw = new StreamWriter(fileName, true))
                {
                    sw.WriteLine();
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}: {server}\n{msg}");
                }
            }
            catch { }
        }
    }

    public ILog PerformanceLogger(string server, string service, string cmd, string message)
    {
        return new SimpleFilePerformanceLogger(this, message)
        {
            Server = server,
            Service = service,
            Command = cmd
        };
    }

    public IWebGISLogger Clone(IMap map) => this;
    public IWebGISLogger Clone(CmsDocument.UserIdentification ui) => this;

    #endregion
}
