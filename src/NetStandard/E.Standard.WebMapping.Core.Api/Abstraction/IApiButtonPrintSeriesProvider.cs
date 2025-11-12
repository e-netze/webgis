#nullable enable

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiButtonPrintSeriesProvider
{
    void CheckPrintMapSeriesSupport(IMap map, Shape toolSketch, ApiToolEventArguments e);
    IEnumerable<PrintMapOrientation>? GetPrintMapOrientations(Shape toolSketch);
    IGraphicsContainer GetPrintSeriesGraphicElements(Shape toolSketch, double extentWidth, double extentHeight);

    PrintMapSeriesOverviewPageDefinition? GetPrintMapSeriesOverviewPageDefinition(IMap mapPrototype, ApiToolEventArguments e);
}

public record PrintMapSeriesOverviewPageDefinition(string LayoutFile, PageSize? PageSize, PageOrientation? pageOrientation, IMap Map, Envelope MapExtent);
public record PrintMapOrientation(string PageName, Point? MapCenter, Envelope? mapExtent, double MapRoation);