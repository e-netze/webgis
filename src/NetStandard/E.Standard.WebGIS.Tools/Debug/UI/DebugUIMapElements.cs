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
using E.Standard.WebMapping.Core.Collections;

namespace E.Standard.WebGIS.Tools.Debug.UI;

/// <summary>
/// Debug tool: elements, that are filled from the services/themes of the current map
/// (querycombo, editthemecombo/-tree, chainagethemecombo, visfiltercombo/-list,
/// timefiltercontrol/-list, opacity-control, checkable menu items, menu items with highlight_feature).
/// </summary>
#if DEBUG
[Export(typeof(IApiButton))]
#endif
public class DebugUIMapElements : DebugUIToolBase
{
    private const string IdPrefix = "debug-ui-map-";
    private const string ServiceElementsCommand = "service-elements";

    public override string Name => "UI: Map Elements";

    public override string ToolTip => "Debug: combos/lists filled from the map services (queries, edit themes, filters, opacity)";

    public override IEnumerable<Type> DemonstratedTypes =>
    [
        typeof(UIQueryCombo), typeof(UIEditThemeCombo), typeof(UIEditThemeTree), typeof(UIChainageThemeCombo),
        typeof(UIVisFilterCombo), typeof(UIVisFilterListElement),
        typeof(UITimeFilterControlElement), typeof(UITimeFilterListElement),
        typeof(UIOpacityControl), typeof(UIMenuItemCheckable)
    ];

    public override Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var response = new ApiEventResponse()
            .AddUIElements(
                SketchSection(),
                Hint("All elements are filled on the client from the services of the current map. Empty combos/lists => the map has no matching themes (queries, edit themes, ...)."),

                Section("querycombo",
                    new UILabel() { label = "combotype = query (value: serviceId:queryId)" },
                    new UIQueryCombo() { id = Id("querycombo"), css = UICss.ToolParameter },
                    new UILabel() { label = "combotype = indentify, with customitems (first entries)" },
                    new UIQueryCombo(UIQueryCombo.ComboType.indentify)
                    {
                        id = Id("querycombo-identify"),
                        css = UICss.ToolParameter,
                        customitems = CustomItems()
                    },
                    EchoButton()),

                Section("service elements (from the selected query)",
                    Hint("Select a query in the 'querycombo' section (and draw a sketch), then create: opacity-control for the query's service, a checkable menu item (checkbox toggles the query's associated layers, only shown if the query has associated layers) and a menu item with highlight_feature (hover => the sketch is highlighted)."),
                    EchoButton("Create service elements", command: ServiceElementsCommand)),

                Section("editthemecombo / editthemetree",
                    new UILabel() { label = "editthemecombo" },
                    new UIEditThemeCombo() { id = Id("editthemecombo"), css = UICss.ToolParameter, onchange = EchoCommand },
                    new UILabel() { label = "editthemecombo, db_rights = \"i\" (only themes with insert rights expected) + customitems" },
                    new UIEditThemeCombo()
                    {
                        id = Id("editthemecombo-insert"),
                        css = UICss.ToolParameter,
                        db_rights = "i",
                        customitems = CustomItems()
                    },
                    new UILabel() { label = "editthemetree" },
                    new UIEditThemeTree() { id = Id("editthemetree"), css = UICss.ToolParameter },
                    EchoButton()),

                Section("chainagethemecombo",
                    new UILabel() { label = "with customitems (are they shown?)" },
                    new UIChainageThemeCombo() { id = Id("chainagethemecombo"), css = UICss.ToolParameter, customitems = CustomItems() },
                    EchoButton()),

                Section("visfiltercombo / visfilterlist",
                    Hint("onchange = 'echo'. The list shows the currently active filters of the map (set some with the 'Filter' tool)."),
                    new UIVisFilterCombo() { id = Id("visfiltercombo"), css = UICss.ToolParameter, onchange = EchoCommand },
                    new UIVisFilterListElement()),

                Section("timefiltercontrol / timefilterlist",
                    Hint("Only services with time information are listed."),
                    new UITimeFilterControlElement(),
                    new UITimeFilterListElement()));

        return Task.FromResult(response);
    }

    #region Commands

    [ServerToolCommand(ServiceElementsCommand)]
    public ApiEventResponse OnServiceElements(IBridge bridge, ApiToolEventArguments e)
    {
        var queryValue = e[Id("querycombo")];
        var parts = queryValue?.Split(':');

        if (parts is null || parts.Length != 2)
        {
            return EchoResponse(e, "No query selected", Hint("Select a query in the 'querycombo' section first."));
        }

        var highlightItem = new UIMenuItem(this, e, UIButton.UIButtonType.servertoolcommand, EchoCommand)
        {
            text = e.SketchWgs84 is null ? "highlight_feature (no sketch => none)" : "highlight_feature: hover => sketch preview",
            value = "highlight"
        };
        if (e.SketchWgs84 is not null)
        {
            highlightItem.highlight_feature = bridge.ToGeoJson(new FeatureCollection(new Feature() { Shape = e.SketchWgs84 }));
        }

        return new ApiEventResponse()
            .UIElementsBehavoir(AppendUIElementsMode.Append)
            .AddUIElement(new UIDiv() { title = $"service elements: {queryValue} ({DateTime.Now:HH:mm:ss})" }
                .AddChildren(
                    new UILabel() { label = $"opacity-control (service '{parts[0]}')" },
                    new UIOpacityControl(parts[0]),
                    new UIMenu()
                    {
                        header = "menu (click => echo)",
                        elements = [
                            new UIMenuItemCheckable(this, e, UIButton.UIButtonType.servertoolcommand, EchoCommand)
                            {
                                text = "checkable menuitem (value = query)",
                                value = queryValue
                            },
                            highlightItem
                        ]
                    }));
    }

    #endregion

    #region Helpers

    static private string Id(string name) => IdPrefix + name;

    static private UINameValue[] CustomItems()
        => [
            new UINameValue() { name = "custom item 1", value = "custom-1" },
            new UINameValue() { name = "custom item 2", value = "custom-2" }
        ];

    #endregion
}
