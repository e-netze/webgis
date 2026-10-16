using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;

using WebGIS.UI.Tests.Infrastructure;

namespace WebGIS.UI.Tests.UIBuilder;

/// <summary>
/// Characterization tests for the combo elements
/// (package 5: select, querycombo, editthemecombo, editthemetree, chainagethemecombo,
/// circleradiuscombo, print-query-labelfield-combo, print-coordinates-labelfield-combo, sketchselect).
/// </summary>
[TestClass]
public class ComboElementsTests : UIBuilderPageTest
{
    private static readonly object TestTool = new { id = "test-tool", type = "clientbutton" };

    // one service with queries, editthemes and chainage themes (plain objects, used by the combo plugins)
    private const string ServicesSetup = @"h => h.setProperty('map.services', {
        s1: {
            id: 's1', name: 'Dienst 1', map: h.map, serviceInfo: { id: 's1', name: 'Dienst 1' },
            getLayer: function (id) { return id === 'l1' ? { id: 'l1', type: 'point' } : null; },
            checkLayerVisibility: h.fake('service.checkLayerVisibility'),
            setLayerVisibility: h.fake('service.setLayerVisibility'),
            layerVisibleAndInScale: function () { return true; },
            queries: [
                { id: 'q1', name: 'Abfrage 1', layerid: 'l1', items: [{ id: 'f1' }] },
                { id: 'q2', name: 'Unsichtbar', visible: false, items: [{ id: 'f1' }] },
                { id: 'q3', name: 'Ohne Items', items: [] }
            ],
            editthemes: [
                { layerid: 'l1', themeid: 't1', name: 'Thema 1', dbrights: 'iud', tags: ['a', 'b'] },
                { layerid: 'l2', themeid: 't2', name: 'Thema 2', dbrights: 'i' }
            ],
            chainagethemes: [{ id: 'c1', name: 'Station 1' }]
        }
    })";

    private static readonly UINameValue[] CustomItems =
    [
        new UINameValue { name = "Eigenes 1", value = "#" },
        new UINameValue { name = "Eigenes 2", value = "c2", category = "Kategorie" }
    ];

    private static IUIElement Create(string type) => type switch
    {
        "select" => new UISelect { id = "combo" },
        "querycombo" => new UIQueryCombo { id = "combo" },
        "editthemecombo" => new UIEditThemeCombo { id = "combo" },
        "editthemetree" => new UIEditThemeTree { id = "combo" },
        "chainagethemecombo" => new UIChainageThemeCombo { id = "combo" },
        "circleradiuscombo" => new UIMarkerCircleRadiusCombo { id = "combo" },
        "print-query-labelfield-combo" => new UIElement("print-query-labelfield-combo") { id = "combo" },
        "print-coordinates-labelfield-combo" => new UIElement("print-coordinates-labelfield-combo") { id = "combo" },
        "sketchselect" => new UISketchSelect { id = "combo" },
        _ => throw new ArgumentException(type)
    };

    #region common variants (all combo types)

    [TestMethod]
    [DataRow("select")]
    [DataRow("querycombo")]
    [DataRow("editthemecombo")]
    [DataRow("editthemetree")]
    [DataRow("chainagethemecombo")]
    [DataRow("circleradiuscombo")]
    [DataRow("print-query-labelfield-combo")]
    [DataRow("print-coordinates-labelfield-combo")]
    [DataRow("sketchselect")]
    public async Task Combo_Default(string type)
    {
        await BuildJsonAsync(UIJson.Serialize(Create(type)));
        await AssertSnapshotAsync(type, "default");
    }

    [TestMethod]
    [DataRow("select")]
    [DataRow("querycombo")]
    [DataRow("editthemecombo")]
    [DataRow("editthemetree")]
    [DataRow("chainagethemecombo")]
    [DataRow("circleradiuscombo")]
    [DataRow("print-query-labelfield-combo")]
    [DataRow("print-coordinates-labelfield-combo")]
    [DataRow("sketchselect")]
    public async Task Combo_IdCssStyle(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { css = "custom-css", style = "width:100px", vis_dependency = "webgis-dependencies webgis-dependency-hasselection" }));
        await AssertSnapshotAsync(type, "id-css-style");
    }

    [TestMethod]
    [DataRow("select")]
    [DataRow("querycombo")]
    [DataRow("editthemecombo")]
    [DataRow("editthemetree")]
    [DataRow("chainagethemecombo")]
    [DataRow("circleradiuscombo")]
    [DataRow("print-query-labelfield-combo")]
    [DataRow("print-coordinates-labelfield-combo")]
    [DataRow("sketchselect")]
    public async Task Combo_Invisible(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { visible = false }));
        await AssertSnapshotAsync(type, "invisible");
    }

    [TestMethod]
    [DataRow("select")]
    [DataRow("querycombo")]
    [DataRow("editthemecombo")]
    [DataRow("editthemetree")]
    [DataRow("chainagethemecombo")]
    [DataRow("circleradiuscombo")]
    [DataRow("print-query-labelfield-combo")]
    [DataRow("print-coordinates-labelfield-combo")]
    [DataRow("sketchselect")]
    public async Task Combo_WithTool(string type)
    {
        await BuildJsonAsync(UIJson.Serialize(Create(type)), options: new { tool = TestTool });
        await AssertSnapshotAsync(type, "with-tool");
    }

    [TestMethod]
    [DataRow("select")]
    [DataRow("querycombo")]
    [DataRow("editthemecombo")]
    [DataRow("chainagethemecombo")]
    [DataRow("sketchselect")]
    public async Task Combo_StringValue(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { value = "v'1" }));
        await AssertSnapshotAsync(type, "string-value");
    }

    [TestMethod]
    [DataRow("select")]
    [DataRow("querycombo")]
    [DataRow("editthemecombo")]
    [DataRow("editthemetree")]
    [DataRow("chainagethemecombo")]
    public async Task Combo_WithServices(string type)
    {
        await BuildJsonAsync(UIJson.Serialize(Create(type)), setup: ServicesSetup);
        await AssertSnapshotAsync(type, "with-services");
    }

    [TestMethod]
    [DataRow("select")]
    [DataRow("querycombo")]
    [DataRow("circleradiuscombo")]
    [DataRow("sketchselect")]
    public async Task Combo_InFormContainer_MarksDirty(string type)
    {
        await BuildJsonAsync(UIJson.Serialize(Create(type)),
            setup: "h => { $('#host').addClass('webgis-form-container'); h.returns('map.currentToolSketches', []); }");
        await TriggerAsync("#combo", "change");
        await AssertSnapshotAsync(type, "form-container-changed");
    }

    #endregion

    #region querycombo

    [TestMethod]
    public async Task QueryCombo_IdentifyType()
    {
        await BuildAsync(new UIQueryCombo(UIQueryCombo.ComboType.indentify) { id = "combo" }, setup: ServicesSetup);
        await AssertSnapshotAsync("querycombo", "identify-type");
    }

    [TestMethod]
    public async Task QueryCombo_CustomItems()
    {
        await BuildAsync(new UIQueryCombo { id = "combo", customitems = CustomItems }, setup: ServicesSetup);
        await AssertSnapshotAsync("querycombo", "customitems");
    }

    [TestMethod]
    public async Task QueryCombo_CustomItems_ListVisibleQueries()
    {
        await BuildAsync(new UIQueryCombo { id = "combo", customitems = CustomItems },
            setup: "h => { webgis.usability.listVisibleQueriesInQueryCombo = true; }");
        await AssertSnapshotAsync("querycombo", "customitems-list-visible-queries");
    }

    [TestMethod]
    public async Task QueryCombo_InitialQueryTheme()
    {
        await BuildAsync(new UIQueryCombo { id = "combo" },
            setup: "h => { webgis.initialParameters.querythemeid = 'q1'; (" + ServicesSetup + ")(h); }");
        await AssertSnapshotAsync("querycombo", "initial-query-theme");
    }

    [TestMethod]
    public async Task QueryCombo_Changed()
    {
        await BuildAsync(new UIQueryCombo { id = "combo" }, setup: ServicesSetup);
        await ClearCallsAsync();
        await TriggerAsync("select#combo", "change", "s1:q1");
        await AssertSnapshotAsync("querycombo", "changed");
    }

    #endregion

    #region editthemecombo / editthemetree

    [TestMethod]
    public async Task EditThemeCombo_CustomItems()
    {
        await BuildAsync(new UIEditThemeCombo { id = "combo", customitems = CustomItems }, setup: ServicesSetup);
        await AssertSnapshotAsync("editthemecombo", "customitems");
    }

    [TestMethod]
    [DataRow("editthemecombo")]
    [DataRow("editthemetree")]
    public async Task EditTheme_DbRights(string type)
    {
        // the builder passes element.dbrights (not db_rights) to the plugin
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { dbrights = "u", db_rights = "x" }), setup: ServicesSetup);
        await AssertSnapshotAsync(type, "dbrights");
    }

    [TestMethod]
    public async Task EditThemeTree_Persistent()
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create("editthemetree"), new { css = "webgis-tool-parameter-persistent" }),
            setup: "h => { (" + ServicesSetup + ")(h); h.returns('map.getPersistentToolParameter', 's1,l2,t2'); }");
        await AssertSnapshotAsync("editthemetree", "persistent");
    }

    [TestMethod]
    public async Task EditThemeTree_ItemClicked()
    {
        await BuildAsync(Create("editthemetree"), setup: ServicesSetup);
        await ClearCallsAsync();
        await TriggerAsync("li[data-edittheme='s1,l1,t1']", "click");
        await AssertSnapshotAsync("editthemetree", "item-clicked");
    }

    #endregion

    #region circleradiuscombo

    [TestMethod]
    public async Task CircleRadiusCombo_SelectedRadius()
    {
        await BuildAsync(Create("circleradiuscombo"),
            setup: "h => h.setProperty('map._circleMarkerOptions', { radius: 1000 })");
        await AssertSnapshotAsync("circleradiuscombo", "selected-radius");
    }

    [TestMethod]
    public async Task CircleRadiusCombo_CustomRadii()
    {
        await BuildAsync(new UIMarkerCircleRadiusCombo { id = "combo", radii = [10, 20, 1500] },
            setup: "h => h.setProperty('map._circleMarkerOptions', { radius: 20 })");
        await AssertSnapshotAsync("circleradiuscombo", "custom-radii");
    }

    [TestMethod]
    public async Task CircleRadiusCombo_Changed()
    {
        await BuildAsync(new UIMarkerCircleRadiusCombo { id = "combo", radii = [10, 20, 1500] },
            setup: "h => h.setProperty('map._circleMarkerOptions', { radius: 20 })");
        await ClearCallsAsync();
        await TriggerAsync("select#combo", "change", "1500");
        await AssertSnapshotAsync("circleradiuscombo", "changed");
    }

    #endregion

    #region print-query-labelfield-combo

    private const string QueryFeatureObjectSetup = @"h => h.returns('map.queryResultFeatures.first',
        { properties: { NAME: 'Name 1', _fulltext: 'x', _label: 'y', _hidden: 'z', LINK: '<a href=""#"">link</a>', NUM: 5 } })";

    [TestMethod]
    public async Task PrintQueryLabelFieldCombo_ObjectProperties()
    {
        await BuildAsync(Create("print-query-labelfield-combo"), setup: QueryFeatureObjectSetup);
        await AssertSnapshotAsync("print-query-labelfield-combo", "object-properties");
    }

    [TestMethod]
    public async Task PrintQueryLabelFieldCombo_ArrayProperties()
    {
        await BuildAsync(Create("print-query-labelfield-combo"),
            setup: "h => h.returns('map.queryResultFeatures.first', { properties: [{ A: '1', B: '2' }, { C: '3' }] })");
        await AssertSnapshotAsync("print-query-labelfield-combo", "array-properties");
    }

    [TestMethod]
    public async Task PrintQueryLabelFieldCombo_EmptyArrayProperties()
    {
        await BuildAsync(Create("print-query-labelfield-combo"),
            setup: "h => h.returns('map.queryResultFeatures.first', { properties: [] })");
        await AssertSnapshotAsync("print-query-labelfield-combo", "empty-array-properties");
    }

    [TestMethod]
    public async Task PrintQueryLabelFieldCombo_ToolParameterPersistent()
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create("print-query-labelfield-combo"), new { css = "webgis-tool-parameter webgis-tool-parameter-persistent" }),
            setup: "h => { (" + QueryFeatureObjectSetup + ")(h); h.returns('map.getPersistentToolParameter', 'NAME;NUM;NAME'); }");
        await AssertSnapshotAsync("print-query-labelfield-combo", "persistent");
    }

    [TestMethod]
    public async Task PrintQueryLabelFieldCombo_ItemsAddedAndRemoved()
    {
        await BuildAsync(Create("print-query-labelfield-combo"), setup: QueryFeatureObjectSetup);
        await TriggerAsync(".webgis-multiselect-container ~ select, select.webgis-select", "change", "NAME");
        await TriggerAsync("select.webgis-select", "change", "_label");
        await TriggerAsync("select.webgis-select", "change", "NAME");
        await AssertSnapshotAsync("print-query-labelfield-combo", "items-added");

        await TriggerAsync(".webgis-multiselect-continer-item:first-child .remove", "click");
        await AssertSnapshotAsync("print-query-labelfield-combo", "item-removed");
    }

    #endregion

    #region print-coordinates-labelfield-combo

    private const string CoordinateFeatureSetup = @"h => h.returns('map.queryResultFeatures.firstToolResult',
        { properties: { RW_HW: [1, 2], NUR_RW: [1, null], DREI: [1, 2, 3], TEXT: 'abc', _label: 'y', _x: 'z', HTML: '<b>x</b>' } })";

    [TestMethod]
    public async Task PrintCoordinatesLabelFieldCombo_WithFeature()
    {
        await BuildAsync(Create("print-coordinates-labelfield-combo"), setup: CoordinateFeatureSetup);
        await AssertSnapshotAsync("print-coordinates-labelfield-combo", "with-feature");
    }

    [TestMethod]
    public async Task PrintCoordinatesLabelFieldCombo_PairsOnly()
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create("print-coordinates-labelfield-combo"), new { showCoordinatePairsOnly = true }),
            setup: CoordinateFeatureSetup);
        await AssertSnapshotAsync("print-coordinates-labelfield-combo", "pairs-only");
    }

    [TestMethod]
    public async Task PrintCoordinatesLabelFieldCombo_ItemAdded()
    {
        await BuildAsync(Create("print-coordinates-labelfield-combo"), setup: CoordinateFeatureSetup);
        await TriggerAsync("select.webgis-select", "change", "RW_HW");
        await AssertSnapshotAsync("print-coordinates-labelfield-combo", "item-added");
    }

    #endregion

    #region sketchselect

    private const string SketchesSetup = @"h => h.returns('map.currentToolSketches', [
        { id: 's1', name: 'Skizze 1', storedSketch: { id: 'stored1' } },
        { id: 's2', name: 'Skizze 2', storedSketch: { id: 'stored2' } }
    ])";

    [TestMethod]
    public async Task SketchSelect_WithSketches()
    {
        await BuildAsync(Create("sketchselect"), setup: SketchesSetup);
        await AssertSnapshotAsync("sketchselect", "with-sketches");
    }

    [TestMethod]
    [DataRow(false, "changed-load")]
    [DataRow(true, "changed-load-active-tool")]
    public async Task SketchSelect_ChangedLoad(bool activeTool, string variant)
    {
        await BuildAsync(Create("sketchselect"),
            setup: activeTool
                ? "h => { (" + SketchesSetup + ")(h); h.returns('map.getActiveTool', { id: 'active-tool' }); }"
                : SketchesSetup);
        await ClearCallsAsync();
        await TriggerAsync("select#combo", "change", "s2");
        await AssertSnapshotAsync("sketchselect", variant);
    }

    [TestMethod]
    public async Task SketchSelect_ChangedRemove()
    {
        await BuildAsync(Create("sketchselect"), setup: SketchesSetup);
        await TriggerAsync("select#combo", "change", "s2");
        await ClearCallsAsync();
        await TriggerAsync("select#combo", "change", "");
        await AssertSnapshotAsync("sketchselect", "changed-remove");
    }

    #endregion

    #region select

    private static UISelect SelectWithOptions(string? defaultValue = null) => new()
    {
        id = "combo",
        defaultvalue = defaultValue!,
        options =
        [
            new UISelect.Option { value = "a", label = "Option A" },
            new UISelect.Option { value = "b", label = "Option B" },
            new UISelect.Option { value = "c", label = "Option C" }
        ]
    };

    [TestMethod]
    public async Task Select_Options()
    {
        await BuildAsync(SelectWithOptions());
        await AssertSnapshotAsync("select", "options");
    }

    [TestMethod]
    public async Task Select_OptionsDefaultValue()
    {
        await BuildAsync(SelectWithOptions("b"));
        await AssertSnapshotAsync("select", "options-defaultvalue");
    }

    [TestMethod]
    public async Task Select_AllowAddValues()
    {
        var select = SelectWithOptions();
        select.allow_addvalues = true;

        await BuildAsync(select);
        await AssertSnapshotAsync("select", "allow-addvalues");
    }

    [TestMethod]
    public async Task Select_MapScales()
    {
        await BuildAsync(new UISelect { id = "combo", css = "webgis-map-scales-select webgis-tool-parameter" },
            setup: "h => { h.returns('map.scale', 4000); h.returns('map.scales', [1000, 2504, 5000, 10000, 1000000]); }");
        await AssertSnapshotAsync("select", "map-scales");
    }

    [TestMethod]
    public async Task Select_MapScalesWithOptions()
    {
        // explicit options win over the map scales
        var select = SelectWithOptions();
        select.css = "webgis-map-scales-select";

        await BuildAsync(select, setup: "h => { h.returns('map.scale', 4000); h.returns('map.scales', [1000, 5000]); }");
        await AssertSnapshotAsync("select", "map-scales-with-options");
    }

    [TestMethod]
    [DataRow("clientbutton", null, "changecommand")]
    [DataRow("servertoolcommand", "other-tool", "changecommand-changetool")]
    public async Task Select_ChangeCommand(string changeType, string? changeTool, string variant)
    {
        var select = SelectWithOptions();
        select.changetype = changeType;
        select.changecommand = "do-something";
        select.changetool = changeTool!;

        await BuildAsync(select, options: new { tool = TestTool });
        await AssertSnapshotAsync("select", variant);

        await ClearCallsAsync();
        await TriggerAsync("select#combo", "change", "b");
        await AssertSnapshotAsync("select", variant + "-changed");
    }

    [TestMethod]
    public async Task Select_ChangeCommandUnknown()
    {
        var select = SelectWithOptions();
        select.changecommand = "unknown";

        await BuildAsync(select);
        await TriggerAsync("select#combo", "change", "b");
        await AssertSnapshotAsync("select", "changecommand-unknown");
    }

    [TestMethod]
    public async Task Select_DependencyFieldIds()
    {
        var select = SelectWithOptions();
        select.dependency_field_ids = ["f1", "f2"];
        select.dependency_field_ids_callback_toolid = "callback-tool";

        await BuildAsync(select);
        await AssertSnapshotAsync("select", "dependency-field-ids");
    }

    [TestMethod]
    [DataRow("webgis-print-show-query-markers-select", "print-show-query-markers")]
    [DataRow("webgis-print-show-coordinate-markers-select", "print-show-coordinate-markers")]
    [DataRow("webgis-print-show-chainage-markers-select", "print-show-chainage-markers")]
    public async Task Select_PrintShowMarkers(string css, string variant)
    {
        await BuildAsync(new UISelect
        {
            id = "combo",
            css = css,
            options =
            [
                new UISelect.Option { value = "show", label = "anzeigen" },
                new UISelect.Option { value = "hide", label = "ausblenden" }
            ]
        });
        await ClearCallsAsync();
        await TriggerAsync("select#combo", "change", "hide");
        await TriggerAsync("select#combo", "change", "show");
        await AssertSnapshotAsync("select", variant);
    }

    [TestMethod]
    [DataRow(true, "select2", "pro-behaviour-select2")]
    [DataRow(true, null, "pro-behaviour-off")]
    [DataRow(false, "select2", "pro-behaviour-not-allowed")]
    public async Task Select_ProBehaviour(bool allow, string? usability, string variant)
    {
        var select = SelectWithOptions();
        select.allow_pro_behaviour = allow;

        await BuildAsync(select,
            setup: "h => { webgis.usability.select_pro_behaviour = " + (usability is null ? "undefined" : "'" + usability + "'") + "; h.spy('webgis.require'); }");
        await AssertSnapshotAsync("select", variant);
    }

    [TestMethod]
    public async Task Select_Required()
    {
        var select = SelectWithOptions();
        select.IsRequired = true;

        await BuildAsync(select);
        await AssertSnapshotAsync("select", "required");
    }

    [TestMethod]
    public async Task Select_InMenu_Collapsable()
    {
        await BuildJsonAsync(UIJson.SerializeWith(SelectWithOptions("c"), new { iscollapsable = true, collapsestate = "collapsed", title = "Auswahl" }));
        await AssertSnapshotAsync("select", "collapsable");
    }

    #endregion
}
