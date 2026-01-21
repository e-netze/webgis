using E.Standard.Extensions.Collections;
using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
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
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static E.Standard.WebMapping.Core.Api.UI.Elements.UICollapsableElement;

namespace E.Standard.WebGIS.Tools.Export;

[Export(typeof(IApiButton))]
[ToolHelp("tools/map/print.html")]
[ToolConfigurationSection("print")]
[ToolId("webgis.tools.print")]
[AdvancedToolProperties(
        MapBBoxDependent = true,
        MapCrsDependent = true,
        PrintLayoutRotationDependent = true,
        ScaleDependent = true,
        QueryMarkersVisibliityDependent = true,
        CoordinateMarkersVisibilityDependent = true,
        ChainageMarkersVisibilityDependent = true
    )]
public class Print : IApiServerToolLocalizable<Print>,
                     IApiButtonResources
{
    public const string ConfigQualitiesDpi = "qualities-dpi";
    public const string ConfigDefaultQuality = "default-quality";
    public const string ConfigDefaultFormat = "default-format";

    #region IApiServerTool

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Print> localizer)
    {
        var printLayouts = bridge.GetPrintLayouts(e.GetToolOptions<string[]>());
        var printFormats = bridge.GetPrintFormats();

        var qualitites = e.GetConfigDictionay<int, string>(ConfigQualitiesDpi);
        var markersSelector = new UIPrintMarkerSelector(this, "print-marker-selector");

        return new ApiEventResponse()
            .AddMapViewLense(Lense.Current)  // Keep current, if tool is already selected
            .AddUIElements(
                new UIHidden()
                    .WithId("print-map-scales")
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
                        .WithId("print-layout-select")
                        .AsPersistantToolParameter(UICss.PrintToolLayout, UICss.ToolInitializationParameterImportant)
                        .AddOptions(printLayouts.Select(l => new UISelect.Option()
                                                                    .WithValue(l.Id)
                                                                    .WithLabel(l.Name))),
                    new UILabel()
                        .WithLabel(localizer.Localize("format")),
                    UISelect.PrintFormats(this, "print-format-select", printFormats, UIButton.UIButtonType.servertoolcommand, "selectionchanged", defaultValue: e.GetConfigValue(ConfigDefaultFormat)),
                    new UILabel()
                        .WithLabel(localizer.Localize("print-scale")),
                    UISelect.Scales("print-scale-select", UIButton.UIButtonType.servertoolcommand, "selectionchanged", allowAddValues: true, scales: e.GetConfigArray<int>("scales")),
                    new UILabel()
                        .WithLabel(localizer.Localize("print-quality")),
                    UISelect.PrintQuality(this, "print-qualitity-select", qualitites)
                    ),
                new UIDiv()
                    .WithVisibilityDependency(VisibilityDependency.ToolSketchesExists)
                    .AddChild(new UIPrintSketchSelector("print-sketch-selector")),
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
            .AddUISetter(new UIApplyPersistentParametersSetter(this))
            .AddUISetters(new IUISetter[] {
                new UISetter(markersSelector.QueryMarkersVisibilitySelectorId, e.QueryMarkersVisible() ? "show" : ""),
                new UISetter(markersSelector.CoodianteMakersVisiblitySelectorId, e.CoordinateMarkersVisible() ? "show" : ""),
                new UISetter(markersSelector.ChainageMarkersVisiblitySelectorId, e.ChainageMarkersVisible() ? "show" : "")
            });
    }

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<Print> localizer) => null;

    public ToolType Type => ToolType.print_preview;

    public ToolCursor Cursor => ToolCursor.Pointer;

    #endregion

    #region IApiButton Member

    public string Name => "Print";

    public string Container => "Map";

    public string Image => UIImageButton.ToolResourceImage(this, "print");

    public string ToolTip => "Print the current map section in PDF format.";

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
            e["print-init-scale"] = e.MapScale.Value.ToString();
        }
        return OnSelectionChanged(bridge, e);
    }


    [ServerToolCommand("selectionchanged")]
    public ApiEventResponse OnSelectionChanged(IBridge bridge, ApiToolEventArguments e)
    {
        string layoutId = e["print-layout-select"];
        string layoutFormat = e["print-format-select"];
        double scale = !String.IsNullOrEmpty(e["print-init-scale"]) ?
                            e.GetDouble("print-init-scale") :
                            e.GetDouble("print-scale-select");
        var scaleComboValues = new List<int>(e.TryGetArray<int>("print-scale-select.values") ?? new int[0]);

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

        response.AddUISetter(new UISelectOptionsSetter("print-format-select", $"{pageSize}.{pageOrientation}",
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
                new UISelectOptionsSetter("print-scale-select", scale.ToString(),
                                          scales.Select(s => new UISelect.Option()
                                                                         .WithValue(s.ToString())
                                                                         .WithLabel(string.Format("1:{0:0,0.}", s).Replace(" ", ".")))));
        }

        #endregion

        var properties = bridge.GetPrintLayoutProperties(layoutId, pageSize, pageOrientation, scale);

        double? lenseScale = null;
        if (e.GetConfigBool("scale-wysiwyg", false) == true)
        {
            lenseScale = scale;
        }

        float lenseFactor = 1f;

        var mapSRef = bridge.CreateSpatialReference((int)e.MapCrs);
        var mapEnvelope = new Envelope(e.MapBBox());

        if (mapSRef.IsWebMercator())
        {
            lenseFactor = 1f / (float)Math.Cos(mapEnvelope.CenterPoint.Y / 180.0 * Math.PI);
        }

        return response
                .AddMapViewLense(new Lense()
                                     .WithSize(properties.WorldSize.Width * lenseFactor, properties.WorldSize.Height * lenseFactor)
                                     .WithScale(lenseScale)
                                     .WithScaleControlId("print-scale-select")
                                     .ZommTo());
    }

    [ServerToolCommand("print")]
    async public Task<ApiEventResponse> OnPrint(IBridge bridge, ApiToolEventArguments e, ILocalizer<Print> localizer)
    {
        string layoutId = e["print-layout-select"];
        string layoutFormat = e["print-format-select"];
        double scale = e.GetDouble("print-scale-select");
        int dpi = e.GetInt("print-qualitity-select");

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

        #region Features um aktuellen Auschnitt hinzufügen

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
                            .WithStyles(UICss.PrintToolSketchLabels)
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
}
