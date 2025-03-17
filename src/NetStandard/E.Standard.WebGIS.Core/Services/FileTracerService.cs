using E.Standard.Extensions.Compare;
using E.Standard.Extensions.Formatting;
using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.Options;
using System;
using System.IO;

namespace E.Standard.WebGIS.Core.Services;

public class FileTracerService : ITracerService
{
    private readonly string _outputPath;
    private readonly string _filename;

    public FileTracerService(ISecurityConfigurationService config,
                             IOptionsMonitor<FileTracerServiceOptions> optionsMonitor)
    {
        var options = optionsMonitor.CurrentValue;

        _outputPath = config[options.OutputPathConfigKey];
        _filename = config[options.FileNameConfigKey].OrTake("trace.log");

        this.Trace = !String.IsNullOrEmpty(_outputPath) && config[options.TraceConfigKey] == "true";
    }

    #region ITracerService

    public bool Trace { get; }

    public void Log(object source, string msg)
    {
        if (!Trace)
        {
            return;
        }

        try
        {
            string filePath = $"{_outputPath}/{_filename}";
            try
            {
                FileInfo fi = new FileInfo(filePath);
                if (fi.Exists && fi.Length > 1024 * 1024)
                {
                    fi.Delete();
                }
            }
            catch
            {
            }
            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}: {source.ClassName()} - {msg}");
                sw.Close();
            }
        }
        catch { }
    }

    #endregion
}
