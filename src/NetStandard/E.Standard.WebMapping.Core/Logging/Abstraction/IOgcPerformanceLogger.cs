using System.Runtime.CompilerServices;

using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

public interface IOgcPerformanceLogger : IGeoServicePerformanceLogger
{
    /// <summary>
    /// OGC requests (WMS/WFS/WMTS, ...) don't map to a fixed <see cref="GeoServiceCommand"/> -
    /// the command is the dynamic "service/request" combination taken from the incoming OGC
    /// request parameters (e.g. "wms/getmap"), so it stays a plain string here.
    /// </summary>
    ILog Start(string cmd, IMap map, string server, string service,
        [InterpolatedStringHandlerArgument("")] GeoServicePerformanceLogMessage message = default);
}
