using E.Standard.Api.App.Configuration;
using E.Standard.WebMapping.Core.Logging;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using Microsoft.Extensions.Configuration;

namespace Api.Core.AppCode.Services.Logging;

public class CsvDatalinqPerformanceLogger : GenericUsagePerformanceLogger<CSVLogger>, IDatalinqPerformanceLogger
{
    static int BufferMaxMB = 5;
    static int Digits = 2;

    private readonly CSVLogger _loggerInstance;


    public CsvDatalinqPerformanceLogger(IConfiguration configuration)
        : base(new CSVLogger(
                new StreamBuffer($"{configuration[ApiConfigKeys.LogPath]}/webgis_datalinq.csv", "Date;Time;User;Command;Message", BufferMaxMB),
                null,
                configuration[ApiConfigKeys.LogPerformanceColumns], Digits))
    {
        _loggerInstance = base._logger;
    }

    override public void Flush() => _loggerInstance.FlushBuffer();
}