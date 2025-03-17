using E.Standard.Api.App.Configuration;
using E.Standard.WebMapping.Core.Logging;
using Microsoft.Extensions.Configuration;

namespace Api.Core.AppCode.Services.Logging;

public class CsvUsagePerformaceLogger : GenericUsagePerformanceLogger<CSVLogger>
{
    static int BufferMaxMB = 5;
    static int Digits = 2;

    private readonly CSVLogger _loggerInstance;


    public CsvUsagePerformaceLogger(IConfiguration configuration)
        : base(new CSVLogger(
                new StreamBuffer($"{configuration[ApiConfigKeys.LogPath]}/webgis_usage.csv", "Date;Time;User;Command;Message", BufferMaxMB),
                null,
                configuration[ApiConfigKeys.LogPerformanceColumns], Digits))
    {
        _loggerInstance = base._logger;
    }

    override public void Flush() => _loggerInstance.FlushBuffer();
}
