using E.Standard.WebMapping.Core.Logging.Abstraction;
using System;

namespace E.Standard.WebMapping.Core.Logging;

public class SimpleFilePerformanceLogger : ILog
{
    private IWebGISLogger _logger;
    private DateTime _startTime = DateTime.Now;
    private string _msg;
    private bool _success = false;

    public SimpleFilePerformanceLogger(IWebGISLogger logger, string msg)
    {
        _logger = logger;
        _msg = msg;
    }

    public string Server { get; set; }
    public string Service { get; set; }
    public string Command { get; set; }

    public void AppendToMessage(string str)
    {
        if (str == null)
        {
            return;
        }

        if (String.IsNullOrWhiteSpace(_msg))
        {
            _msg = str;
        }
        else
        {
            _msg += ", " + str;
        }
    }


    #region IDisposable Member

    public void Dispose()
    {
        if (_logger != null && !this.SuppressLogging)
        {
            DateTime n = DateTime.Now;

            TimeSpan ts = (n - _startTime);
            try
            {
                //if (ts.TotalMilliseconds > 0)
                _logger.LogString(this.Server, this.Service, this.Command, _msg + " " + ts.TotalMilliseconds + " " + _success.ToString(), Convert.ToInt32(ts.TotalMilliseconds));
            }
            catch { }
        }
    }

    #endregion

    #region IPerformanceLogger Member

    public bool Success
    {
        get
        {
            return _success;
        }
        set
        {
            _success = value;
        }
    }

    public bool SuppressLogging { get; set; }

    #endregion
}
