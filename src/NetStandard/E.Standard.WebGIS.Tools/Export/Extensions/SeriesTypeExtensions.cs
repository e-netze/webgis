using System.Collections.Generic;

using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Tools.Export.Calc;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebGIS.Tools.Export.Extensions;

internal static class SeriesTypeExtensions
{
    static public string LocalizationString(this SeriesType seriesType)
        => seriesType switch
        {
            SeriesType.BoundingBoxRaster => "create.method.bbox-grid",
            SeriesType.IntersectionRaster => "create.method.intersection-grid",
            SeriesType.AlongPolylines => "create.method.along-polyline",
            SeriesType.OnePerFeature => "create.method.one-per-feature",
            _ => ""
        };

    static public string LocalizationBodyString(this SeriesType seriesType)
        => $"{seriesType.LocalizationString()}:body";

    static public UISelect.Option AsUISelectOption(this SeriesType seriesType, ILocalizer localizer)
        => new UISelect.Option()
                .WithValue(((int)seriesType).ToString())
                .WithLabel(localizer.Localize(seriesType.LocalizationString()));

    static public UIParagraph AsUIParagraphWithDescription(this SeriesType seriesType, ILocalizer localizer)
        => new UIParagraph(localizer.Localize(seriesType.LocalizationBodyString()))
        {
            style = "font-size: 1.4em"
        };

    static public UIImage AsPreviewImage(this SeriesType seriesType, IBridge bridge)
        => new UIImage(
            seriesType switch
            {
                SeriesType.BoundingBoxRaster => $"{bridge.AppRootUrl}/content/api/img/map-series-print/bounding-box-raster.png",
                SeriesType.IntersectionRaster => $"{bridge.AppRootUrl}/content/api/img/map-series-print/intersection-raster.png",
                SeriesType.AlongPolylines => $"{bridge.AppRootUrl}/content/api/img/map-series-print/along-polylines.png",
                SeriesType.OnePerFeature => $"{bridge.AppRootUrl}/content/api/img/map-series-print/one-per-feature.png",
                _ => ""
            })
        {
            style = "height:140px;border-radius:7px;"
        };
}
