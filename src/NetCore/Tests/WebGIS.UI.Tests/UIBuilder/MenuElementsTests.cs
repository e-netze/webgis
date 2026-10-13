using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;

using WebGIS.UI.Tests.Infrastructure;

namespace WebGIS.UI.Tests.UIBuilder;

/// <summary>
/// Characterization tests for the menu elements (package 3: menu, menuitem incl. callbacks).
/// </summary>
[TestClass]
public class MenuElementsTests : UIBuilderPageTest
{
    private static readonly object TestTool = new { id = "test-tool", type = "clientbutton" };

    private const string SpyToolRequest = "h => h.spy('webgis.tools.sendToolRequest')";

    // object is no api server tool => no callback is created by the constructor
    private static UIMenuItem Item(string id, string text = "Eintrag") =>
        new(new object(), null!) { id = id, text = text };

    private static object Callback(string type, string command = "cmd") =>
        new { tool = new { id = "callback-tool", tooltype = "servertool" }, type, command };

    #region menu

    [TestMethod]
    public async Task Menu_Default()
    {
        await BuildAsync(new UIMenu());
        await AssertSnapshotAsync("menu", "default");
    }

    [TestMethod]
    public async Task Menu_HeaderCollapsable()
    {
        await BuildAsync(new UIMenu { header = "Überschrift", collapsable = true });
        await AssertSnapshotAsync("menu", "header-collapsable");
    }

    [TestMethod]
    public async Task Menu_IdCssStyleIgnored()
    {
        // menu doesn't use elementProperties => id/css/style/visible are currently ignored
        await BuildAsync(new UIMenu { id = "menu", css = "custom-css", style = "width:50%", visible = false }, options: new { tool = TestTool });
        await AssertSnapshotAsync("menu", "id-css-style-ignored");
    }

    [TestMethod]
    public async Task Menu_WithItems()
    {
        await BuildAsync(new UIMenu
        {
            header = "Menü",
            elements = [Item("i1", "Erster"), Item("i2", "Zweiter")]
        });
        await AssertSnapshotAsync("menu", "with-items");
    }

    [TestMethod]
    public async Task Menu_IsCollapsableWrapper()
    {
        await BuildJsonAsync(UIJson.SerializeWith(
            new UIMenu { elements = [Item("i1")] },
            new { iscollapsable = true, collapsestate = "collapsed", title = "Abschnitt" }));
        await AssertSnapshotAsync("menu", "iscollapsable");
    }

    [TestMethod]
    public async Task Menu_Collapsable_ItemClicks()
    {
        var item1 = Item("i1", "Erster");
        item1.callback = Callback("servertoolcommand");
        var item2 = Item("i2", "Zweiter");
        item2.callback = Callback("servertoolcommand");

        await BuildAsync(new UIMenu { collapsable = true, elements = [item1, item2] });

        // not collapsed yet => collapses, marks item, executes callback
        await ClearCallsAsync();
        await TriggerAsync("#i2", "click");
        await AssertSnapshotAsync("menu", "collapsable-clicked-1");

        // collapsed => expands, fires event, no callback
        await ClearCallsAsync();
        await TriggerAsync("#i1", "click");
        await AssertSnapshotAsync("menu", "collapsable-clicked-2");
    }

    [TestMethod]
    public async Task Menu_TargetModalDialog_ItemClickClosesModal()
    {
        var item = Item("i1");
        item.callback = Callback("servertoolcommand");

        await BuildJsonAsync(UIJson.SerializeWith(new UIMenu { elements = [item] }, new { target = "modaldialog" }));
        await AssertSnapshotAsync("menu", "target-modaldialog");

        await ClearCallsAsync();
        await TriggerAsync("#i1", "click");
        await AssertSnapshotAsync("menu", "target-modaldialog-clicked");
    }

    #endregion

    #region menuitem - rendering

    [TestMethod]
    public async Task MenuItem_Default()
    {
        await BuildAsync(new UIMenuItem(new object(), null!) { text = "Eintrag" });
        await AssertSnapshotAsync("menuitem", "default");
    }

    [TestMethod]
    public async Task MenuItem_WithoutText()
    {
        await BuildAsync(new UIMenuItem(new object(), null!));
        await AssertSnapshotAsync("menuitem", "without-text");
    }

    [TestMethod]
    public async Task MenuItem_HtmlText()
    {
        await BuildAsync(Item("i1", "<b>Fett</b> &amp; normal"));
        await AssertSnapshotAsync("menuitem", "html-text");
    }

    [TestMethod]
    public async Task MenuItem_IdCssStyle()
    {
        var item = Item("i1");
        item.css = "custom-css another";
        item.style = "color:red";
        await BuildAsync(item);
        await AssertSnapshotAsync("menuitem", "id-css-style");
    }

    [TestMethod]
    public async Task MenuItem_Invisible()
    {
        var item = Item("i1");
        item.visible = false;
        await BuildAsync(item);
        await AssertSnapshotAsync("menuitem", "invisible");
    }

    [TestMethod]
    public async Task MenuItem_VisDependency()
    {
        await BuildJsonAsync(UIJson.SerializeWith(Item("i1"), new { vis_dependency = "webgis-dependencies webgis-dependency-hasselection" }));
        await AssertSnapshotAsync("menuitem", "vis-dependency");
    }

    [TestMethod]
    public async Task MenuItem_WithTool()
    {
        // menuitems call elementProperties(element) without options => currently no data-tool-id
        await BuildAsync(Item("i1"), options: new { tool = TestTool });
        await AssertSnapshotAsync("menuitem", "with-tool");
    }

    [TestMethod]
    public async Task MenuItem_StringValue()
    {
        var item = Item("i1");
        item.value = "Wert 'x'";
        await BuildAsync(item);
        await AssertSnapshotAsync("menuitem", "string-value");
    }

    [TestMethod]
    public async Task MenuItem_Text2Subtext()
    {
        var item = Item("i1");
        item.text2 = "Text <2>";
        item.subtext = "Untertext";
        await BuildAsync(item);
        await AssertSnapshotAsync("menuitem", "text2-subtext");
    }

    [TestMethod]
    public async Task MenuItem_Icon()
    {
        var item = Item("i1");
        item.icon = "ui/edit-26.png";
        await BuildAsync(item);
        await AssertSnapshotAsync("menuitem", "icon");
    }

    [TestMethod]
    public async Task MenuItem_IconLarge()
    {
        var item = Item("i1");
        item.icon = "ui/edit-26.png";
        item.icon_large = "ui/large.png";
        await BuildAsync(item);
        await AssertSnapshotAsync("menuitem", "icon-large");
    }

    [TestMethod]
    public async Task MenuItem_IconDataUrl()
    {
        var item = Item("i1");
        item.icon = "data:image/png;base64,iVBORw0KGgo=";
        await BuildAsync(item);
        await AssertSnapshotAsync("menuitem", "icon-data-url");
    }

    [TestMethod]
    public async Task MenuItem_IsCollapsableWrapper()
    {
        await BuildJsonAsync(UIJson.SerializeWith(Item("i1"), new { iscollapsable = true, collapsestate = "expanded", title = "Abschnitt" }));
        await AssertSnapshotAsync("menuitem", "iscollapsable");
    }

    #endregion

    #region menuitem - callbacks

    [TestMethod]
    public async Task MenuItem_WithoutCallback_Click()
    {
        await BuildAsync(Item("i1"));
        await ClearCallsAsync();
        await TriggerAsync("#i1", "click");
        await AssertSnapshotAsync("menuitem", "without-callback-clicked");
    }

    [TestMethod]
    [DataRow("servertoolcommand")]
    [DataRow("servertoolcommand_ext")]
    public async Task MenuItem_ServerToolCommand_Click(string type)
    {
        var item = Item("i1");
        item.value = "v1";
        item.callback = Callback(type, "select");

        await BuildAsync(item, setup: SpyToolRequest);
        await AssertSnapshotAsync("menuitem", type);

        await ClearCallsAsync();
        await TriggerAsync("#i1", "click");
        await AssertSnapshotAsync("menuitem", type + "-clicked");
    }

    [TestMethod]
    public async Task MenuItem_ToolEvent_OriginalTool_Click()
    {
        var item = Item("i1");
        item.value = "v1";
        item.callback = Callback("toolevent");

        await BuildAsync(item,
            setup: "h => { h.spy('webgis.tools.sendToolRequest'); h.returns('map.getTool', { id: 'callback-tool', name: 'Original' }); }",
            options: new { @event = new { eventtype = "click", x = 1 } });
        await AssertSnapshotAsync("menuitem", "toolevent-original-tool");

        await ClearCallsAsync();
        await TriggerAsync("#i1", "click");
        await AssertSnapshotAsync("menuitem", "toolevent-original-tool-clicked");
    }

    [TestMethod]
    public async Task MenuItem_ToolEvent_UnknownTool_Click()
    {
        var item = Item("i1");
        item.callback = Callback("toolevent");

        await BuildAsync(item, setup: "h => { h.spy('webgis.tools.sendToolRequest'); h.returns('map.getTool', null); }");
        await ClearCallsAsync();
        await TriggerAsync("#i1", "click");
        await AssertSnapshotAsync("menuitem", "toolevent-unknown-tool-clicked");
    }

    [TestMethod]
    public async Task MenuItem_ItemCommandAttribute_Click()
    {
        var item = Item("i1");
        item.callback = Callback("servertoolcommand");

        await BuildAsync(item);
        await Page.EvaluateAsync("() => $('#i1').attr('data-item-command', 'custom-command')");
        await ClearCallsAsync();
        await TriggerAsync("#i1", "click");
        await AssertSnapshotAsync("menuitem", "item-command-clicked");
    }

    [TestMethod]
    public async Task MenuItem_Removable_RemoveClick()
    {
        var item = Item("i1");
        item.removable = true;
        item.callback = Callback("servertoolcommand");

        await BuildAsync(new UIMenu { elements = [item, Item("i2")] });
        await AssertSnapshotAsync("menuitem", "removable");

        await ClearCallsAsync();
        await TriggerAsync("#i1 .remove-element", "click");
        await AssertSnapshotAsync("menuitem", "removable-removed");
    }

    #endregion

    #region menuitem - checkbox, highlight

    private const string ServiceSetup = """
        h => {
            h.returns('map.getService', h.fake('service'));
            h.returns('service.getQuery', { associatedlayers: [{ id: 'l1' }, { id: 'l2' }] });
            h.returns('service.checkLayerVisibility', __VISIBILITY__);
        }
        """;

    private static IUIElement CheckableItem(string value) =>
        new UIMenuItemCheckable(new object(), null!) { id = "i1", text = "Abfrage", value = value };

    [TestMethod]
    [DataRow(0, "hidden-layers")]
    [DataRow(1, "visible-layers")]
    public async Task MenuItem_Checkbox(int visibility, string variant)
    {
        await BuildAsync(CheckableItem("service1:query1"), setup: ServiceSetup.Replace("__VISIBILITY__", visibility.ToString()));
        await AssertSnapshotAsync("menuitem", "checkbox-" + variant);

        await ClearCallsAsync();
        await TriggerAsync("#i1 .webgis-checkbox", "click");
        await AssertSnapshotAsync("menuitem", "checkbox-" + variant + "-clicked");
    }

    [TestMethod]
    [DataRow("no-service", "service1:query1", "h => h.returns('map.getService', null)")]
    [DataRow("no-query", "service1:query1", "h => { h.returns('map.getService', h.fake('service')); h.returns('service.getQuery', null); }")]
    [DataRow("no-layers", "service1:query1", "h => { h.returns('map.getService', h.fake('service')); h.returns('service.getQuery', { associatedlayers: [] }); }")]
    [DataRow("no-query-value", "service1", "h => h.returns('map.getService', null)")]
    public async Task MenuItem_Checkbox_NotShown(string variant, string value, string setup)
    {
        await BuildAsync(CheckableItem(value), setup: setup);
        await AssertSnapshotAsync("menuitem", "checkbox-" + variant);
    }

    private const string HighlightFeature = """{"features":[{"geometry":{"type":"Point","coordinates":[15.4,47.1]}}]}""";

    [TestMethod]
    public async Task MenuItem_HighlightFeature_HoverAndClick()
    {
        var item = Item("i1");
        item.highlight_feature = HighlightFeature;
        item.callback = Callback("servertoolcommand");

        await BuildAsync(item);
        await AssertSnapshotAsync("menuitem", "highlight-feature");

        await ClearCallsAsync();
        await TriggerAsync("#i1", "mouseenter mouseleave click");
        await AssertSnapshotAsync("menuitem", "highlight-feature-hover-clicked");
    }

    [TestMethod]
    public async Task MenuItem_HighlightFeature_Object()
    {
        // already parsed object (hand-written JSON) is used as is
        await BuildJsonAsync("""[{"type":"menuitem","id":"i1","text":"Eintrag","highlight_feature":""" + HighlightFeature + "}]");
        await ClearCallsAsync();
        await TriggerAsync("#i1", "mouseenter");
        await AssertSnapshotAsync("menuitem", "highlight-feature-object-hover");
    }

    #endregion
}
