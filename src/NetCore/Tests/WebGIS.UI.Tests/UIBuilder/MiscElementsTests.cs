using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

using WebGIS.UI.Tests.Infrastructure;

namespace WebGIS.UI.Tests.UIBuilder;

/// <summary>
/// Characterization tests for the miscellaneous elements
/// (package 7: literal, literal-bold, image, input-bbox, input-size, condition_div, opacity-control,
/// ui-liveshare, static-overlay-control, image-selector, graphics-info-container, graphics-info-stage,
/// sketch-info-container, sharelink-buttons).
/// </summary>
[TestClass]
public class MiscElementsTests : UIBuilderPageTest
{
    private static readonly object TestTool = new { id = "test-tool", type = "clientbutton" };

    private const string OpacityServiceSetup = @"h => h.returns('map.getService', {
        id: 's1', name: 'Dienst 1', opacity: 0.5,
        getOpacity: function () { return this.opacity; },
        setOpacity: h.fake('service.setOpacity')
    })";

    // deterministic share options (the real ones depend on the browser)
    private const string ShareOptionsSetup = @"h => { webgis.shareOptions = [
        { name: 'Mail', img: function () { return 'mail.png'; }, available: function () { return true; }, share: h.fake('shareOption.mail.share') },
        { name: 'Nicht verfuegbar', img: function () { return 'na.png'; }, available: function () { return false; }, share: h.fake('shareOption.na.share') }
    ]; }";

    private static IUIElement Create(string type) => type switch
    {
        "literal" => new UILiteral { id = "elem", literal = "Text <b>fett</b>\nZeile 2" },
        "literal-bold" => new UILiteralBold { id = "elem", literal = "Text <i>kursiv</i>" },
        "image" => new UIImage("content/api/img/test.png") { id = "elem" },
        "input-bbox" => new UIBoundBoxInput { id = "elem" },
        "input-size" => new UISizeInput { id = "elem" },
        "condition_div" => new UIConditionDiv
        {
            id = "elem",
            ConditionType = UIConditionDiv.ConditionTypes.LayersVisible,
            ConditionArguments = ["s1:l1", "s1:l2"],
            ConditionResult = true,
            ContitionElementId = "other"
        },
        "opacity-control" => new UIOpacityControl("s1") { id = "elem" },
        "ui-liveshare" => new UILiveShareElement { id = "elem" },
        "static-overlay-control" => new UIStaticOverlayControl { id = "elem" },
        "image-selector" => new UIImageSelector { id = "elem", ImageUrls = ["a.png", "b.png"], ImageLabels = ["A", "B"] },
        "graphics-info-container" => new UIGraphicsInfoContainer { id = "elem" },
        "graphics-info-stage" => new UIGraphicsInfoStage { id = "elem" },
        "sketch-info-container" => new UISketchInfoContainer { id = "elem" },
        "sharelink-buttons" => new UIShareLinkButtons("https://example.org/share") { id = "elem", subject = "Betreff", qr_base64 = "qr" },
        _ => throw new ArgumentException(type)
    };

    private const string GraphicsSetup = @"h => {
        h.returns('map.graphics.getElements', []);
        h.returns('map.graphics.hasSelectedElements', false);
        h.returns('map.graphics.selectedElements', []);
        h.returns('map.graphics.hasStagedElement', false);
        h.returns('map.graphics.getTool', 'point');
        h.returns('map.graphics.getToolDescription', 'Punkt setzen');
    }";

    private static string? SetupFor(string type) => type switch
    {
        "graphics-info-container" => GraphicsSetup,
        "opacity-control" => OpacityServiceSetup,
        "sharelink-buttons" => ShareOptionsSetup,
        // fallback: render inline (no overlay container)
        "sketch-info-container" => "h => h.returns('map.sketchInfoOverlayContainer', null)",
        _ => null
    };

    // static-overlay-control loads Sortable.js asynchronously (webgis.require)
    private Task WaitForAsyncPluginsAsync(string type) => type == "static-overlay-control"
        ? WaitForAsync("() => typeof Sortable === 'function'")
        : Task.CompletedTask;

    #region common variants

    [TestMethod]
    [DataRow("literal")]
    [DataRow("literal-bold")]
    [DataRow("image")]
    [DataRow("input-bbox")]
    [DataRow("input-size")]
    [DataRow("condition_div")]
    [DataRow("opacity-control")]
    [DataRow("ui-liveshare")]
    [DataRow("static-overlay-control")]
    [DataRow("image-selector")]
    [DataRow("graphics-info-container")]
    [DataRow("graphics-info-stage")]
    [DataRow("sketch-info-container")]
    [DataRow("sharelink-buttons")]
    public async Task Misc_Default(string type)
    {
        await BuildAsync(Create(type), setup: SetupFor(type));
        await WaitForAsyncPluginsAsync(type);
        await AssertSnapshotAsync(type, "default");
    }

    [TestMethod]
    [DataRow("literal")]
    [DataRow("literal-bold")]
    [DataRow("image")]
    [DataRow("input-bbox")]
    [DataRow("input-size")]
    [DataRow("condition_div")]
    [DataRow("opacity-control")]
    [DataRow("ui-liveshare")]
    [DataRow("static-overlay-control")]
    [DataRow("image-selector")]
    [DataRow("graphics-info-container")]
    [DataRow("graphics-info-stage")]
    [DataRow("sketch-info-container")]
    [DataRow("sharelink-buttons")]
    public async Task Misc_IdCssStyle(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { css = "custom-css", style = "width:100px", vis_dependency = "webgis-dependencies webgis-dependency-hasselection" }), setup: SetupFor(type));
        await WaitForAsyncPluginsAsync(type);
        await AssertSnapshotAsync(type, "id-css-style");
    }

    [TestMethod]
    [DataRow("literal")]
    [DataRow("image")]
    [DataRow("input-bbox")]
    [DataRow("input-size")]
    [DataRow("condition_div")]
    [DataRow("opacity-control")]
    [DataRow("ui-liveshare")]
    [DataRow("static-overlay-control")]
    [DataRow("image-selector")]
    [DataRow("graphics-info-container")]
    [DataRow("sketch-info-container")]
    [DataRow("sharelink-buttons")]
    public async Task Misc_Invisible(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { visible = false }), setup: SetupFor(type));
        await WaitForAsyncPluginsAsync(type);
        await AssertSnapshotAsync(type, "invisible");
    }

    [TestMethod]
    [DataRow("literal")]
    [DataRow("image")]
    [DataRow("input-bbox")]
    [DataRow("input-size")]
    [DataRow("condition_div")]
    [DataRow("opacity-control")]
    [DataRow("ui-liveshare")]
    [DataRow("static-overlay-control")]
    [DataRow("image-selector")]
    [DataRow("graphics-info-container")]
    [DataRow("graphics-info-stage")]
    [DataRow("sketch-info-container")]
    [DataRow("sharelink-buttons")]
    public async Task Misc_WithTool(string type)
    {
        await BuildAsync(Create(type), setup: SetupFor(type), options: new { tool = TestTool });
        await WaitForAsyncPluginsAsync(type);
        await AssertSnapshotAsync(type, "with-tool");
    }

    [TestMethod]
    [DataRow("image")]
    [DataRow("input-bbox")]
    [DataRow("input-size")]
    [DataRow("condition_div")]
    [DataRow("image-selector")]
    public async Task Misc_StringValue(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { value = "v'1" }), setup: SetupFor(type));
        await WaitForAsyncPluginsAsync(type);
        await AssertSnapshotAsync(type, "string-value");
    }

    [TestMethod]
    [DataRow("input-bbox")]
    [DataRow("input-size")]
    [DataRow("image-selector")]
    public async Task Misc_Persistent(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { css = "webgis-tool-parameter-persistent" }), setup: SetupFor(type));
        await WaitForAsyncPluginsAsync(type);
        await AssertSnapshotAsync(type, "persistent");
    }

    #endregion

    #region literal / literal-bold

    [TestMethod]
    [DataRow("literal")]
    [DataRow("literal-bold")]
    public async Task Literal_AfterSiblingWithEvents(string type)
    {
        // the literal is appended via $parent.html(...) => previous siblings are re-created (jQuery events/data get lost)
        await BuildAsync([new UIButton(UIButton.UIButtonType.clientbutton, "cmd") { id = "btn", text = "Button" }, Create(type)]);
        await AssertSnapshotAsync(type, "after-sibling-with-events");
    }

    [TestMethod]
    public async Task Literal_Markdown()
    {
        await BuildAsync(new UILiteral { literal = "**fett** und _kursiv_", as_markdown = true });
        await AssertSnapshotAsync("literal", "markdown");
    }

    [TestMethod]
    public async Task Literal_WithChildren()
    {
        // a literal never creates an element => no children, no events
        await BuildJsonAsync(UIJson.SerializeWith(Create("literal"), new { onchange = "cmd", elements = new[] { new { type = "div", id = "child" } } }));
        await AssertSnapshotAsync("literal", "with-children");
    }

    #endregion

    #region image

    [TestMethod]
    public async Task Image_Base64()
    {
        await BuildAsync(new UIImage("iVBORw0KGgo=", isBase64: true) { id = "elem" });
        await AssertSnapshotAsync("image", "base64");
    }

    #endregion

    #region input-bbox / input-size

    [TestMethod]
    [DataRow("input-bbox")]
    [DataRow("input-size")]
    public async Task BboxSize_Readonly(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { @readonly = true }));
        await AssertSnapshotAsync(type, "readonly");
    }

    [TestMethod]
    public async Task Bbox_InputChanged()
    {
        await BuildAsync(Create("input-bbox"));
        await Page.EvaluateAsync("() => { $('#elem .webgis-bbox-minx').val(1); $('#elem .webgis-bbox-miny').val(2); $('#elem .webgis-bbox-maxx').val(3); $('#elem .webgis-bbox-maxy').val(4); }");
        await TriggerAsync("#elem .webgis-bbox-maxy", "change");
        await AssertSnapshotAsync("input-bbox", "input-changed");
    }

    [TestMethod]
    public async Task Size_InputChanged()
    {
        await BuildAsync(Create("input-size"));
        await Page.EvaluateAsync("() => { $('#elem .webgis-size-x').val(800); $('#elem .webgis-size-y').val(600); }");
        await TriggerAsync("#elem .webgis-size-y", "change");
        await AssertSnapshotAsync("input-size", "input-changed");
    }

    [TestMethod]
    [DataRow("input-bbox", "1.5,2,3,4", "set-val")]
    [DataRow("input-bbox", "1,2", "set-val-invalid")]
    [DataRow("input-size", "800,600", "set-val")]
    [DataRow("input-size", "1,2,3", "set-val-invalid")]
    public async Task BboxSize_SetVal(string type, string value, string variant)
    {
        await BuildAsync(Create(type));
        var plugin = type == "input-bbox" ? "webgis_bbox_input" : "webgis_size_input";
        var result = await Page.EvaluateAsync<string?>($"v => $('#elem').{plugin}('val', v)", value);
        Assert.AreEqual(variant == "set-val" ? value : "", result ?? "");
        await AssertSnapshotAsync(type, variant);
    }

    [TestMethod]
    [DataRow("input-bbox")]
    [DataRow("input-size")]
    public async Task BboxSize_ElementValue(string type)
    {
        // webgis.tools.getElementValue reads the holder via data('webgis-value-element'),
        // which only the bbox plugin sets => input-size returns an empty value
        await BuildAsync(Create(type));
        var value = type == "input-bbox" ? "1,2,3,4" : "800,600";
        var plugin = type == "input-bbox" ? "webgis_bbox_input" : "webgis_size_input";
        await Page.EvaluateAsync($"v => $('#elem').{plugin}('val', v)", value);
        var result = await Page.EvaluateAsync<string>("() => String(webgis.tools.getElementValue($('#elem').get(0)))");
        Assert.AreEqual(type == "input-bbox" ? value : "", result);
    }

    #endregion

    #region condition_div

    [TestMethod]
    public async Task ConditionDiv_ElementValue()
    {
        await BuildAsync(new UIConditionDiv
        {
            id = "elem",
            ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
            ConditionArguments = ["a", "b"],
            ConditionLogicalOperator = UIConditionDiv.ContitionLogicalOperators.Or,
            ContitionElementId = "other"
        });
        await AssertSnapshotAsync("condition_div", "element-value");
    }

    [TestMethod]
    public async Task ConditionDiv_WithChildren()
    {
        var div = (UIConditionDiv)Create("condition_div");
        div.elements = [new UILiteral { literal = "Kind" }, new UIButton(UIButton.UIButtonType.clientbutton, "cmd") { id = "btn", text = "Button" }];
        await BuildAsync(div);
        await AssertSnapshotAsync("condition_div", "with-children");
    }

    [TestMethod]
    public async Task ConditionDiv_NoArguments()
    {
        // without condition arguments the generic fallback renders a <condition_div> tag
        var div = new UIConditionDiv { id = "elem", ConditionType = UIConditionDiv.ConditionTypes.LayersVisible, ConditionArguments = [] };
        div.elements = [new UILiteral { literal = "Kind" }];
        await BuildAsync(div);
        await AssertSnapshotAsync("condition_div", "no-arguments");
    }

    [TestMethod]
    public async Task ConditionDiv_NullArguments()
    {
        await BuildAsync(new UIConditionDiv { id = "elem", ConditionType = UIConditionDiv.ConditionTypes.LayersInScale });
        await AssertSnapshotAsync("condition_div", "null-arguments");
    }

    #endregion

    #region opacity-control

    [TestMethod]
    public async Task OpacityControl_GetServiceCalled()
    {
        await BuildAsync(Create("opacity-control"), setup: OpacityServiceSetup);
        var calls = await Page.EvaluateAsync<string>("() => JSON.stringify(uiHarness.calls().filter(c => c.call === 'map.getService'))");
        StringAssert.Contains(calls, "\"s1\"");
    }

    #endregion

    #region static-overlay-control

    [TestMethod]
    public async Task StaticOverlayControl_CommandButtons()
    {
        await BuildAsync(new UIStaticOverlayControl
        {
            id = "elem",
            command_buttons = [new UIStaticOverlayControl.CommandButton("cmd-1", "icon1.png"), new UIStaticOverlayControl.CommandButton("cmd-2", "icon2.png")]
        });
        await WaitForAsyncPluginsAsync("static-overlay-control");
        await AssertSnapshotAsync("static-overlay-control", "command-buttons");
    }

    #endregion

    #region image-selector

    [TestMethod]
    public async Task ImageSelector_SizeSelectedMulti()
    {
        await BuildJsonAsync(UIJson.SerializeWith(
            new UIImageSelector { id = "elem", ImageUrls = ["a.png", "b.png", "c.png"], ImageLabels = ["A"], ImageWidth = 50, ImageHeight = 40, MultiSelect = true },
            new { value = "b.png" }));
        await AssertSnapshotAsync("image-selector", "size-selected-multi");
    }

    [TestMethod]
    [DataRow(false, "clicked-single")]
    [DataRow(true, "clicked-multi")]
    public async Task ImageSelector_Clicked(bool multi, string variant)
    {
        await BuildJsonAsync(UIJson.SerializeWith(
            new UIImageSelector { id = "elem", ImageUrls = ["a.png", "b.png", "c.png"], MultiSelect = multi },
            new { value = "a.png" }));
        await TriggerAsync("#elem .webgis-image-selector-image-container:eq(1)", "click");
        await TriggerAsync("#elem .webgis-image-selector-image-container:eq(2)", "click");
        await AssertSnapshotAsync("image-selector", variant);
    }

    [TestMethod]
    public async Task ImageSelector_ClickedSelectedAgain()
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create("image-selector"), new { value = "a.png" }));
        await TriggerAsync("#elem .webgis-image-selector-image-container:eq(0)", "click");
        await AssertSnapshotAsync("image-selector", "clicked-selected-again");
    }

    #endregion

    #region sketch-info-container

    private const string OverlaySetup = "h => { $('<div id=\"sketch-overlay\"><span>alt</span></div>').appendTo('body'); h.returnsFn('map.sketchInfoOverlayContainer', function () { return $('#sketch-overlay'); }); }";

    [TestMethod]
    public async Task SketchInfo_Overlay()
    {
        await BuildAsync(Create("sketch-info-container"), setup: OverlaySetup);
        await AssertSnapshotAsync("sketch-info-container", "overlay");
    }

    [TestMethod]
    public async Task SketchInfo_NoFallback()
    {
        await BuildAsync(new UISketchInfoContainer(allowFallback: false) { id = "elem" }, setup: SetupFor("sketch-info-container"));
        await AssertSnapshotAsync("sketch-info-container", "no-fallback");
    }

    [TestMethod]
    [DataRow("hidden")]
    [DataRow("minimal")]
    public async Task SketchInfo_UserPreference(string mode)
    {
        await BuildAsync(Create("sketch-info-container"),
            setup: "h => { webgis.usability.userPreferences = { get: function (k) { return k === 'sketch-info-display-mode' ? '" + mode + "' : null; } }; (" + OverlaySetup + ")(h); }");
        await AssertSnapshotAsync("sketch-info-container", "preference-" + mode);
    }

    [TestMethod]
    [DataRow("useMobileCurrent", "mobile-current")]
    [DataRow("mobileDeviceNoBubble", "mobile-device-no-bubble")]
    public async Task SketchInfo_Mobile(string kind, string variant)
    {
        var setup = kind == "useMobileCurrent"
            ? "webgis.useMobileCurrent = function () { return true; };"
            : "webgis.isMobileDevice = function () { return true; }; webgis.usability.clickBubble = false;";
        await BuildAsync(Create("sketch-info-container"), setup: "h => { " + setup + " (" + OverlaySetup + ")(h); }");
        await AssertSnapshotAsync("sketch-info-container", variant);
    }

    [TestMethod]
    public async Task SketchInfo_WithChildren()
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create("sketch-info-container"), new { elements = new[] { new { type = "div", id = "child" } } }),
            setup: OverlaySetup);
        await AssertSnapshotAsync("sketch-info-container", "with-children");
    }

    #endregion

    #region sharelink-buttons

    [TestMethod]
    public async Task ShareLink_NoLink()
    {
        await BuildAsync(new UIShareLinkButtons(null!) { id = "elem" }, setup: ShareOptionsSetup);
        await AssertSnapshotAsync("sharelink-buttons", "no-link");
    }

    [TestMethod]
    public async Task ShareLink_Clicked()
    {
        await BuildAsync(Create("sharelink-buttons"), setup: ShareOptionsSetup);
        await ClearCallsAsync();
        // sharelink-buttons has no element properties (no id)
        await TriggerAsync("#host .webgis-ui-imagebutton", "click");
        await AssertSnapshotAsync("sharelink-buttons", "clicked");
    }

    #endregion
}
