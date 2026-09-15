using System.Runtime.CompilerServices;

using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

/// <summary>
/// Typed, discoverable entry points per <see cref="GeoServiceCommand"/>, replacing the former
/// <c>Start(map, server, service, "GetMap", message)</c> call sites with magic string "cmd"
/// parameters.
/// </summary>
public static class GeoServicePerformanceLoggerExtensions
{
    public static ILog StartInit(this IGeoServicePerformanceLogger logger, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("logger")] GeoServicePerformanceLogMessage message = default)
        => logger.Start(GeoServiceCommand.Init, map, server, service, message);

    public static ILog StartGetMap(this IGeoServicePerformanceLogger logger, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("logger")] GeoServicePerformanceLogMessage message = default)
        => logger.Start(GeoServiceCommand.GetMap, map, server, service, message);

    public static ILog StartGetSelection(this IGeoServicePerformanceLogger logger, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("logger")] GeoServicePerformanceLogMessage message = default)
        => logger.Start(GeoServiceCommand.GetSelection, map, server, service, message);

    public static ILog StartGetLegend(this IGeoServicePerformanceLogger logger, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("logger")] GeoServicePerformanceLogMessage message = default)
        => logger.Start(GeoServiceCommand.GetLegend, map, server, service, message);

    public static ILog StartGetPrintImage(this IGeoServicePerformanceLogger logger, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("logger")] GeoServicePerformanceLogMessage message = default)
        => logger.Start(GeoServiceCommand.GetPrintImage, map, server, service, message);

    public static ILog StartGetPrint(this IGeoServicePerformanceLogger logger, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("logger")] GeoServicePerformanceLogMessage message = default)
        => logger.Start(GeoServiceCommand.GetPrint, map, server, service, message);
}
