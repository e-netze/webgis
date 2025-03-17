using E.Standard.Cms.Abstraction;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace E.Standard.Cms.Services.Logging;

public abstract class CmsFileSystemLogger : ICmsLogger
{
    private static readonly object lockObject = new object();
    private readonly CmsLoggerOptions _options;

    public CmsFileSystemLogger(IOptions<CmsLoggerOptions> options)
    {
        _options = options.Value;
    }

    protected abstract string CalcLine(string username,
                                       string method,
                                       string command,
                                       params string[] values);

    public void Log(string username,
                    string method,
                    string command,
                    params string[] values)
    {
        string line = CalcLine(username, method, command, values);

        Task.Run(() =>
        {
            try
            {
                string? dirName = Path.GetDirectoryName(_options.ConnectionString);

                if (!String.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }

                CheckAndRotateLogFile();

                lock (lockObject)
                {
                    using (StreamWriter sw = File.AppendText(_options.ConnectionString))
                    {
                        sw.WriteLine(line);
                    }
                }
            }
            catch /*(Exception ex)*/
            {
                //Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        });
    }

    private void CheckAndRotateLogFile()
    {
        string logFilePath = _options.ConnectionString;

        if (File.Exists(logFilePath) && new FileInfo(logFilePath).Length > _options.MaxFileSizeBytes)
        {
            string newFileName = $"{Path.GetFileNameWithoutExtension(logFilePath)}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(logFilePath)}";
            string newFilePath = Path.Combine(Path.GetDirectoryName(logFilePath)!, newFileName);

            File.Move(logFilePath, newFilePath);
            File.Create(logFilePath).Dispose();
        }
    }
}
