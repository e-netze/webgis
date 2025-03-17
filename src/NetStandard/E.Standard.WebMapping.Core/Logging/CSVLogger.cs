using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.WebMapping.Core.Logging;

public class CSVLogger : IWebGISLogger
{
    private static System.Globalization.NumberFormatInfo _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
    private int _bufferLength = 0;
    private StreamBuffer _buffer = null;

    private enum LogColumn
    {
        SessionId = 0,
        RequestId = 1,
        Date = 2,
        Time = 3,
        MapName = 4,
        ClientIp = 5,
        X = 6,
        Y = 7,
        Scale = 8,
        Custom = 9,
        UserName = 10
    }

    private static object thisLock = new object();
    private object localLock = new object();

    private string _filename;
    private int _maxMB, _digits = 0;
    private IMap _map = null;
    private string _username = null;

    private Dictionary<LogColumn, string> _columns = null;

    public CSVLogger(IMap map, CSVLogger logger)
    {
        _bufferLength = logger._bufferLength;
        _buffer = logger._buffer;
        _filename = logger._filename;
        _maxMB = logger._maxMB;
        _digits = logger._digits;
        _columns = logger._columns;

        _map = map;

        UseServerContainer = false;
    }
    public CSVLogger(StreamBuffer buffer, IMap map, string columns, int digits)
    {
        _map = map;
        _digits = digits;

        if (!String.IsNullOrEmpty(columns))
        {
            _columns = new Dictionary<LogColumn, string>();
            foreach (string column in columns.ToUpper().Replace(",", ";").Split(';'))
            {
                if (column.StartsWith("$"))
                {
                    string col = column.Substring(1, column.Length - 1);
                    if (!_columns.ContainsKey(LogColumn.Custom))
                    {
                        _columns.Add(LogColumn.Custom, col);
                    }
                    else
                    {
                        _columns[LogColumn.Custom] = _columns[LogColumn.Custom] + ";" + col;
                    }
                }
                else
                {
                    switch (column)
                    {
                        case "SESSIONID":
                            if (!_columns.ContainsKey(LogColumn.SessionId))
                            {
                                _columns.Add(LogColumn.SessionId, String.Empty);
                            }

                            break;
                        case "MAPREQUESTID":
                            if (!_columns.ContainsKey(LogColumn.RequestId))
                            {
                                _columns.Add(LogColumn.RequestId, String.Empty);
                            }

                            break;
                        case "DATE":
                            if (!_columns.ContainsKey(LogColumn.Date))
                            {
                                _columns.Add(LogColumn.Date, String.Empty);
                            }

                            break;
                        case "TIME":
                            if (!_columns.ContainsKey(LogColumn.Time))
                            {
                                _columns.Add(LogColumn.Time, String.Empty);
                            }

                            break;
                        case "MAPNAME":
                            if (!_columns.ContainsKey(LogColumn.MapName))
                            {
                                _columns.Add(LogColumn.MapName, String.Empty);
                            }

                            break;
                        case "CLIENTIP":
                            if (!_columns.ContainsKey(LogColumn.ClientIp))
                            {
                                _columns.Add(LogColumn.ClientIp, String.Empty);
                            }

                            break;
                        case "X":
                            if (!_columns.ContainsKey(LogColumn.X))
                            {
                                _columns.Add(LogColumn.X, String.Empty);
                            }

                            break;
                        case "Y":
                            if (!_columns.ContainsKey(LogColumn.Y))
                            {
                                _columns.Add(LogColumn.Y, String.Empty);
                            }

                            break;
                        case "SCALE":
                            if (!_columns.ContainsKey(LogColumn.Scale))
                            {
                                _columns.Add(LogColumn.Scale, String.Empty);
                            }

                            break;
                        case "USERNAME":
                            if (!_columns.ContainsKey(LogColumn.UserName))
                            {
                                _columns.Add(LogColumn.UserName, String.Empty);
                            }

                            break;
                    }
                }
            }
            if (_columns.Count == 0)
            {
                _columns = null;
            }
        }

        _buffer = buffer;

        UseServerContainer = false;
    }

    public bool UseServerContainer { get; set; }
    public string ServerContainerAppName { get; set; }

    public void FlushBuffer() => _buffer?.Flush();

    #region IWebGISLogger Member

    public void LogString(string server, string service, string command, string msg, int performaceMilliseconds = 0)
    {
        if (_buffer == null)
        {
            return;
        }

        lock (localLock)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                if (_map != null)
                {
                    if (_columns == null)
                    {
                        sb.Append((string)_map.Environment.UserValue("SessionID", String.Empty) + ";");
                        sb.Append(_map.RequestId.ToSimpleRequestId() + ";");
                        sb.Append(DateTime.Now.ToShortDateString() + ";");
                        sb.Append(DateTime.Now.ToLongTimeString() + ";");
                        sb.Append(_map.Name + ";");
                    }
                    else
                    {
                        foreach (LogColumn column in _columns.Keys)
                        {
                            switch (column)
                            {
                                case LogColumn.SessionId:
                                    sb.Append((string)_map.Environment.UserValue("SessionID", String.Empty) + ";");
                                    break;
                                case LogColumn.RequestId:
                                    sb.Append(_map.RequestId.ToSimpleRequestId() + ";");
                                    break;
                                case LogColumn.Date:
                                    sb.Append(DateTime.Now.ToShortDateString() + ";");
                                    break;
                                case LogColumn.Time:
                                    sb.Append(DateTime.Now.ToLongTimeString() + ";");
                                    break;
                                case LogColumn.MapName:
                                    sb.Append(_map.Name + ";");
                                    break;
                                case LogColumn.ClientIp:
                                    sb.Append((string)_map.Environment.UserValue("ClientIp", String.Empty) + ";");
                                    break;
                                case LogColumn.UserName:
                                    if (_map != null)
                                    {
                                        sb.Append((string)_map.Environment.UserValue("username", String.Empty) + ";");
                                    }
                                    else
                                    {
                                        sb.Append($"{_username ?? String.Empty};");
                                    }
                                    break;
                                case LogColumn.X:
                                    sb.Append(Math.Round(_map.Extent.CenterPoint.X, 2).ToString() + ";");
                                    break;
                                case LogColumn.Y:
                                    sb.Append(Math.Round(_map.Extent.CenterPoint.Y, 2).ToString() + ";");
                                    break;
                                case LogColumn.Scale:
                                    sb.Append(Math.Round(_map.MapScale).ToString() + ";");
                                    break;
                                case LogColumn.Custom:
                                    foreach (string customCol in _columns[column].Split(';'))
                                    {
                                        sb.Append(_map.Environment.UserString("logstring:" + customCol) + ";");
                                    }
                                    break;
                            }
                        }

                    }
                    sb.Append(msg.Replace(" ", ";"));

                    _buffer.Append(sb.ToString() + "\r\n");
                }
                else
                {
                    sb.Append($"{DateTime.Now.ToShortDateString()};");
                    sb.Append($"{DateTime.Now.ToLongTimeString()};");
                    sb.Append($"{_username};");
                    sb.Append($"{command};");
                    sb.Append($"{msg};");

                    _buffer.Append(sb.ToString() + "\r\n");
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

    public IWebGISLogger Clone(CmsDocument.UserIdentification ui)
    {
        var clone = new CSVLogger(null, this);

        clone.UseServerContainer = this.UseServerContainer;
        clone.ServerContainerAppName = this.ServerContainerAppName;
        clone._username = ui?.Username ?? "";

        return clone;
    }

    public IWebGISLogger Clone(IMap map)
    {
        var clone = new CSVLogger(map, this);

        clone.UseServerContainer = this.UseServerContainer;
        clone.ServerContainerAppName = this.ServerContainerAppName;

        return clone;
    }

    #endregion

    #region Helper

    public static string CreateHeader(string columns)
    {
        StringBuilder sb = new StringBuilder();

        if (!String.IsNullOrEmpty(columns))
        {
            Dictionary<LogColumn, string> logColumns = new Dictionary<LogColumn, string>();
            foreach (string column in columns.ToUpper().Replace(",", ";").Split(';'))
            {
                if (column.StartsWith("$"))
                {
                    string col = column.Substring(1, column.Length - 1);
                    if (!logColumns.ContainsKey(LogColumn.Custom))
                    {
                        logColumns.Add(LogColumn.Custom, col);
                    }
                    else
                    {
                        logColumns[LogColumn.Custom] = logColumns[LogColumn.Custom] + ";" + col;
                    }
                }
                else
                {
                    switch (column)
                    {
                        case "SESSIONID":
                            if (!logColumns.ContainsKey(LogColumn.SessionId))
                            {
                                logColumns.Add(LogColumn.SessionId, String.Empty);
                            }

                            break;
                        case "MAPREQUESTID":
                            if (!logColumns.ContainsKey(LogColumn.RequestId))
                            {
                                logColumns.Add(LogColumn.RequestId, String.Empty);
                            }

                            break;
                        case "DATE":
                            if (!logColumns.ContainsKey(LogColumn.Date))
                            {
                                logColumns.Add(LogColumn.Date, String.Empty);
                            }

                            break;
                        case "TIME":
                            if (!logColumns.ContainsKey(LogColumn.Time))
                            {
                                logColumns.Add(LogColumn.Time, String.Empty);
                            }

                            break;
                        case "MAPNAME":
                            if (!logColumns.ContainsKey(LogColumn.MapName))
                            {
                                logColumns.Add(LogColumn.MapName, String.Empty);
                            }

                            break;
                        case "USERNAME":
                            if (!logColumns.ContainsKey(LogColumn.UserName))
                            {
                                logColumns.Add(LogColumn.UserName, String.Empty);
                            }

                            break;
                        case "CLIENTIP":
                            if (!logColumns.ContainsKey(LogColumn.ClientIp))
                            {
                                logColumns.Add(LogColumn.ClientIp, String.Empty);
                            }

                            break;
                        case "X":
                            if (!logColumns.ContainsKey(LogColumn.X))
                            {
                                logColumns.Add(LogColumn.X, String.Empty);
                            }

                            break;
                        case "Y":
                            if (!logColumns.ContainsKey(LogColumn.Y))
                            {
                                logColumns.Add(LogColumn.Y, String.Empty);
                            }

                            break;
                        case "SCALE":
                            if (!logColumns.ContainsKey(LogColumn.Scale))
                            {
                                logColumns.Add(LogColumn.Scale, String.Empty);
                            }

                            break;
                    }
                }
            }
            if (logColumns != null)
            {

                foreach (LogColumn logColumn in logColumns.Keys)
                {
                    switch (logColumn)
                    {
                        case LogColumn.SessionId:
                            sb.Append("SESSIONID;");
                            break;
                        case LogColumn.RequestId:
                            sb.Append("MAPREQUESTID;");
                            break;
                        case LogColumn.Date:
                            sb.Append("DATE;");
                            break;
                        case LogColumn.Time:
                            sb.Append("TIME;");
                            break;
                        case LogColumn.MapName:
                            sb.Append("MAPNAME;");
                            break;
                        case LogColumn.UserName:
                            sb.Append("USERNAME;");
                            break;
                        case LogColumn.ClientIp:
                            sb.Append("CLIENTIP;");
                            break;
                        case LogColumn.X:
                            sb.Append("X;");
                            break;
                        case LogColumn.Y:
                            sb.Append("Y;");
                            break;
                        case LogColumn.Scale:
                            sb.Append("SCALE;");
                            break;
                        case LogColumn.Custom:
                            sb.Append(logColumns[logColumn] + ";");
                            break;
                    }
                }
            }
            sb.Append("M2;M3;M4;M5;M6;M7;M8;M9;M10;");
            return sb.ToString();
        }
        return "SESSIONID;MAPREQUESTID;DATE;TIME;MAPNAME;M2;M3;M4;M5;M6;M7;M8;M9;M10;";
    }

    #endregion
}