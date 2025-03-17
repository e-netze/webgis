using System;
using System.IO;
using System.Text;

namespace E.Standard.WebMapping.Core.Logging;


public class StreamBuffer
{
    private object thisLock = new object();
    private static object writeLock = new object();

    private int _bufferLength = 1024 * 1024;
    private StringBuilder _buffer = new StringBuilder();
    private string _filename, _header = String.Empty;
    private int _maxMB;

    public StreamBuffer(string filename, string header, int maxMB)
    {
        _filename = filename;
        _header = header;
        _maxMB = maxMB;

        if (String.IsNullOrWhiteSpace(_header))
        {
            _header = "SESSIONID;REQUESTID;DATE;TIME;MAP";
        }

        _header += ";REQUEST;SERVER;SERVICE;MS;SUCCESS";
    }

    public void Append(string msg)
    {
        lock (thisLock)
        {
            _buffer.Append(msg);
            if (_buffer.Length < _bufferLength)
            {
                return;
            }

            FlushBuffer();
        }
    }

    public void Flush()
    {
        lock (thisLock)
        {
            FlushBuffer();
        }
    }

    private void FlushBuffer()
    {
        lock (writeLock)
        {
            try
            {
                #region Flush
                FileInfo fi = new FileInfo(_filename);
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }

                if (fi.Exists && fi.Length > _maxMB * 1024 * 1024)
                {
                    try
                    {
                        string archiveName = fi.FullName.Substring(0, fi.FullName.Length - fi.Extension.Length) + "_archive" +
                            DateTime.Now.Ticks + "_" +
                            DateTime.Now.ToShortDateString().Replace(":", "_").Replace(".", "_").Replace("/", "_").Replace("\\", "_") + ".csv";

                        fi.CopyTo(archiveName);
                        fi.Delete();
                        fi.Refresh();
                    }
                    catch { }
                }

                try
                {
                    bool writeHeader = !fi.Exists;

                    StreamWriter sw = new StreamWriter(_filename, true);
                    if (writeHeader)
                    {
                        sw.WriteLine(_header);
                    }

                    sw.Write(_buffer.ToString());
                    sw.Close();
                }
                catch { }
                _buffer = new StringBuilder();
                #endregion
            }
            catch { }
        }
    }
}
