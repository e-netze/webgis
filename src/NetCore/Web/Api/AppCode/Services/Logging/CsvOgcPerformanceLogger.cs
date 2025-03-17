using E.Standard.Api.App.Configuration;
using E.Standard.WebMapping.Core.Logging;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using Microsoft.Extensions.Configuration;

namespace Api.Core.AppCode.Services.Logging;

public class CsvOgcPerformanceLogger : GenericGeoServicePerformanceLogger<CSVLogger>, IOgcPerformanceLogger
{
    static int BufferMaxMB = 5;
    static int Digits = 2;

    private readonly CSVLogger _loggerInstance;

    public CsvOgcPerformanceLogger(IConfiguration configuration)
        : base(new CSVLogger(
            new StreamBuffer($"{configuration[ApiConfigKeys.LogPath]}/webgis_ogc_performance.csv", configuration[ApiConfigKeys.LogPerformanceColumns], BufferMaxMB),
            null,
            configuration[ApiConfigKeys.LogPerformanceColumns],
            Digits)
        )
    {
        _loggerInstance = base._logger;
    }

    override public void Flush() => _loggerInstance?.FlushBuffer();
}
