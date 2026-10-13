using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;

using WebGIS.UI.Tests.Infrastructure;

namespace WebGIS.UI.Tests.UIBuilder;

/// <summary>
/// Characterization tests for the button elements (package 2: button, imagebutton, buttongroup, undobutton).
/// </summary>
[TestClass]
public class ButtonElementsTests : UIBuilderPageTest
{
    private static readonly object TestTool = new { id = "test-tool", type = "clientbutton" };

    #region Common variants (button, imagebutton, undobutton)

    private static UIElement Create(string type) => type switch
    {
        "button" => new UIButton(UIButton.UIButtonType.servertoolcommand, "save") { text = "Speichern" },
        "imagebutton" => new UIImageButton("ui/edit-26.png", UIButton.UIButtonType.servertoolcommand, "edit") { text = "Bearbeiten" },
        "undobutton" => new UIToolUndoButton(typeof(ButtonElementsTests), "Rückgängig"),
        _ => throw new ArgumentException(type)
    };

    [TestMethod]
    [DataRow("button")]
    [DataRow("imagebutton")]
    [DataRow("undobutton")]
    public async Task Button_Default(string type)
    {
        await BuildAsync(Create(type));
        await AssertSnapshotAsync(type, "default");
    }

    [TestMethod]
    [DataRow("button")]
    [DataRow("imagebutton")]
    [DataRow("undobutton")]
    public async Task Button_IdCssStyle(string type)
    {
        var element = Create(type);
        element.id = "my-button";
        element.css = "custom-css another";
        element.style = "width:100px";

        await BuildAsync(element);
        await AssertSnapshotAsync(type, "id-css-style");
    }

    [TestMethod]
    [DataRow("button")]
    [DataRow("imagebutton")]
    [DataRow("undobutton")]
    public async Task Button_Invisible(string type)
    {
        var element = Create(type);
        element.visible = false;

        await BuildAsync(element);
        await AssertSnapshotAsync(type, "invisible");
    }

    [TestMethod]
    [DataRow("button")]
    [DataRow("imagebutton")]
    [DataRow("undobutton")]
    public async Task Button_VisDependency(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { vis_dependency = "webgis-dependencies webgis-dependency-hasselection" }));
        await AssertSnapshotAsync(type, "vis-dependency");
    }

    [TestMethod]
    [DataRow("button")]
    [DataRow("imagebutton")]
    [DataRow("undobutton")]
    public async Task Button_StringValue(string type)
    {
        var element = Create(type);
        element.value = "Wert 'x'";

        await BuildAsync(element);
        await AssertSnapshotAsync(type, "string-value");
    }

    [TestMethod]
    [DataRow("button")]
    [DataRow("imagebutton")]
    [DataRow("undobutton")]
    public async Task Button_WithTool(string type)
    {
        // buttons call elementProperties(element) without options => currently no data-tool-id
        await BuildAsync(Create(type), options: new { tool = TestTool });
        await AssertSnapshotAsync(type, "with-tool");
    }

    [TestMethod]
    [DataRow("button")]
    [DataRow("imagebutton")]
    [DataRow("undobutton")]
    public async Task Button_Collapsable(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { iscollapsable = true, collapsestate = "expanded", title = "Abschnitt" }));
        await AssertSnapshotAsync(type, "collapsable");
    }

    [TestMethod]
    [DataRow("button")]
    [DataRow("imagebutton")]
    [DataRow("undobutton")]
    public async Task Button_Target(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { target = "modaldialog" }));
        await AssertSnapshotAsync(type, "target-modaldialog");
    }

    #endregion

    #region button

    [TestMethod]
    public async Task UIButton_WithoutText()
    {
        // text is serialized as null => old code renders "null" via string concatenation
        await BuildAsync(new UIButton(UIButton.UIButtonType.clientbutton, "zoomin"));
        await AssertSnapshotAsync("button", "without-text");
    }

    [TestMethod]
    public async Task UIButton_HtmlText()
    {
        await BuildAsync(new UIButton(UIButton.UIButtonType.clientbutton, "zoomin") { text = "<b>Fett</b> &amp; normal" });
        await AssertSnapshotAsync("button", "html-text");
    }

    [TestMethod]
    public async Task UIButton_Icon()
    {
        await BuildAsync(new UIButton(UIButton.UIButtonType.clientbutton, "zoomin") { text = "Zoom", icon = "ui/zoom-in-26.png" });
        await AssertSnapshotAsync("button", "icon");
    }

    [TestMethod]
    public async Task UIButton_CtrlShortcut_InText()
    {
        await BuildAsync(new UIButton(UIButton.UIButtonType.clientbutton, "save") { text = "SPEICHERN Sie", ctrl_shortcut = "s" });
        await AssertSnapshotAsync("button", "ctrl-shortcut-in-text");
    }

    [TestMethod]
    public async Task UIButton_CtrlShortcut_NotInText()
    {
        await BuildAsync(new UIButton(UIButton.UIButtonType.clientbutton, "save") { text = "Speichern", ctrl_shortcut = "x" });
        await AssertSnapshotAsync("button", "ctrl-shortcut-not-in-text");
    }

    [TestMethod]
    public async Task UIButton_CommandArgument()
    {
        await BuildAsync(new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "export")
        {
            id = "btn",
            text = "Export",
            buttoncommand_argument = "csv",
            value = "v1"
        });
        await AssertSnapshotAsync("button", "command-argument");

        await ClearCallsAsync();
        await TriggerAsync("#btn", "click");
        await AssertSnapshotAsync("button", "command-argument-clicked");
    }

    [TestMethod]
    public async Task UIButton_ObjectValue_Click()
    {
        await BuildJsonAsync(UIJson.SerializeWith(
            new UIButton(UIButton.UIButtonType.clientbutton, "select") { id = "btn", text = "Wählen" },
            new { value = new { id = 7, name = "Objekt" } }));
        await ClearCallsAsync();
        await TriggerAsync("#btn", "click");
        await AssertSnapshotAsync("button", "object-value-clicked");
    }

    [TestMethod]
    public async Task UIButton_WithTool_Click()
    {
        await BuildAsync(new UIButton(UIButton.UIButtonType.serverbutton, "run") { id = "btn", text = "Los" },
            options: new { tool = TestTool });
        await ClearCallsAsync();
        await TriggerAsync("#btn", "click");
        await AssertSnapshotAsync("button", "with-tool-clicked");
    }

    [TestMethod]
    [DataRow("setgraphicsdistancecircleradius", "webgis-graphics-distance_circle-radius")]
    [DataRow("setgraphicshectolineinterval", "webgis-graphics-hectoline-interval")]
    public async Task UIButton_GraphicsCommand_ReadsValueFromOptionsContainer(string command, string inputCss)
    {
        var container = new UIDiv
        {
            css = "webgis-ui-optionscontainer",
            elements =
            [
                new UIInputText { css = inputCss, value = "12.5" },
                new UIButton(UIButton.UIButtonType.clientbutton, command) { id = "btn", text = "Setzen", value = "initial" }
            ]
        };

        await BuildAsync(container);
        await ClearCallsAsync();
        await TriggerAsync("#btn", "click");
        await AssertSnapshotAsync("button", command + "-clicked");
    }

    [TestMethod]
    public async Task UIButton_GraphicsCommand_WithoutInput()
    {
        await BuildAsync(new UIButton(UIButton.UIButtonType.clientbutton, "setgraphicsdistancecircleradius") { id = "btn", text = "Setzen", value = "initial" });
        await ClearCallsAsync();
        await TriggerAsync("#btn", "click");
        await AssertSnapshotAsync("button", "setgraphicsdistancecircleradius-without-input-clicked");
    }

    private static UIDiv ValidationContainer(string inputValue) => new()
    {
        css = "webgis-validation-container",
        elements =
        [
            new UIInputText { id = "required-input", value = inputValue, IsRequired = true, MinLength = 3 },
            new UIButton(UIButton.UIButtonType.servertoolcommand, "save") { id = "btn", text = "Speichern", css = "uibutton-validate-input" }
        ]
    };

    [TestMethod]
    public async Task UIButton_ValidateInput_NotValid()
    {
        await BuildAsync(ValidationContainer("ab"), setup: "h => h.spy('webgis.alert')");
        await ClearCallsAsync();
        await TriggerAsync("#btn", "click");
        await AssertSnapshotAsync("button", "validate-input-not-valid-clicked");
    }

    [TestMethod]
    public async Task UIButton_ValidateInput_Valid()
    {
        await BuildAsync(ValidationContainer("abcd"), setup: "h => h.spy('webgis.alert')");
        await ClearCallsAsync();
        await TriggerAsync("#btn", "click");
        await AssertSnapshotAsync("button", "validate-input-valid-clicked");
    }

    #endregion

    #region imagebutton

    [TestMethod]
    public async Task ImageButton_WithoutSrcAndText()
    {
        await BuildJsonAsync(UIJson.SerializeWith(
            new UIImageButton("ui/edit-26.png", UIButton.UIButtonType.clientbutton, "edit"),
            new { src = (string?)null }));
        await AssertSnapshotAsync("imagebutton", "without-src-text");
    }

    [TestMethod]
    public async Task ImageButton_Click_Standalone()
    {
        await BuildAsync(new UIImageButton("ui/edit-26.png", UIButton.UIButtonType.servertoolcommand, "edit") { id = "img", text = "Bearbeiten", value = "v1" });
        await ClearCallsAsync();
        await TriggerAsync("#img", "click");
        await AssertSnapshotAsync("imagebutton", "standalone-clicked");
    }

    private static UIOptionContainer OptionsContainer(bool collapsable)
    {
        var container = new UIOptionContainer
        {
            id = "options",
            title = "Optionen",
            elements =
            [
                new UIImageButton("ui/a-26.png", UIButton.UIButtonType.clientbutton, "option") { id = "opt-a", text = "A", value = "a" },
                new UIImageButton("ui/b-26.png", UIButton.UIButtonType.clientbutton, "option") { id = "opt-b", text = "B", value = "b" }
            ]
        };
        if (collapsable)
        {
            container.CollapseState = UICollapsableElement.CollapseStatus.Expanded;
        }
        return container;
    }

    [TestMethod]
    public async Task ImageButton_InOptionsContainer()
    {
        // the options container clicks its first option after building
        await BuildAsync(OptionsContainer(false));
        await AssertSnapshotAsync("imagebutton", "optionscontainer");

        await ClearCallsAsync();
        await TriggerAsync("#opt-b", "click");
        await AssertSnapshotAsync("imagebutton", "optionscontainer-clicked");
    }

    [TestMethod]
    public async Task ImageButton_InCollapsableOptionsContainer()
    {
        // selected option is cloned into the collapsable title
        await BuildAsync(OptionsContainer(true));
        await AssertSnapshotAsync("imagebutton", "optionscontainer-collapsable");

        await ClearCallsAsync();
        await TriggerAsync("#opt-b", "click");
        await AssertSnapshotAsync("imagebutton", "optionscontainer-collapsable-clicked");
    }

    #endregion

    #region buttongroup

    private static UIButton GroupButton(string id) =>
        new(UIButton.UIButtonType.clientbutton, "cmd-" + id) { id = id, text = id.ToUpperInvariant() };

    [TestMethod]
    public async Task ButtonGroup_Empty()
    {
        await BuildAsync(new UIButtonGroup());
        await AssertSnapshotAsync("buttongroup", "empty");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public async Task ButtonGroup_Buttons(int count)
    {
        var group = new UIButtonGroup
        {
            elements = Enumerable.Range(1, count).Select(i => (IUIElement)GroupButton("b" + i)).ToArray()
        };

        await BuildAsync(group);
        await AssertSnapshotAsync("buttongroup", "buttons-" + count);
    }

    [TestMethod]
    public async Task ButtonGroup_IdCssStyleIgnored()
    {
        // buttongroup doesn't use elementProperties => id/css/style/visible are currently ignored
        await BuildAsync(new UIButtonGroup
        {
            id = "group",
            css = "custom-css",
            style = "width:50%",
            visible = false,
            elements = [GroupButton("b1")]
        });
        await AssertSnapshotAsync("buttongroup", "id-css-style-ignored");
    }

    [TestMethod]
    public async Task ButtonGroup_MixedChildren()
    {
        // only .uibutton children get a width
        await BuildAsync(new UIButtonGroup
        {
            elements =
            [
                GroupButton("b1"),
                new UIImageButton("ui/edit-26.png", UIButton.UIButtonType.clientbutton, "edit") { id = "img", text = "Bild" },
                GroupButton("b2")
            ]
        });
        await AssertSnapshotAsync("buttongroup", "mixed-children");
    }

    [TestMethod]
    public async Task ButtonGroup_WithToolAndTarget()
    {
        await BuildJsonAsync(UIJson.SerializeWith(
            new UIButtonGroup { elements = [GroupButton("b1"), GroupButton("b2")] },
            new { target = "modaldialog" }),
            options: new { tool = TestTool });
        await AssertSnapshotAsync("buttongroup", "with-tool-target");
    }

    [TestMethod]
    public async Task ButtonGroup_Collapsable()
    {
        await BuildJsonAsync(UIJson.SerializeWith(
            new UIButtonGroup { elements = [GroupButton("b1"), GroupButton("b2")] },
            new { iscollapsable = true, collapsestate = "collapsed", title = "Gruppe" }));
        await AssertSnapshotAsync("buttongroup", "collapsable");
    }

    #endregion

    #region undobutton

    [TestMethod]
    public async Task UndoButton_Image()
    {
        // UIImageToolUndoButton assigns text = text => null
        await BuildAsync(new UIImageToolUndoButton(typeof(ButtonElementsTests), "undo.png"));
        await AssertSnapshotAsync("undobutton", "image");
    }

    [TestMethod]
    public async Task UndoButton_ImageWithText()
    {
        await BuildAsync(new UIImageToolUndoButton(typeof(ButtonElementsTests), "undo.png") { text = "Zurück" });
        await AssertSnapshotAsync("undobutton", "image-text");
    }

    [TestMethod]
    [DataRow("no-tool", "h => h.returns('map.getTool', null)")]
    [DataRow("no-undos", "h => h.returns('map.getTool', { id: 'undo-tool', undos: [] })")]
    public async Task UndoButton_Click_NothingToUndo(string variant, string setup)
    {
        await BuildAsync(new UIToolUndoButton(typeof(ButtonElementsTests), "Rückgängig") { id = "undo" }, setup: setup);
        await ClearCallsAsync();
        await TriggerAsync("#undo", "click");
        await AssertSnapshotAsync("undobutton", "clicked-" + variant);
    }

    private const string UndoToolSetup = """
        h => {
            const now = Date.now();
            h.returns('map.getTool', {
                id: 'undo-tool',
                undos: [
                    { title: 'Erste Änderung', preview: { type: 'p1' } },
                    { title: 'Vor Sekunden', preview: { type: 'p2' }, _ticks: now - 20 * 1000 },
                    { title: 'Vor Minuten', preview: { type: 'p3' }, _ticks: now - 5 * 60 * 1000 },
                    { title: 'Vor Stunden', preview: { type: 'p4' }, _ticks: now - 2 * 3600 * 1000 }
                ]
            });
            h.spy('webgis.tools.onToolUndoClick');
        }
        """;

    [TestMethod]
    public async Task UndoButton_Click_OpensUndoList()
    {
        await BuildAsync(new UIToolUndoButton(typeof(ButtonElementsTests), "Rückgängig") { id = "undo" }, setup: UndoToolSetup);
        await ClearCallsAsync();
        await TriggerAsync("#undo", "click");
        await AssertSnapshotAsync("undobutton", "clicked-undo-list");
    }

    [TestMethod]
    public async Task UndoButton_UndoItem_HoverAndClick()
    {
        await BuildAsync(new UIToolUndoButton(typeof(ButtonElementsTests), "Rückgängig") { id = "undo" }, setup: UndoToolSetup);
        await TriggerAsync("#undo", "click");
        await ClearCallsAsync();

        await TriggerAsync(".webgis-ui-undo-item:last", "mouseenter mouseleave click");
        await AssertSnapshotAsync("undobutton", "undo-item-hover-clicked");
    }

    #endregion
}
