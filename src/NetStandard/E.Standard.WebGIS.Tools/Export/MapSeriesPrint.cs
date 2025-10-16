using E.Standard.Extensions.Collections;
using E.Standard.GeoJson;
using E.Standard.Json;
using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.MapMarkup.Export;
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
using E.Standard.WebMapping.GeoServices.Tiling;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using static E.Standard.WebMapping.Core.Api.UI.Elements.UICollapsableElement;

namespace E.Standard.WebGIS.Tools.Export;

[Export(typeof(IApiButton))]
[ToolHelp("tools/map/print.html")]
[ToolConfigurationSection("print")]
[ToolId("webgis.tools.mapseriesprint")]
[ToolStorageId("WebGIS.Tools.Serialization/{user}/_mapseries")]
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

    protected string toolContainerId = "webgis-mapseriesprint-tool-container";

    #region IApiServerTool

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<MapSeriesPrint> localizer)
    {
        var printLayouts = bridge.GetPrintLayouts(
            e.GetToolOptions<string[]>()?
             .Where(l => l.StartsWith(PrintLayoutOptionPrefix))
             .Select(l => l.Substring(PrintLayoutOptionPrefix.Length)));
        var printFormats = bridge.GetPrintFormats();

        var qualitites = e.GetConfigDictionay<int, string>(ConfigQualitiesDpi);
        var markersSelector = new UIPrintMarkerSelector(this, "mapseriesprint-marker-selector");

        #region Tool Buttons

        List<IUIElement> uiImageButtons = new List<IUIElement>();
        if (bridge.CurrentUser?.IsAnonymous == false)
        {
            uiImageButtons.AddRange(new IUIElement[]{
                        new UIImageButton(this.GetType(),"save", UIButton.UIButtonType.servertoolcommand, "save"){
                            value = "save",
                            text = localizer.Localize("tools.save"),
                            css = UICss.Narrow
                        },
                        new UIImageButton(this.GetType(),"open", UIButton.UIButtonType.servertoolcommand, "open"){
                            value = "open",
                            text = localizer.Localize("tools.open"),
                            css = UICss.Narrow
                        }
            });
        }

        uiImageButtons.AddRange(new IUIElement[]{
                        new UIImageButton(this.GetType(),"upload", UIButton.UIButtonType.servertoolcommand, "upload"){
                            value = "upload",
                            text = localizer.Localize("tools.upload"),
                            css = UICss.Narrow
                        },
                        new UIImageButton(this.GetType(),"download", UIButton.UIButtonType.servertoolcommand, "download"){
                            value = "download",
                            text = localizer.Localize("tools.download"),
                            css = UICss.Narrow
                        },
                        new UIImageButton(this.GetType(), "delete", UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removesketch){
                            text = localizer.Localize("tools.remove-sketch"),
                            css = UICss.Narrow
                        }
            });

        #endregion

        return new ApiEventResponse()
            .AddUIElements(
                new UIHidden()
                    .WithId(MapSeriesPrintScalesId)
                    .AsToolParameter(UICss.AutoSetterMapScales),
                new UIDiv()
                    .WithId(toolContainerId)    
                    .WithStyles(UICss.ImageButtonWithLabelsContaier)
                    .AddChildren(uiImageButtons.ToArray()),
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
                            .AsPersistantToolParameter(UICss.MapSeriesPrintToolLayout, UICss.ToolInitializationParameterImportant)
                            .AddOptions(printLayouts.Select(l => new UISelect.Option()
                                                                        .WithValue(l.Id)
                                                                        .WithLabel(l.Name))),
                        new UILabel()
                            .WithLabel(localizer.Localize("format")),
                        UISelect.PrintFormats(this, MapSeriesPrintFormatId, printFormats, UIButton.UIButtonType.servertoolcommand, "selectionchanged", defaultValue: e.GetConfigValue(ConfigDefaultFormat)),
                        new UILabel()
                            .WithLabel(localizer.Localize("print-scale")),
                        UISelect.Scales(MapSeriesPrintScaleId, UIButton.UIButtonType.servertoolcommand, "selectionchanged", allowAddValues: true, scales: e.GetConfigArray<int>("scales"))
                            .AsPersistantToolParameter(),
                        new UILabel()
                            .WithLabel(localizer.Localize("print-quality")),
                        UISelect.PrintQuality(this, MapSeriesPrintQualityId, qualitites)
                    ),
                //new UIDiv()
                //    .WithVisibilityDependency(VisibilityDependency.ToolSketchesExists)
                //    .AddChild(new UIPrintSketchSelector("print-sketch-selector")),
                new UIDiv()
                    .WithVisibilityDependency(VisibilityDependency.HasToolResults_Coordinates_or_Chainage_or_QueryResults)
                    .AddChild(markersSelector),
                new UIDiv()
                    .WithVisibilityDependency(VisibilityDependency.HasToolResults_Coordinates_or_QueryResults)
                    .AddChild(new UIPrintAttachementsSelector("mapseriesprint-attachment-selector")),
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
        toolResourceManager.AddImageResource("open", Properties.Resources.open);
        toolResourceManager.AddImageResource("save", Properties.Resources.save);
        toolResourceManager.AddImageResource("download", Properties.Resources.download);
        toolResourceManager.AddImageResource("upload", Properties.Resources.upload);
        toolResourceManager.AddImageResource("delete", Properties.Resources.trashcan);
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
        double scale = !String.IsNullOrEmpty(e[MapSeriesPrintScaleId]) ?
                            e.GetDouble(MapSeriesPrintScaleId) :
                            e.GetDouble(MapSeriesPrintInitScaleId);
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
                     e.GetArray<double>(MapSeriesPrintScalesId)?
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

        List<IPrintLayoutTextBridge> layoutTexts = new();
        bool first = true;
        foreach (var lId in new string[] { "layout_map_services_overview.xml", layoutId})
        {
            bridge.GetPrintLayoutTextElements(lId, allowFileName: first == true)  // first (overview) layout allow direct access by file name
                .ToList()
                .ForEach(t =>
                {
                    if (layoutTexts.Any(x => x.Name == t.Name) == false)
                    {
                        layoutTexts.Add(t);
                    }
                });
            first = false;
        }
        
        List<IUIElement> textElements = new List<IUIElement>();
        foreach (var layoutText in layoutTexts)
        {
            textElements.Add(
                new UILabel()
                    .WithLabel(layoutText.AliasName));
            textElements.Add(
                new UIInputText()
                    .WithId(layoutText.Name)
                    .WithValue(layoutText.Default)
                    .WithStyles(UICss.MapSeriesPrintTextElement));
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
                        .WithStyles(UICss.MapSeriesPrintHeaderIdElement)
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
                            .WithStyles(UICss.MapSeriesPrintToolLayout)
                            .WithValue(layoutId),
                        new UIHidden()
                            .WithStyles(UICss.MapSeriesPrintToolFormat)
                            .WithValue(layoutFormat),
                        new UIHidden()
                            .WithStyles(UICss.MapSeriesPrintToolQuality)
                            .WithValue(dpi),
                        new UIHidden()
                            .WithStyles(UICss.MapScalesSelect)
                            .WithValue(scale),

                        new UIHidden()
                            .WithStyles(UICss.MapSeriesPrintToolSketch)
                            .WithValue(UIPrintSketchSelector.GetValue(e, "mapseriesprint-sketch-selector", UIPrintSketchSelector.PrintTookSketch)),
                        new UIHidden()
                            .WithStyles(UICss.MapSeriesPrintToolSketchLables)
                            .WithValue(UIPrintSketchSelector.GetValue(e, "mapseriesprint-sketch-selector", UIPrintSketchSelector.PrintToolSketchLables)),
                        new UIHidden()
                            .WithStyles(UICss.MapSeriesPrintShowQueryMarkers)
                            .WithValue(UIPrintMarkerSelector.GetValue(e, "mapseriesprint-marker-selector", UIPrintMarkerSelector.ShowQueryMarkersId)),
                        new UIHidden()
                            .WithStyles(UICss.MapSeriesPrintQueryMarkerLabelField)
                            .WithValue(UIPrintMarkerSelector.GetValue(e, "mapseriesprint-marker-selector", UIPrintMarkerSelector.QueryLabelFieldId)),
                        new UIHidden()
                            .WithStyles(UICss.MapSeriesPrintShowCoordinatesMarkers)
                            .WithValue(UIPrintMarkerSelector.GetValue(e, "mapseriesprint-marker-selector", UIPrintMarkerSelector.ShowCoordinatesMarkersId)),
                        new UIHidden()
                            .WithStyles(UICss.MapSeriesPrintCoordinatesMarkerLabelField)
                            .WithValue(UIPrintMarkerSelector.GetValue(e, "mapseriesprint-marker-selector", UIPrintMarkerSelector.CoordinatesLabelFieldId)),
                        new UIHidden()
                            .WithStyles(UICss.MapSeriesPrintShowChainageMarkers)
                            .WithValue(UIPrintMarkerSelector.GetValue(e, "mapseriesprint-marker-selector", UIPrintMarkerSelector.ShowChainageMarkersId)),
                        new UIHidden()
                            .WithStyles(UICss.MapSeriesPrintAttachQueryResults)
                            .WithValue(UIPrintAttachementsSelector.GetValue(e, "mapseriesprint-attachment-selector", UIPrintAttachementsSelector.AttachQueryResultsId)),
                        new UIHidden()
                            .WithStyles(UICss.MapSeriesPrintAttachCoordinates)
                            .WithValue(UIPrintAttachementsSelector.GetValue(e, "mapseriesprint-attachment-selector", UIPrintAttachementsSelector.AttachCoordinatesId)),
                        new UIHidden()
                            .WithStyles(UICss.MapSeriesPrintAttachCoordinatesFieldId)
                            .WithValue(UIPrintAttachementsSelector.GetValue(e, "mapseriesprint-attachment-selector", UIPrintAttachementsSelector.CoordinatesFieldId)),

                        new UIDiv()
                            .AddChildren(textElements.ToArray()),

                        new UIButtonContainer(
                            new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.print)
                                .WithText(localizer.Localize("start-print-job")))
                    )
                );
    }

    #region IO

    [ServerToolCommand("save")]
    public ApiEventResponse OnSaveClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<MapSeriesPrint> localizer)
    {
        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Pointer },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target=UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("tools.save"),
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements=new UIElement[]{
                        new UILabel(){ label = localizer.Localize("label-name") },
                        new UIBreak(),
                        new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge,this.GetType(), "autocomplete-series"),0){
                            id= "mapseriesprint-io-save-name",
                            css=UICss.ToClass(new string[]{UICss.ToolParameter}),
                        },
                        new UIButtonContainer(new UIButton(UIButton.UIButtonType.servertoolcommand,"save-series") {
                            text = localizer.Localize("save")
                        })
                    }
                }
            }
        };
    }

    [ServerToolCommand("autocomplete-series")]
    public ApiEventResponse OnAutocompleteProject(IBridge bridge, ApiToolEventArguments e)
    {
        List<string> values = new List<string>();
        string term = e["term"].ToLower();

        foreach (string name in bridge.Storage.GetNames().Select(n => n.FromValidEncodedName()))
        {
            if (name.ToLower().Contains(term))
            {
                values.Add(name);
            }
        }

        values.Sort();

        return new ApiRawJsonEventResponse(values.ToArray());
    }

    [ServerToolCommand("save-series")]
    public ApiEventResponse OnSaveProject(IBridge bridge, ApiToolEventArguments e, ILocalizer<MapSeriesPrint> localizer)
    {
        string name = e["mapseriesprint-io-save-name"];

        if (!name.IsValidFilename(out string invalidChars))
        {
            throw new Exception(String.Format(localizer.Localize("io.exception-invalid-char"), invalidChars));
        }

        var model = new PrintSeriesModel()
        {
            LayoutId = e[MapSeriesPrintLayoutId],
            Format = e[MapSeriesPrintFormatId],
            Scale = e.GetDouble(MapSeriesPrintScaleId),
            Quality = e.GetInt(MapSeriesPrintQualityId),
            SketchWKT = e.Sketch?.WKTFromShape(WKTFormat.WKTWithMetadata),
            SketchSrs = e.CalcCrs ?? 0
        };

        bridge.Storage.Save(name.ToValidEncodedName(), JSerializer.Serialize(model));

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] {
                new UIEmpty(){
                    target=UIElementTarget.modaldialog.ToString(),
                }
            }
        };
    }

    [ServerToolCommand("open")]
    public ApiEventResponse OnOpenClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<MapSeriesPrint> localizer)
    {
        var names = bridge.Storage.GetNames().Select(n => n.FromValidEncodedName()).ToArray();

        if (names == null || names.Length == 0)
        {
            throw new Exception(localizer.Localize("io.exception-no-projects-found"));
        }

        return new ApiEventResponse()
        {
            Graphics = new GraphicsResponse(bridge) { ActiveGraphicsTool = GraphicsTool.Pointer },
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target=UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("tools.open"),
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements=new UIElement[]{
                        new UILabel(){ label = localizer.Localize("label-name") },
                        new UIBreak(),
                        new UISelect(names){
                              id="mapseriesprint-io-load-name",
                              css=UICss.ToClass(new []{ UICss.ToolParameter }),
                        },
                        new UIButtonContainer(
                            new []
                            {
                                new UIButton(UIButton.UIButtonType.servertoolcommand,"delete-series") {
                                    css = UICss.ToClass(new []{ UICss.DangerButtonStyle }),
                                    text = localizer.Localize("delete")
                                },
                                new UIButton(UIButton.UIButtonType.servertoolcommand,"load-series") {
                                    text = localizer.Localize("open")
                                }
                            })
                    }
                }
            },
            UISetters = new IUISetter[]  // select/highlight tool
            {
                 new UISetter("webgis-mapmarkup-tool", "pointer")
            }
        };
    }

    [ServerToolCommand("load-series")]
    public ApiEventResponse OnLoadProject(IBridge bridge, ApiToolEventArguments e)
    {
        string name = e["mapseriesprint-io-load-name"]?.ToValidEncodedName();

        string json = bridge.Storage.LoadString(name);
        var model = JSerializer.Deserialize<PrintSeriesModel>(json);
        var sketch = !String.IsNullOrEmpty(model.SketchWKT) ? model.SketchWKT.ShapeFromWKT() : null;
        sketch.SrsId = model.SketchSrs;
        sketch.HasM = true;

        e[MapSeriesPrintFormatId] = model.Format;
        e[MapSeriesPrintLayoutId] = model.LayoutId;
        e[MapSeriesPrintScaleId] = ((int)model.Scale).ToString();
        e[MapSeriesPrintQualityId] = model.Quality.ToString();

        var response = OnSelectionChanged(bridge, e);

        response.AddUIElement(new UIEmpty().WithTarget(UIElementTarget.modaldialog.ToString()));
        response.AddUISetters(
            new UISetter(MapSeriesPrintLayoutId, model.LayoutId),
            new UISetter(MapSeriesPrintFormatId, model.Format),
            new UISetter(MapSeriesPrintQualityId, model.Quality.ToString()));
        response.Sketch = sketch;

        return response;
        //return new ApiEventResponse()
        //{
        //    Sketch = sketch,
        //}
        //.AddUIElement(new UIEmpty().WithTarget(UIElementTarget.modaldialog.ToString()))
        //.AddUISetters(
        //    new UISetter(MapSeriesPrintLayoutId, model.LayoutId),
        //    new UISetter(MapSeriesPrintFormatId, model.Format),
        //    new UISetter(MapSeriesPrintQualityId, model.Quality.ToString()),
        //    new UISetter(MapSeriesPrintScaleId, ((int)model.Scale).ToString()));
    }

    [ServerToolCommand("delete-series")]
    [ToolCommandConfirmation("io.confirm-delete-project", ApiToolConfirmationType.YesNo, ApiToolConfirmationEventType.ButtonClick)]
    public ApiEventResponse OnDeleteProject(IBridge bridge, ApiToolEventArguments e)
    {
        string name = e["mapseriesprint-io-load-name"]?.ToValidEncodedName();

        if (!String.IsNullOrEmpty(name))
        {
            bridge.Storage.Remove(name);
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] {
                new UIEmpty(){
                    target=UIElementTarget.modaldialog.ToString(),
                }
            }
        };
    }

    #endregion

    #region Download/Upload

    [ServerToolCommand("download")]
    public ApiEventResponse OnDownloadObjects(IBridge bridge, ApiToolEventArguments e)
    {
        var model = new PrintSeriesModel()
        {
            LayoutId = e[MapSeriesPrintLayoutId],
            Format = e[MapSeriesPrintFormatId],
            Scale = e.GetDouble(MapSeriesPrintScaleId),
            Quality = e.GetInt(MapSeriesPrintQualityId),
            SketchWKT = e.Sketch?.WKTFromShape(WKTFormat.WKTWithMetadata),
            SketchSrs = e.CalcCrs ?? 0
        };

        return new ApiRawDownloadEventResponse(
            "map-series-print.json", 
            Encoding.UTF8.GetBytes(JSerializer.Serialize(model, true)));
    }

    [ServerToolCommand("upload")]
    public ApiEventResponse OnUploadClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<MapSeriesPrint> localizer)
    {
        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("tools.upload"),
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements = new IUIElement[]
                    {
                        new UILabel()
                        {
                            label = localizer.Localize("io.upload-label1:body")
                        },
                        new UIBreak(2),
                        new UISelect()
                        {
                            id="mapseriesprint-upload-replaceelements",
                            css = UICss.ToClass(new string[]{ UICss.ToolParameter }),
                            options=new UISelect.Option[]
                            {
                                new UISelect.Option() { value = "false", label = localizer.Localize("io.extend-current-session") },
                                new UISelect.Option() { value = "true", label = localizer.Localize("io.replace-current-session") }
                            }
                        },
                        new UIBreak(2),
                        new UIUploadFile(this.GetType(), "upload-series") {
                            id="upload-file",
                            css=UICss.ToClass(new string[]{UICss.ToolParameter})
                        }
                    }
                }
            }
        };
    }

    [ServerToolCommand("upload-series")]
    public ApiEventResponse OnUploadObject(IBridge bridge, ApiToolEventArguments e)
    {
        var file = e.GetFile("upload-file");
        var replaceExistingMapMarkup = e["mapseriesprint-upload-replaceelements"] == "true";

        string json = Encoding.UTF8.GetString(file.Data);
        var model = JSerializer.Deserialize<PrintSeriesModel>(json);
        var sketch = !String.IsNullOrEmpty(model.SketchWKT) ? model.SketchWKT.ShapeFromWKT() : null;

        if(sketch is null)
        {
            throw new Exception("Uploaded file contains no valid geometry data.");
        }

        sketch.SrsId = model.SketchSrs;
        sketch.HasM = true;

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] {
                new UIEmpty(){
                    target=UIElementTarget.modaldialog.ToString(),
                }
            },
            Sketch = sketch
        };
    }

    #endregion

    #endregion

    #region IApiButtonPrintSeriesProvider

    public IEnumerable<PrintMapOrientation> GetPrintMapOrientations(Shape toolSketch)
    {
        var points = toolSketch as MultiPoint;
        if(points is null)
        {
            throw new ArgumentException("Tool sketch is null or not an valid Multipoint") ;
        }

        int index = 1;
        return points
                .ToArray()
                .Select(p => new PrintMapOrientation(GetMapSericesPrintPageName(index++), p, null, p switch
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
                name: GetMapSericesPrintPageName(index++),
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

        return graphicElements;
    }

    public PrintMapSeriesOverviewPageDefinition GetPrintMapSeriesOverviewPageDefinition(IMap mapPrototype)
    {
        var mapFrames = mapPrototype?.GraphicsContainer?.Where(e => e is MapFrameElement).ToArray();

        if (mapFrames?.Any() == false) return null;

        Envelope extent = null;
        mapFrames
            .Select(f => f.Extent)
            .Where(e => e is not null)
            .ToList()
            .ForEach(e =>{
                if(extent is null)
                {
                    extent = new Envelope(e);
                } 
                else
                {
                    extent.Union(e);
                }
            });
        extent?.Raise(110);
        var map = mapPrototype.Clone(null);

        map.Services.RemoveAll(s => s is not TileService && s is not IGraphicsService);

        return new PrintMapSeriesOverviewPageDefinition("layout_map_services_overview.xml", map, extent);
    }

    private string GetMapSericesPrintPageName(int index)
    {
        return $"M{index:000}";
    }

    #endregion

    #region Models

    private class PrintSeriesModel
    {
        [JsonProperty("layoutId")]
        [System.Text.Json.Serialization.JsonPropertyName("layoutId")]
        public string LayoutId { get; set; }

        [JsonProperty("format")]
        [System.Text.Json.Serialization.JsonPropertyName("format")]
        public string Format { get; set; }

        [JsonProperty("scale")]
        [System.Text.Json.Serialization.JsonPropertyName("scale")]
        public double Scale { get; set; }

        [JsonProperty("quality")]
        [System.Text.Json.Serialization.JsonPropertyName("quality")]
        public int Quality { get; set; }

        [JsonProperty("sketchWKT")]
        [System.Text.Json.Serialization.JsonPropertyName("sketchWKT")]
        public string SketchWKT { get; set; }

        [JsonProperty("sketchSrs")]
        [System.Text.Json.Serialization.JsonPropertyName("sketchSrs")]
        public int SketchSrs { get; set; }
    }

    #endregion
}
