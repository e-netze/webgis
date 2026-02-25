using E.Standard.Extensions.Collections;
using E.Standard.Localization.Abstractions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Export.Extensions;

static internal class ApiEventResponseExtensions
{
    static public ApiEventResponse CalcPrintSericesDimension(this ApiEventResponse response, IBridge bridge, ApiToolEventArguments e)
    {
        string layoutId = e[MapSeriesPrint.MapSeriesPrintLayoutId];
        string layoutFormat = e[MapSeriesPrint.MapSeriesPrintFormatId];
        double scale = !String.IsNullOrEmpty(e[MapSeriesPrint.MapSeriesPrintScaleId]) ?
                            e.GetDouble(MapSeriesPrint.MapSeriesPrintScaleId) :
                            e.GetDouble(MapSeriesPrint.MapSeriesPrintInitScaleId);
        var scaleComboValues = new List<int>(e.TryGetArray<int>($"{MapSeriesPrint.MapSeriesPrintScaleId}.values") ?? new int[0]);

        PageSize pageSize = (PageSize)Enum.Parse(typeof(PageSize), layoutFormat.Split('.')[0], true);
        PageOrientation pageOrientation = PageOrientation.Landscape;
        if (layoutFormat.Contains("."))
        {
            pageOrientation = (PageOrientation)Enum.Parse(typeof(PageOrientation), layoutFormat.Split('.')[1], true);
        }
        else { pageOrientation = 0; }

        #region Formats (allowed formats)

        var allowedFormats = bridge.GetPrintFormats(layoutId);
        if (allowedFormats.Count() > 0)
        {
            var currentFormat = allowedFormats.Where(f => f.Size == pageSize && f.Orientation == pageOrientation).FirstOrDefault();
            if (currentFormat == null)
            {
                pageSize = allowedFormats.First().Size;
                pageOrientation = allowedFormats.First().Orientation;
            }
        }

        response.AddUISetter(new UISelectOptionsSetter(MapSeriesPrint.MapSeriesPrintFormatId, $"{pageSize}.{pageOrientation}",
                                                        allowedFormats.Select(f => UISelect.ToPrintFormatsOption(f))));

        #endregion

        #region Scales (allowed Scales)

        var allowAddScales = bridge.GetPrintLayoutScales(layoutId) == null;

        var scales = bridge.GetPrintLayoutScales(layoutId) ??
                     e.GetConfigArray<int>("scales") ??
                     e.GetArray<double>(MapSeriesPrint.MapSeriesPrintScalesId)?
                        .Select(s => Math.Round(s / 10.0, 0) * 10.0)
                        .ToArray()
                        .ConvertItems<int>();

        if (allowAddScales)
        {
            List<int> extendedScales = new List<int>();
            foreach (var extendScale in scaleComboValues)
            {
                if (!scales.Contains(extendScale) &&
                    extendScale < scales.Max() &&
                    extendScale > scales.Min())
                {
                    extendedScales.Add(extendScale);
                }
            }

            var scalesList = new List<int>(scales);
            scalesList.AddRange(extendedScales);
            scales = scalesList;
        }

        if (scales != null)
        {
            scales = scales.OrderByDescending(v => v);
            scale = scales.Closest(Convert.ToInt32(scale));

            response.AddUISetter(
                new UISelectOptionsSetter(MapSeriesPrint.MapSeriesPrintScaleId, scale.ToString(),
                                          scales.Select(s => new UISelect.Option()
                                                                         .WithValue(s.ToString())
                                                                         .WithLabel(string.Format("1:{0:0,0.}", s).Replace(" ", ".")))));
        }

        #endregion

        var properties = bridge.GetPrintLayoutProperties(layoutId, pageSize, pageOrientation, scale);

        var mapSRef = bridge.CreateSpatialReference((int)e.MapCrs);
        var mapEnvelope = new Envelope(e.MapBBox());

        float scaleFactor = 1.0f;
        if (mapSRef.IsWebMercator())
        {
            // no scale factor needed. But its important apply cos(phi) for preview polygon
            // size in the MapServicePrint.GetPrintSeriesGraphicElements() 
            // method.

            // recalc scale for web mercator ??
            //scaleFactor = /*1f /*/ (float)Math.Cos(mapEnvelope.CenterPoint.Y / 180.0 * Math.PI);
        }

        return response
                .AddSketchProperties(
                    elementWidth: properties.WorldSize.Width * scaleFactor,
                    elementHeight: properties.WorldSize.Height * scaleFactor);
    }

    static async public Task<ApiEventResponse> AddCreateMapSeriesFromFeaturesDialog(this ApiEventResponse response, MapSeriesPrint tool, IBridge bridge, ApiToolEventArguments e, ILocalizer<MapSeriesPrint> localizer)
    {
        var features = await e.GetFeatureCollectionForMapSeries(bridge);

        var printLayouts = bridge.GetPrintLayouts(
            e.GetToolOptions<string[]>()?
             .Where(l => l.StartsWith(MapSeriesPrint.PrintLayoutOptionPrefix))
             .Select(l => l.Substring(MapSeriesPrint.PrintLayoutOptionPrefix.Length)));
        var printFormats = bridge.GetPrintFormats();

        response
            .AddUIElements(
                new UIDiv()
                    .AsDialog()
                    .WithDialogTitle(localizer.Localize("create-series-from-features"))
                    .AddChildren(e.AddRequiredMapSeriesPrintCreateFromFeaturesHiddenElements())
                    .AddChildren(
                         new UILabel()
                            .WithLabel(localizer.Localize("create.method")),
                         new UISelect()
                            .WithId(MapSeriesPrint.ParameterSeriesType)
                            .AsPersistantToolParameter()
                            .AddPossibleSeriesTypeOptions(features, localizer),
                         new UILabel()
                            .WithLabel(localizer.Localize("create.overlapping-percent")),
                         new UISelect()
                            .WithId(MapSeriesPrint.ParameterOverlappingPercent)
                            .AsPersistantToolParameter()
                            .AddOptions(
                                Enumerable.Range(0, 50)
                                    .Select(r => new UISelect.Option() { value = r.ToString(), label = $"{r}%" })
                            ),
                         
                         // add info about features count
                         new UIValidationErrorSummary(e[MapSeriesPrint.CreateSeriesValidationErrors]),

                         // add layout /format/scale selectors
                         new UILabel()
                            .WithLabel(localizer.Localize("layout")),
                         new UISelect(UIButton.UIButtonType.servertoolcommand, "seleectionchanged")
                            .WithId($"{MapSeriesPrint.MapSeriesPrintLayoutId}-create")
                            .AsToolParameter(UICss.MapSeriesPrintToolLayout, UICss.ToolInitializationParameterImportant)
                            .AddOptions(printLayouts.Select(l => new UISelect.Option()
                                                                        .WithValue(l.Id)
                                                                        .WithLabel(l.Name))),
                        new UILabel()
                            .WithLabel(localizer.Localize("format")),
                        UISelect.PrintFormats(tool, $"{MapSeriesPrint.MapSeriesPrintFormatId}-create", printFormats, UIButton.UIButtonType.servertoolcommand, "selectionchanged", defaultValue: e.GetConfigValue(MapSeriesPrint.ConfigDefaultFormat)),
                        new UILabel()
                            .WithLabel(localizer.Localize("print-scale")),
                        UISelect.Scales($"{MapSeriesPrint.MapSeriesPrintScaleId}-create", UIButton.UIButtonType.servertoolcommand, "selectionchanged", allowAddValues: true, scales: e.GetConfigArray<int>("scales"))
                            .AsToolParameter(),
                        new UIButtonContainer()
                            .AddChildren(
                                new UIButton(UIButton.UIButtonType.servertoolcommand, "create-series-from-features-calc")
                                    .WithText(localizer.Localize("create.start")))
                    )
            )
            .AddUISetter(new UIApplyPersistentParametersSetter(tool))
            .AddUISetters(
                new UISetter($"{MapSeriesPrint.MapSeriesPrintLayoutId}-create", e[MapSeriesPrint.MapSeriesPrintLayoutId]),
                new UISetter($"{MapSeriesPrint.MapSeriesPrintFormatId}-create", e[MapSeriesPrint.MapSeriesPrintFormatId]),
                new UISetter($"{MapSeriesPrint.MapSeriesPrintScaleId}-create", e[MapSeriesPrint.MapSeriesPrintScaleId])
            );

        return response;
    }
}
