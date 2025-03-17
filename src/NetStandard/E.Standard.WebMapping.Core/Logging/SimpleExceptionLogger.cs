using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using System;
using System.Text;

namespace E.Standard.WebMapping.Core.Logging;

public class SimpleExceptionLogger : SimpleFileLogger
{
    private IMap _map = null;
    private string _username = null;

    public SimpleExceptionLogger(string filename, int maxMB, IMap map)
        : base(filename, maxMB)
    {
        _map = map;
    }

    private SimpleExceptionLogger(IMap map, SimpleExceptionLogger logger)
        : base(logger)
    {
        _map = map;
    }

    public bool UseServerContainer { get; set; }
    public string ServerContainerAppName { get; set; }

    #region IExceptionLogger Member

    public void LogException(string server, string service, string command, Exception ex)
    {
        try
        {
            int i = 0;
            StringBuilder sb = new StringBuilder();
            Exception originException = ex;

            while (ex != null)
            {

                if (_map != null && !String.IsNullOrEmpty(_map.Name))
                {
                    sb.Append("Map: " + _map.Name + "\r\n");
                }

                sb.Append("Exception Type: " + ex.GetType().ToString() + " \r\n");
                sb.Append("    Message   : " + ex.Message + "\r\n");
                sb.Append("    Source    : " + ex.Source + "\r\n");
                sb.Append("    Stacktrace: " + ex.StackTrace);

                //Exception baseEx = ex.GetBaseException();
                //if (baseEx != null)
                //{
                //    sb.Append("\r\n      Base Exception Type: " + baseEx.GetType().ToString() + " \r\n");
                //    sb.Append("                   Message   : " + baseEx.Message);
                //}


                ex = ex.InnerException;
                i++;
                if (i > 20)
                {
                    break;
                }

                if (ex != null)
                {
                    sb.Append("\r\n*InnerException:\r\n");
                }
            }
            base.LogString(server, service, command, sb.ToString());
        }
        catch { }
    }

    new public IWebGISLogger Clone(IMap map)
    {
        var clone = new SimpleExceptionLogger(map, this);

        clone.UseServerContainer = this.UseServerContainer;
        clone.ServerContainerAppName = this.ServerContainerAppName;

        return clone;
    }

    new public IWebGISLogger Clone(CmsDocument.UserIdentification ui)
    {
        var clone = new SimpleExceptionLogger(null, this);

        clone.UseServerContainer = this.UseServerContainer;
        clone.ServerContainerAppName = this.ServerContainerAppName;
        clone._username = ui?.Username ?? "";

        return clone;
    }

    #endregion
}
