using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using System;
using System.IO;

namespace E.Standard.WebMapping.Core.Logging;

public class SimpleFileLogger : IWebGISLogger
{
    private static object thisLock = new object();
    private string _filename;
    private int _maxMB;

    public SimpleFileLogger(string filename, int maxMB)
    {
        _filename = filename;
        _maxMB = maxMB;
    }

    public SimpleFileLogger(SimpleFileLogger logger)
    {
        _filename = logger._filename;
        _maxMB = logger._maxMB;
    }

    #region IWebGISLogger Member

    public void LogString(string server, string service, string commad, string msg, int performaceMilliseconds = 0)
    {
        lock (thisLock)
        {
            StreamWriter sw = null;
            try
            {
                FileInfo fi = new FileInfo(_filename);
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }

                if (fi.Exists && fi.Length > _maxMB * 1024 * 1024)
                {
                    try { fi.Delete(); }
                    catch { }
                }

                sw = new StreamWriter(_filename, true);
                sw.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " " + msg);
            }
            catch { }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
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

    public IWebGISLogger Clone(IMap parent) => this;
    public IWebGISLogger Clone(CmsDocument.UserIdentification ui) => this;

    #endregion
}
