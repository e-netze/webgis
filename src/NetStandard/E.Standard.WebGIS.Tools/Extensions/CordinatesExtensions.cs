using E.Standard.Platform;
using E.Standard.WebGIS.Tools.Helpers;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Extensions;

internal static class CordinatesExtensions
{
    static public IEnumerable<Point> GetWGSFormCoordinatesTable(this ApiToolEventArguments e, IBridge bridge)
    {
        var tableData = e[Coordinates.CoordinatesTableId];
        char separator = ';';
        var data = tableData.Split(separator);

        var heightColumns = new RasterQueryHelper().HeightNameNodes(System.IO.Path.Combine(bridge.AppEtcPath, "coordinates", "h.xml"));
        var colCount = 3 + heightColumns.Count();

        for (int i = 0; i < data.Length; i += colCount)
        {
            if (data.Length > i + 2)
            {
                yield return new Point(data[i + 1].ToPlatformDouble(), data[i + 2].ToPlatformDouble());
            }
        }
    }
}
