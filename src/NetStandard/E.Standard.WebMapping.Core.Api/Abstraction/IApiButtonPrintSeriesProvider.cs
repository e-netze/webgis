#nullable enable

using E.Standard.WebMapping.Core.Geometry;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiButtonPrintSeriesProvider
{
    IEnumerable<PrintMapOrientation>? GetPrintMapOrientations(Shape toolSketch);
}

public record PrintMapOrientation(Point MapCenter, double MapRoation);