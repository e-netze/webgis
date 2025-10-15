using E.Standard.Extensions.Collections;
using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Reflection;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static E.Standard.WebMapping.Core.Api.UI.Elements.UICollapsableElement;

namespace E.Standard.WebGIS.Tools.Export;

[Export(typeof(IApiButton))]
[ToolHelp("tools/map/print.html")]
[ToolConfigurationSection("print")]
[ToolId("webgis.tools.mapseriesprint")]
[AdvancedToolProperties(
        MapBBoxDependent = true,
        MapCrsDependent = true,
        PrintLayoutRotationDependent = true,
        ScaleDependent = true,
        QueryMarkersVisibliityDependent = true,
        CoordinateMarkersVisibilityDependent = true,
        ChainageMarkersVisibilityDependent = true
    )]
internal class MapSeriesPrint : IApiServerToolLocalizable<MapSeriesPrint>,
                                IApiButtonResources,
                                IApiButtonPrintSeriesProvider
{
    public const string ConfigQualitiesDpi = "qualities-dpi";
    public const string ConfigDefaultQuality = "default-quality";
    public const string ConfigDefaultFormat = "default-format";

    public const string PrintLayoutOptionPrefix = "series-layout:";

    public const string MapSeriesPrintScalesId = "mapseriesprint-map-scales";
    public const string MapSeriesPrintLayoutId = "mapseriesprint-layout-select";
    public const string MapSeriesPrintFormatId = "mapsseriesprint-format-select";
    public const string MapSeriesPrintScaleId = "mapseriesprint-scale-select";
    public const string MapSeriesPrintInitScaleId = "mapseriesprint-init-scale";
    public const string MapSeriesPrintQualityId = "mapseriesprint-qualitity-select";

    #region IApiServerTool

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<MapSeriesPrint> localizer)
    {
        var printLayouts = bridge.GetPrintLayouts(
            e.GetToolOptions<string[]>()?
             .Where(l => l.StartsWith(PrintLayoutOptionPrefix))
             .Select(l => l.Substring(PrintLayoutOptionPrefix.Length)));
        var printFormats = bridge.GetPrintFormats();

        var qualitites = e.GetConfigDictionay<int, string>(ConfigQualitiesDpi);
        var markersSelector = new UIPrintMarkerSelector("print-marker-selector");

        return new ApiEventResponse()
            .AddUIElements(
                new UIHidden()
                    .WithId(MapSeriesPrintScalesId)
                    .AsToolParameter(UICss.AutoSetterMapScales),
                new UIOptionContainer() 
                    { 
                        title = localizer.Localize("layout-quality"),
                        CollapseState = CollapseStatus.Expanded
                    }
                    .AddChildren(
                        new UILabel()
                            .WithLabel(localizer.Localize("layout")),
                        new UISelect(UIButton.UIButtonType.servertoolcommand, "selectionchanged")
                            .WithId(MapSeriesPrintLayoutId)
                            .AsPersistantToolParameter(UICss.PrintToolLayout, UICss.ToolInitializationParameterImportant)
                            .AddOptions(printLayouts.Select(l => new UISelect.Option()
                                                                        .WithValue(l.Id)
                                                                        .WithLabel(l.Name))),
                        new UILabel()
                            .WithLabel(localizer.Localize("format")),
                        UISelect.PrintFormats(MapSeriesPrintFormatId, printFormats, UIButton.UIButtonType.servertoolcommand, "selectionchanged", defaultValue: e.GetConfigValue(ConfigDefaultFormat)),
                        new UILabel()
                            .WithLabel(localizer.Localize("print-scale")),
                        UISelect.Scales(MapSeriesPrintScaleId, UIButton.UIButtonType.servertoolcommand, "selectionchanged", allowAddValues: true, scales: e.GetConfigArray<int>("scales")),
                        new UILabel()
                            .WithLabel(localizer.Localize("print-quality")),
                        UISelect.PrintQuality(MapSeriesPrintQualityId, qualitites)
                    ),
                //new UIDiv()
                //    .WithVisibilityDependency(VisibilityDependency.ToolSketchesExists)
                //    .AddChild(new UIPrintSketchSelector("print-sketch-selector")),
                new UIDiv()
                    .WithVisibilityDependency(VisibilityDependency.HasToolResults_Coordinates_or_Chainage_or_QueryResults)
                    .AddChild(markersSelector),
                new UIDiv()
                    .WithVisibilityDependency(VisibilityDependency.HasToolResults_Coordinates_or_QueryResults)
                    .AddChild(new UIPrintAttachementsSelector("print-attachment-selector")),
                new UIButtonContainer()
                    .AddChildren(
                        new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.showprinttasks)
                            .WithStyles(UICss.CancelButtonStyle)
                            .WithText(localizer.Localize("print-jobs")),
                        new UIButton(UIButton.UIButtonType.servertoolcommand, "print")
                            .WithText(localizer.Localize("name")))
            )
            .AddUISetter(new UIPersistentParametersSetter(this))
            .AddUISetters(new IUISetter[] {
                new UISetter(markersSelector.QueryMarkersVisibilitySelectorId, e.QueryMarkersVisible() ? "show" : ""),
                new UISetter(markersSelector.CoodianteMakersVisiblitySelectorId, e.CoordinateMarkersVisible() ? "show" : ""),
                new UISetter(markersSelector.ChainageMarkersVisiblitySelectorId, e.ChainageMarkersVisible() ? "show" : "")
            });
    }

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<MapSeriesPrint> localizer) => null;

    public ToolType Type => ToolType.sketchseries;

    public ToolCursor Cursor => ToolCursor.Crosshair;

    #endregion

    #region IApiButton Member

    public string Name => "Map Series";

    public string Container => "Map";

    public string Image => UIImageButton.ToolResourceImage(this, "print");

    public string ToolTip => "Print a map series in PDF format.";

    public bool HasUI => true;

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("print", Properties.Resources.print);
    }

    #endregion

    #region Commands

    [ServerToolCommand("init")]
    public ApiEventResponse OnInit(IBridge bridge, ApiToolEventArguments e)
    {
        if (e.MapScale.HasValue)
        {
            e[MapSeriesPrintInitScaleId] = (e.MapScale.Value / 4.0).ToString();
        }
        return OnSelectionChanged(bridge, e);
    }


    [ServerToolCommand("selectionchanged")]
    public ApiEventResponse OnSelectionChanged(IBridge bridge, ApiToolEventArguments e)
    {
        string layoutId = e[MapSeriesPrintLayoutId];
        string layoutFormat = e[MapSeriesPrintFormatId];
        double scale = !String.IsNullOrEmpty(e[MapSeriesPrintInitScaleId]) ?
                            e.GetDouble(MapSeriesPrintInitScaleId) :
                            e.GetDouble(MapSeriesPrintScaleId);
        var scaleComboValues = new List<int>(e.TryGetArray<int>($"{MapSeriesPrintScaleId}.values") ?? new int[0]);

        PageSize pageSize = (PageSize)Enum.Parse(typeof(PageSize), layoutFormat.Split('.')[0], true);
        PageOrientation pageOrientation = PageOrientation.Landscape;
        if (layoutFormat.Contains("."))
        {
            pageOrientation = (PageOrientation)Enum.Parse(typeof(PageOrientation), layoutFormat.Split('.')[1], true);
        }
        else { pageOrientation = 0; }

        var response = new ApiEventResponse();

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

        response.AddUISetter(new UISelectOptionsSetter(MapSeriesPrintFormatId, $"{pageSize}.{pageOrientation}",
                                                        allowedFormats.Select(f => UISelect.ToPrintFormatsOption(f))));

        #endregion

        #region Scales (allowed Scales)

        var allowAddScales = bridge.GetPrintLayoutScales(layoutId) == null;

        var scales = bridge.GetPrintLayoutScales(layoutId) ??
                     e.GetConfigArray<int>("scales") ??
                     e.GetArray<double>("print-map-scales")?
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
                new UISelectOptionsSetter(MapSeriesPrintScaleId, scale.ToString(),
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
            // recalc scale for web mercator ??
            scaleFactor = 1f / (float)Math.Cos(mapEnvelope.CenterPoint.Y / 180.0 * Math.PI);
        }

        return response
                .AddSketchProperties(
                    elementWidth: properties.WorldSize.Width * scaleFactor, 
                    elementHeight: properties.WorldSize.Height * scaleFactor);
    }

    [ServerToolCommand("print")]
    async public Task<ApiEventResponse> OnPrint(IBridge bridge, ApiToolEventArguments e, ILocalizer<Print> localizer)
    {
        string layoutId = e[MapSeriesPrintLayoutId];
        string layoutFormat = e[MapSeriesPrintFormatId];
        double scale = e.GetDouble(MapSeriesPrintScaleId);
        int dpi = e.GetInt(MapSeriesPrintQualityId);

        List<IUIElement> textElements = new List<IUIElement>();
        foreach (var layoutText in bridge.GetPrintLayoutTextElements(layoutId))
        {
            textElements.Add(
                new UILabel()
                    .WithLabel(layoutText.AliasName));
            textElements.Add(
                new UIInputText()
                    .WithId(layoutText.Name)
                    .WithValue(layoutText.Default)
                    .WithStyles(UICss.PrintTextElement));
        }

        PageSize pageSize = (PageSize)Enum.Parse(typeof(PageSize), layoutFormat.Split('.')[0], true);
        PageOrientation pageOrientation = PageOrientation.Landscape;

        if (layoutFormat.Contains("."))
        {
            pageOrientation = (PageOrientation)Enum.Parse(typeof(PageOrientation), layoutFormat.Split('.')[1], true);
        }
        else { pageOrientation = 0; }

        var properties = bridge.GetPrintLayoutProperties(layoutId, pageSize, pageOrientation, scale);

        #region Add Features in current Extent

        if (properties.IsQueryDependent)
        {
            try
            {
                var query = await bridge.GetQuery(properties.QueryDependencyQueryUrl.Split(':')[0], properties.QueryDependencyQueryUrl.Split(':')[1]);

                var mapSRef = bridge.CreateSpatialReference((int)e.MapCrs);
                var mapEnvelope = new Envelope(e.MapBBox());
                var layoutRotation = e.GetDouble("_printlayout_rotation");

                using (var transformer = new GeometricTransformerPro(bridge.CreateSpatialReference(4326), mapSRef))
                {
                    transformer.Transform(mapEnvelope);
                }

                var printEnvelope = new Envelope(mapEnvelope.CenterPoint, properties.WorldSize.Width, properties.WorldSize.Height).ToPolygon();

                SpatialAlgorithms.Rotate(printEnvelope, printEnvelope.ShapeEnvelope.CenterPoint, layoutRotation);

                ApiSpatialFilter filter = new ApiSpatialFilter()
                {
                    FeatureSpatialReference = mapSRef,
                    Fields = QueryFields.All,
                    FilterSpatialReference = mapSRef,
                    QueryShape = printEnvelope,
                    QueryGeometry = true
                };

                var features = await query.PerformAsync(bridge.RequestContext, filter);
                var fields = query.GetSimpleTableFields().Take(3);

                var uiSelect = new UIOptionList()
                        .WithStyles(UICss.PrintHeaderIdElement)
                        .AddChildren(features
                                        .Where(f => printEnvelope.ShapeEnvelope.Intersects(f.Shape.ShapeEnvelope))
                                        .OrderBy(f => f.Shape.ShapeEnvelope.CenterPoint.Distance2D(printEnvelope.ShapeEnvelope.CenterPoint))
                                        .Select(f => new UIOptionList.Option()
                                                .WithValue(f[properties.QueryDependencyQueryField])
                                                .AddChildren(fields.Select(field =>
                                                    new UIDiv()
                                                        .AddChildren(
                                                            new UILiteralBold().WithLiteral($"{field.Value}: "),
                                                            new UILiteral().WithLiteral(f[field.Key])
                                                        )))
                                        ));

                textElements.Add(new UILabel().WithLabel(query.Name));
                textElements.Add(uiSelect);
            }
            catch /*(Exception ex)*/
            {
                //throw new Exception($"Das gewählte Layout ist von ausgewählten Geo-Objeten abhängig: { ex.Message }");
            }
        }

        #endregion

        return new ApiEventResponse()
            .AddMapViewLense(Lense.Current)
            .AddUIElements(
                new UIDiv()
                    .AsDialog()
                    .WithDialogTitle(localizer.Localize("name"))
                    .WithStyles(UICss.NarrowFormMarginAuto)
                    .AddChildren(
                        new UIHidden()
                            .WithStyles(UICss.PrintToolLayout)
                            .WithValue(layoutId),
                        new UIHidden()
                            .WithStyles(UICss.PrintToolFormat)
                            .WithValue(layoutFormat),
                        new UIHidden()
                            .WithStyles(UICss.PrintToolQuality)
                            .WithValue(dpi),
                        new UIHidden()
                            .WithStyles(UICss.MapScalesSelect)
                            .WithValue(scale),

                        new UIHidden()
                            .WithStyles(UICss.PrintToolSketch)
                            .WithValue(UIPrintSketchSelector.GetValue(e, "print-sketch-selector", UIPrintSketchSelector.PrintTookSketch)),
                        new UIHidden()
                            .WithStyles(UICss.PrintToolSketchLables)
                            .WithValue(UIPrintSketchSelector.GetValue(e, "print-sketch-selector", UIPrintSketchSelector.PrintToolSketchLables)),
                        new UIHidden()
                            .WithStyles(UICss.PrintShowQueryMarkers)
                            .WithValue(UIPrintMarkerSelector.GetValue(e, "print-marker-selector", UIPrintMarkerSelector.ShowQueryMarkersId)),
                        new UIHidden()
                            .WithStyles(UICss.PrintQueryMarkerLabelField)
                            .WithValue(UIPrintMarkerSelector.GetValue(e, "print-marker-selector", UIPrintMarkerSelector.QueryLabelFieldId)),
                        new UIHidden()
                            .WithStyles(UICss.PrintShowCoordinatesMarkers)
                            .WithValue(UIPrintMarkerSelector.GetValue(e, "print-marker-selector", UIPrintMarkerSelector.ShowCoordinatesMarkersId)),
                        new UIHidden()
                            .WithStyles(UICss.PrintCoordinatesMarkerLabelField)
                            .WithValue(UIPrintMarkerSelector.GetValue(e, "print-marker-selector", UIPrintMarkerSelector.CoordinatesLabelFieldId)),
                        new UIHidden()
                            .WithStyles(UICss.PrintShowChainageMarkers)
                            .WithValue(UIPrintMarkerSelector.GetValue(e, "print-marker-selector", UIPrintMarkerSelector.ShowChainageMarkersId)),
                        new UIHidden()
                            .WithStyles(UICss.PrintAttachQueryResults)
                            .WithValue(UIPrintAttachementsSelector.GetValue(e, "print-attachment-selector", UIPrintAttachementsSelector.AttachQueryResultsId)),
                        new UIHidden()
                            .WithStyles(UICss.PrintAttachCoordinates)
                            .WithValue(UIPrintAttachementsSelector.GetValue(e, "print-attachment-selector", UIPrintAttachementsSelector.AttachCoordinatesId)),
                        new UIHidden()
                            .WithStyles(UICss.PrintAttachCoordinatesFieldId)
                            .WithValue(UIPrintAttachementsSelector.GetValue(e, "print-attachment-selector", UIPrintAttachementsSelector.CoordinatesFieldId)),

                        new UIDiv()
                            .AddChildren(textElements.ToArray()),

                        new UIButtonContainer(
                            new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.print)
                                .WithText(localizer.Localize("start-print-job")))
                    )
                );
    }

    #endregion

    #region IApiButtonPrintSeriesProvider

    public IEnumerable<PrintMapOrientation> GetPrintMapOrientations(Shape toolSketch)
    {
        var points = toolSketch as MultiPoint;
        if(points is null)
        {
            throw new ArgumentException("Tool sketch is null or not an valid Multipoint") ;
        }

        return points
                .ToArray()
                .Select(p => new PrintMapOrientation(p, p switch
                    {
                        PointM m => -Convert.ToDouble(m.M),
                        _ => 0D
                    })
                );
    }

    public IGraphicsContainer GetPrintSeriesGraphicElements(Shape toolSketch, double extentWidth, double extentHeight)
    {
        var graphicElements = new GraphicsContainer();
        var points = toolSketch as MultiPoint;

        if (toolSketch is null || points?.ToArray().Any() != true)
        {
            return graphicElements;
        }

        int index = 1;
        foreach (var point in points.ToArray())         
        {
            graphicElements.Add(new MapFrameElement(
                name: $"A{index++}",
                center: point,
                width: extentWidth,
                height: extentHeight,
                rotation: point switch
                {
                    PointM m => -Convert.ToDouble(m.M),
                    _ => 0D
                },
                borderColor: gView.GraphicsEngine.ArgbColor.Black));
        }

        // Add logic to populate graphicElements based on toolSketch, extentWidth, and extentHeight

        return graphicElements;
    }

    #endregion
}
