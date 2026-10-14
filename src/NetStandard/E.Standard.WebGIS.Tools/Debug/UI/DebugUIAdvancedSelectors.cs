using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Extensions;

namespace E.Standard.WebGIS.Tools.Debug.UI;

/// <summary>
/// Debug tool: advanced selectors (style selectors, symbol/image selector, print selectors),
/// query builder and static overlay control.
/// </summary>
#if DEBUG
[Export(typeof(IApiButton))]
#endif
public class DebugUIAdvancedSelectors : DebugUIToolBase, IApiButtonResources
{
    private const string IdPrefix = "debug-ui-advsel-";

    private const string QueryBuilderCommand = "querybuilder";  // fixed command name, sent by the webgis_queryBuilder plugin
    private const string OverlayInfoCommand = "overlay-info";
    private const string OverlayZoomCommand = "overlay-zoom";

    private static readonly string[] Images = ["pointer", "rectangle", "polygon", "circle"];

    public override string Name => "UI: Advanced Selectors";

    public override string ToolTip => "Debug: style/symbol/image selectors, print selectors, query builder, static overlay control";

    public override IEnumerable<Type> DemonstratedTypes =>
    [
        typeof(UIColorSelector), typeof(UIFontSizeSelector), typeof(UIFontStyleSelector),
        typeof(UILineStyleSelector), typeof(UILineWieghtSelector), typeof(UIOpacitySelector),
        typeof(UISymbolSelector), typeof(UIImageSelector),
        typeof(UIPrintAttachementsSelector), typeof(UIPrintMarkerSelector), typeof(UIPrintSketchSelector),
        typeof(UIQueryBuilder), typeof(UIStaticOverlayControl)
    ];

    public override Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var response = new ApiEventResponse()
            .AddUIElements(
                SketchSection(),

                Section("style selectors",
                    Hint("Option containers with client buttons (no request). Each selection is a tool parameter => check the values with 'Echo'."),
                    new UIColorSelector("UIColorSelector", UIButton.UIButtonType.clientbutton).WithId(Id("color")).AsToolParameter(),
                    new UIColorSelector("UIColorSelector (allowNoColor)", UIButton.UIButtonType.clientbutton, allowNoColor: true).WithId(Id("color-nocolor")).AsToolParameter(),
                    new UIFontSizeSelector("UIFontSizeSelector", UIButton.UIButtonType.clientbutton).WithId(Id("fontsize")).AsToolParameter(),
                    new UIFontStyleSelector("UIFontStyleSelector", UIButton.UIButtonType.clientbutton).WithId(Id("fontstyle")).AsToolParameter(),
                    new UILineStyleSelector("UILineStyleSelector", UIButton.UIButtonType.clientbutton).WithId(Id("linestyle")).AsToolParameter(),
                    new UILineWieghtSelector("UILineWieghtSelector", UIButton.UIButtonType.clientbutton).WithId(Id("lineweight")).AsToolParameter(),
                    new UIOpacitySelector("UIOpacitySelector", UIButton.UIButtonType.clientbutton).WithId(Id("opacity")).AsToolParameter(),
                    Hint("servertoolcommand variant: every click sends 'echo' with the selected value."),
                    new UIColorSelector(UIButton.UIButtonType.servertoolcommand, EchoCommand) { title = "UIColorSelector (servertoolcommand)", CollapseState = UICollapsableElement.CollapseStatus.Collapsed }
                        .WithId(Id("color-server")).AsToolParameter(),
                    EchoButton()),

                Section("symbol selector",
                    Hint("UISymbolSelector: all markers from content/api/img/graphics/markers. Initially selected: symbolId 'graphics/markers/marker_rot.gif' (if it exists). The value is a json object (id, icon, iconSize, ...)."),
                    new UISymbolSelector(bridge, "UISymbolSelector", UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.unknown, "graphics/markers/marker_rot.gif")
                        .WithId(Id("symbol")).AsToolParameter(),
                    EchoButton()),

                Section("image selector",
                    Hint("Single select: initial value = 2nd image, click again deselects. Multi select: comma separated urls. Without labels/size: default tiles."),
                    new UILabel() { label = "single select (labels, 96x96)" },
                    new UIImageSelector()
                    {
                        id = Id("image-single"),
                        ImageUrls = ImageUrls(),
                        ImageLabels = Images,
                        ImageWidth = 96,
                        ImageHeight = 96,
                        MultiSelect = false,
                        value = ImageUrls()[1],
                        css = UICss.ToClass([UICss.ToolParameter])
                    },
                    new UILabel() { label = "multi select (no labels, default size)" },
                    new UIImageSelector()
                    {
                        id = Id("image-multi"),
                        ImageUrls = ImageUrls(),
                        MultiSelect = true,
                        css = UICss.ToClass([UICss.ToolParameter])
                    },
                    EchoButton()),

                Section("print selectors",
                    Hint("The parts of the print selectors depend on the map state (VisibilityDependency): " +
                         "query results => query markers/attachments, coordinate tool results => coordinate markers/attachments, " +
                         "chainage results => chainage markers, tool sketch => sketch selector. Without them the containers are (nearly) empty."),
                    new UIPrintMarkerSelector(this, Id("print-marker")),
                    new UIPrintSketchSelector(Id("print-sketch")),
                    new UIPrintAttachementsSelector(Id("print-attachment")),
                    EchoButton()),

                Section("query builder",
                    Hint($"'Apply' sends servertoolcommand_ext '{QueryBuilderCommand}' (callback_tool_id = this tool) with the parameter '<id>-result' (json)."),
                    new UILabel() { label = "empty, with geometry option" },
                    QueryBuilder("querybuilder", showGeometryOption: true, argument: "empty"),
                    new UILabel() { label = "initial where clause: NAME like 'A%' and AREA >= 100 or IS_ACTIVE = 1" },
                    QueryBuilder("querybuilder-where", showGeometryOption: false, argument: "where",
                        whereClause: "NAME like 'A%' and AREA >= 100 or IS_ACTIVE = 1")),

                Section("static overlay control",
                    Hint("Lists the static overlay services of the map (e.g. added by the georeferencing tool) => without overlays the list is empty. " +
                         "Click: select (value) / passpoints, sort by drag&drop, remove, topmost, opacity. " +
                         $"The command buttons send '{OverlayInfoCommand}'/'{OverlayZoomCommand}' with the service id as argument."),
                    new UIStaticOverlayControl()
                    {
                        id = Id("static-overlay"),
                        css = UICss.ToClass([UICss.ToolParameter]),
                        command_buttons =
                        [
                            new UIStaticOverlayControl.CommandButton(OverlayInfoCommand, UIImageButton.ToolResourceImage(this, "pointer")),
                            new UIStaticOverlayControl.CommandButton(OverlayZoomCommand, UIImageButton.ToolResourceImage(this, "rectangle"))
                        ]
                    },
                    EchoButton()));

        return Task.FromResult(response);
    }

    #region IApiButtonResources

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("pointer", Properties.Resources.pointer);
        toolResourceManager.AddImageResource("rectangle", Properties.Resources.rectangle);
        toolResourceManager.AddImageResource("polygon", Properties.Resources.polygon);
        toolResourceManager.AddImageResource("circle", Properties.Resources.circle);
    }

    #endregion

    #region Commands

    [ServerToolCommand(QueryBuilderCommand)]
    public ApiEventResponse OnQueryBuilder(IBridge bridge, ApiToolEventArguments e)
        => EchoResponse(e, "Echo: querybuilder (see <id>-result)");

    [ServerToolCommand(OverlayInfoCommand)]
    public ApiEventResponse OnOverlayInfo(IBridge bridge, ApiToolEventArguments e)
        => EchoResponse(e, "Echo: static overlay command button 1");

    [ServerToolCommand(OverlayZoomCommand)]
    public ApiEventResponse OnOverlayZoom(IBridge bridge, ApiToolEventArguments e)
        => EchoResponse(e, "Echo: static overlay command button 2");

    #endregion

    #region Helpers

    static private string Id(string name) => IdPrefix + name;

    private string[] ImageUrls()
        => Array.ConvertAll(Images, i => UIImageButton.ToolResourceImage(this, i));

    private UIQueryBuilder QueryBuilder(string id, bool showGeometryOption, string argument, string whereClause = null)
    {
        var queryBuilder = new UIQueryBuilder()
        {
            id = Id(id),
            ShowGeometryOption = showGeometryOption,
            CallbackToolId = GetType().ToToolId(),
            CallbackArgument = argument
        };

        queryBuilder.TryAddField("NAME", FieldType.String, "Name (string)");
        queryBuilder.TryAddField("AREA", FieldType.Double, "Area (double)");
        queryBuilder.TryAddField("COUNT", FieldType.Interger, "Count (int)");
        queryBuilder.TryAddField("IS_ACTIVE", FieldType.Boolean, "Active (bool)");
        queryBuilder.TryAddField("CREATED", FieldType.Date, "Created (date)");

        if (!String.IsNullOrEmpty(whereClause))
        {
            queryBuilder.TrySetWhereClause(whereClause);
        }

        return queryBuilder;
    }

    #endregion
}
