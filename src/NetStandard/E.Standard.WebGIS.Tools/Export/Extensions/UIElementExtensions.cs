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
        select.AddOptions(
            SeriesType.IntersectionRaster.AsUISelectOption(localizer),
            SeriesType.BoundingBoxRaster.AsUISelectOption(localizer),
            SeriesType.OnePerFeature.AsUISelectOption(localizer)
            );

        select.AddOptions(features.GeometryPrototype() switch
        {
            Polyline => [SeriesType.AlongPolylines.AsUISelectOption(localizer)],
            _ => []
        });

        return select;
    }
}
