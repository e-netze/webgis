namespace E.Standard.WebMapping.Core.Logging.Abstraction;

/// <summary>
/// Typed replacement for the former "cmd" magic strings ("Init", "GetMap", "GetSelection", ...)
/// used with <see cref="IGeoServicePerformanceLogger"/>. Member names are intentionally identical
/// to the former string values so that <c>cmd.ToString()</c> can be used at the boundary to
/// existing string based sinks (CSV, Microsoft.Extensions.Logging, ...) without a mapping table.
/// </summary>
public enum GeoServiceCommand
{
    Init,
    GetMap,
    GetSelection,
    GetLegend,

    /// <summary>
    /// A single map service rendering the (possibly partial) map image used while composing a
    /// print/layout - see <see cref="E.Standard.WebMapping.Core.Abstraction.IPrintableMapService.GetPrintImageAsync"/>.
    /// Not to be confused with <see cref="GetPrint"/>, the actual print/layout request.
    /// </summary>
    GetPrintImage,

    /// <summary>
    /// The actual print request (layout composition, e.g. in RestPrintHelperService), as opposed
    /// to <see cref="GetPrintImage"/> which is just the underlying map image of a single service.
    /// </summary>
    GetPrint
}
