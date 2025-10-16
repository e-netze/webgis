using E.Standard.WebMapping.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UICss
{
    public const string DontOverrideWithQueryStringParameters = "initialized";
    public const string ToolParameter = "webgis-tool-parameter";
    public const string ToolParameterPersistent = "webgis-tool-parameter-persistent";
    public const string ToolParameterPersistentImportant = "webgis-tool-parameter-persistent webgis-tool-parameter-persistent-important";
    public const string ToolParameterRequired = "webgis-tool-parameter-required";
    public const string ToolParameterRequiredClientside = "webgis-tool-parameter-required clientside";
    public const string ToolInitializationParameter = "webgis-tool-initialization-parameter";
    public const string ToolInitializationParameterImportant = "webgis-tool-initialization-parameter webgis-tool-initialization-parameter-important";
    public const string ToolAutocompleteParameter = "webgis-autocomplete-parameter";

    public const string AutoSetterMapScale = "webgis-map-scale";
    public const string AutoSetterMapScales = "webgis-map-scales";
    public const string AutoSetterMapCrsId = "webgis-map-crsid";
    public const string AutoSetterAllQueries = "webgis-all-queries";
    public const string AutoSetterAllEditThemes = "webgis-all-editthemes";
    public const string AutoSetterAllEidtThemesInScale = "webgis-all-editthemes-inscale";
    public const string AutoSetterAllServices = "webgis-all-services";
    //public const string AutoSetterActiveVisFilters = "webgis-active-visfilters";
    //public const string AutoSetterActiveLabelings = "webgis-active-labelings";

    public const string AutoSetterMapBBox = "webgis-map-bbox";
    public const string AutoSetterMapSize = "webgis-map-size";

    public const string AutoSetterMapGraphicsTool = "webgis-map-graphics-tool";
    public const string AutoSetterMapGraphicsColor = "webgis-map-graphics-color";
    public const string AutoSetterMapGraphicsOpacity = "webgis-map-graphics-opacity";
    public const string AutoSetterMapGraphicsFillColor = "webgis-map-graphics-fillcolor";
    public const string AutoSetterMapGraphicsFillOpacity = "webgis-map-graphics-fillopacity";
    public const string AutoSetterMapGraphicsLineWeight = "webgis-map-graphics-lineweight";
    public const string AutoSetterMapGraphicsLineStyle = "webgis-map-graphics-linestyle";
    public const string AutoSetterMapGraphicsSymbol = "webgis-map-graphics-symbol";
    public const string AutoSetterMapGraphicsGeoJson = "webgis-map-graphics-geojson";
    public const string AutoSetterMapGraphicsFontColor = "webgis-map-graphics-fontcolor";
    public const string AutoSetterMapGraphicsFontSize = "webgis-map-graphics-fontsize";
    public const string AutoSetterMapGraphicsFontStyle = "webgis-map-graphics-fontstyle";
    public const string AutoSetterMapGraphicsPointColor = "webgis-map-graphics-pointcolor";
    public const string AutoSetterMapGraphicsPointSize = "webgis-map-graphics-pointsize";
    public const string AutoSetterCurrentToolSketch = "webgis-current-toolsketch";


    public const string AutoSetterMapBuilderRawMapDescription = "webgis-mapbuilder-description-raw";
    public const string AutoSetterMapBuilderHtmlMetaTags = "webgis-mapbuilder-html-meta-tags";

    public const string AutoSetterMapPageId = "webgis-map-page-id";
    public const string AutoSetterMapName = "webgis-map-name";
    public const string AutoSetterMapCategory = "webgis-map-category";

    public const string AutoSetterAnoymousUserId = "webgis-map-anonymous-user-id";

    public const string AutoSetterMapTools = "webgis-map-tools";
    public const string AutoSetterToolOptions = "webgis-tool-options";

    public const string AutoSetterMapSerialization = "webgis-map-serialization";
    public const string AutoSetterMapCurrentVisibility = "webgis-map-currentvisibility";

    public const string CancelButtonStyle = "uibutton-cancel";
    public const string OkButtonStyle = "uibutton-ok";
    public const string OptionButtonStyle = "uibutton-option";
    public const string OptionRectButtonStyle = "uibutton-option-rect";
    public const string OptionRectButton_Style = "uibutton-option-rect";
    public const string DefaultButtonStyle = "uibutton-default";
    public const string DangerButtonStyle = "uibutton-danger";
    public const string ButtonIcon = "uibutton-icon";
    public const string LineBreakButton = "uibutton-line-break";
    public const string ValidateInputButton = "uibutton-validate-input";
    public const string WrapText = "webgis-wraptext";

    public const string Width_10Percent = "webgis-width-10percent";
    public const string Width_20Percent = "webgis-width-20percent";
    public const string Width_25Percent = "webgis-width-25percent";
    public const string Width_33Percent = "webgis-width-33percent";
    public const string Width_50Percent = "webgis-width-50percent";
    public const string Width_100Percent = "webgis-width-100percent";

    public const string JoinLiveshareSessionButton = "uibutton-join-liveshare";

    public const string ValidationContainer = "webgis-validation-container";
    public const string FormContainer = "webgis-form-container";

    public const string OptionContainerWithLabels = "contains-labels";
    public const string ImageButtonWithLabelsContaier = "image-button-container contains-labels";

    public const string InputSetBorderOnChange = "input-setborder-onchange";
    public const string InputAutocomplete = "webgis-autocomplete";

    public const string PrintToolLayout = "webgis-print-tool-layout";
    public const string PrintToolFormat = "webgis-print-tool-format";
    public const string PrintToolQuality = "webgis-print-tool-quality";
    public const string PrintTextElement = "webgis-print-textelement";
    public const string PrintHeaderIdElement = "webgis-print-header-id-element";
    public const string PrintShowQueryMarkers = "webgis-print-show-query-markers";
    public const string PrintShowCoordinatesMarkers = "webgis-print-show-coords-markers";
    public const string PrintQueryMarkerLabelField = "webgis-print-query-markers-label-field";
    public const string PrintShowChainageMarkers = "webgis-print-show-chainage-markers";
    public const string PrintCoordinatesMarkerLabelField = "webgis-print-coords-markers-label-field";
    public const string PrintAttachQueryResults = "webgis-print-attach-query-results";
    public const string PrintAttachCoordinates = "webgis-print-attach-coordinates";
    public const string PrintAttachCoordinatesFieldId = "webgis-print-attach-coordinates-field";
    public const string PrintToolSketch = "webgis-print-show-tool-sketch";
    public const string PrintToolSketchLabels = "webgis-print-tool-sketch-labels";

    public const string PrintShowQueryMarkersSelect = "webgis-print-show-query-markers-select";
    public const string PrintShowCoordinateMarkersSelect = "webgis-print-show-coordinate-markers-select";
    public const string PrintShowChainageMarkersSelect = "webgis-print-show-chainage-markers-select";

    public const string MapSeriesPrintToolLayout = "webgis-mapseriesprint-tool-layout";
    public const string MapSeriesPrintToolFormat = "webgis-mapseriesprint-tool-format";
    public const string MapSeriesPrintToolQuality = "webgis-mapseriesprint-tool-quality";
    public const string MapSeriesPrintTextElement = "webgis-mapseriesprint-textelement";
    public const string MapSeriesPrintHeaderIdElement = "webgis-mapseriesprint-header-id-element";
    public const string MapSeriesPrintShowQueryMarkers = "webgis-mapseriesprint-show-query-markers";
    public const string MapSeriesPrintShowCoordinatesMarkers = "webgis-mapseriesprint-show-coords-markers";
    public const string MapSeriesPrintQueryMarkerLabelField = "webgis-mapseriesprint-query-markers-label-field";
    public const string MapSeriesPrintShowChainageMarkers = "webgis-mapseriesprint-show-chainage-markers";
    public const string MapSeriesPrintCoordinatesMarkerLabelField = "webgis-mapseriesprint-coords-markers-label-field";
    public const string MapSeriesPrintAttachQueryResults = "webgis-mapseriesprint-attach-query-results";
    public const string MapSeriesPrintAttachCoordinates = "webgis-mapseriesprint-attach-coordinates";
    public const string MapSeriesPrintAttachCoordinatesFieldId = "webgis-mapseriesprint-attach-coordinates-field";
    public const string MapSeriesPrintToolSketch = "webgis-mapseriesprint-show-tool-sketch";
    public const string MapSeriesPrintToolSketchLables = "webgis-mapseriesprint-tool-sketch-labels";

    public const string MapSeriesPrintShowQueryMarkersSelect = "webgis-mapseriesprint-show-query-markers-select";
    public const string MapSeriesPrintShowCoordinateMarkersSelect = "webgis-mapseriesprint-show-coordinate-markers-select";
    public const string MapSeriesPrintShowChainageMarkersSelect = "webgis-mapseriesprint-show-chainage-markers-select";

    public const string DownloadMapImageBBox = "webgis-download-mapimage-bbox";
    public const string DownloadMapImageSize = "webgis-download-mapimage-size";
    public const string DownloadMapImageDpi = "webgis-download-mapimage-dpi";
    public const string DownloadMapImageFormat = "webgis-download-mapimage-format";
    public const string DownloadMapImageWorldfile = "webgis-download-mapimage-worldfile";

    public const string CollapsableAutoClick = "webgis-ui-collapsable-autoclick";

    static public string ToolResultElement(Type toolType)
    {
        return $"webgis-tool-result-{toolType.ToToolId().Replace(".", "-")}";
    }
    static public string ToolResultCounter(Type toolType)
    {
        return $"webgis-tool-result-counter-{toolType.ToToolId().Replace(".", "-")}";
    }

    public const string GraphicsDistanceCircleSteps = "webgis-graphics-distance_circle-steps";
    public const string GraphicsDistanceCircleRadius = "webgis-graphics-distance_circle-radius";

    public const string GraphicsCompassRoseSteps = "webgis-graphics-compass_rose-steps";

    public const string GraphicsHectolineUnit = "webgis-graphics-hectoline-unit";
    public const string GraphicsHectolineInterval = "webgis-graphics-hectoline-interval";

    public const string GraphicsDimLineLengthUnit = "webgis-graphics-dimline-length-unit";
    public const string GraphicsDimLineLabelTotalLength = "webgis-graphics-dimline-label-total-length";

    public const string GraphicsDimPolygonAreaUnit = "webgis-graphics-dimpolygon-area-unit";
    public const string GraphicsDimPolygonLabelEdges = "webgis-graphics-dimpolygon-label-edges";

    public const string ModalCloseElement = "webgis-modal-close-element";

    public const string MapScalesSelect = "webgis-map-scales-select";

    public const string HiddenUIElement = "webgis-ui-hidden";

    public const string EventTriggerOnSketchClosed = "webgis-event-trigger-onsketchclosed";

    public const string UIButtonContainer = "webgis-uibutton-container";

    public const string TableAlternateRowColor = "webgis-table-alternate-row-color";

    public const string EmptyOnChangeSketch = "webgis-ui-emtpy-onchage-sketch";

    public const string Narrow = "webgis-narrow";
    public const string NarrowForm = "webgis-narrow-form";
    public const string NarrowFormMarginAuto = "webgis-narrow-form webgis-margin-auto";

    public const string AttachmentItem = "webgis-attachment-item";

    public static string ToClass(IEnumerable<string> css)
    {
        return String.Join(" ", css);
    }

    public static string ToClass(string style, IEnumerable<string> css)
    {
        if (css == null || css.Count() == 0)
        {
            return style ?? String.Empty;
        }

        if (String.IsNullOrEmpty(style))
        {
            return ToClass(css);
        }

        return $"{style} {ToClass(css)}";
    }

    public static string ToClass(string style1, string style2)
    {
        if (String.IsNullOrEmpty(style1))
        {
            return style2 ?? String.Empty;
        }

        if (String.IsNullOrEmpty(style2))
        {
            return style1 ?? String.Empty;
        }

        return ToClass(new[] { style1, style2 });
    }
}
