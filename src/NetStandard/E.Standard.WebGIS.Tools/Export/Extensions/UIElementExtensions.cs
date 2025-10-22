using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Tools.Export.Calc;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebGIS.Tools.Export.Extensions;

static internal class UIElementExtensions
{
    static public UISelect AddPossibleSeriesTypeOptions(
        this UISelect select,
        FeatureCollection features,
        ILocalizer localizer)
    {
        var shapeProto = features.GeometryPrototype();

        select.AddOptions(
            new UISelect.Option()
                .WithValue(((int)SeriesType.IntersectionRaster).ToString())
                .WithLabel(localizer.Localize("Intersection Raster")),
            new UISelect.Option()
                .WithValue(((int)SeriesType.BoundingBoxRaster).ToString())
                .WithLabel(localizer.Localize("Bounding Box Raster")));

        if (shapeProto is Polyline)
        {
            select.AddOption(
                new UISelect.Option()
                    .WithValue(((int)SeriesType.AlongPolylines).ToString())
                    .WithLabel(localizer.Localize("Along Polylines")));
        }

        return select;
    }
}
