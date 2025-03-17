using E.Standard.Extensions.Formatting;
using Microsoft.Extensions.Options;
using System;

namespace E.Standard.WebGIS.Core.Services;

public class ConsoleTracerService : ITracerService
{
    private readonly ConsoleTracerServiceOptions _options;

    public ConsoleTracerService(IOptionsMonitor<ConsoleTracerServiceOptions> optionsMonitor)
    {
        _options = optionsMonitor.CurrentValue;
    }

    #region ITracerService

    public bool Trace
    {
        get { return _options.Trace; }
    }

    public void Log(object source, string msg)
    {
        if (!Trace)
        {
            return;
        }

        try
        {
            Console.WriteLine($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}: {source.ClassName()} - {msg}");
        }
        catch { }
    }

    #endregion
}
