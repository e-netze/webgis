using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

namespace E.Standard.WebGIS.Tools.Debug.UI;

/// <summary>
/// Debug tool: text and layout elements
/// (title, label, paragraph, literal, br, div, columns, input-stack, card, image, checkable-div, condition_div, empty).
/// </summary>
#if DEBUG
[Export(typeof(IApiButton))]
#endif
public class DebugUITextLayout : DebugUIToolBase
{
    private const string ConditionSelectId = "debug-ui-layout-condition-select";
    private const string OpenDialogCommand = "open-dialog";
    private const string CloseDialogCommand = "close-dialog";

    // 16x16 red square
    private const string Base64Png = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAGUlEQVR4nGO4Y2PznxLMMGrAqAGjBgwXAwCuWFMfG2uhAwAAAABJRU5ErkJggg==";

    public override string Name => "UI: Text & Layout";

    public override string ToolTip => "Debug: text and layout UI elements";

    public override IEnumerable<Type> DemonstratedTypes =>
    [
        typeof(UITitle), typeof(UILabel), typeof(UIParagraph),
        typeof(UILiteral), typeof(UILiteralBold), typeof(UILiteralWarning),
        typeof(UIBreak), typeof(UIDiv), typeof(UIColumns), typeof(UIColumn),
        typeof(UIInputElementStack), typeof(UICard), typeof(UIImage),
        typeof(UICheckableDiv), typeof(UIConditionDiv), typeof(UIEmpty)
    ];

    public override Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var response = new ApiEventResponse()
            .AddUIElements(
                Hint("Expand the sections and check the rendering. 'Echo' sends all tool parameters to the server."),

                SketchSection(),

                Section("title / label",
                    new UITitle() { label = "UITitle (title)" },
                    new UILabel() { label = "UILabel (label)" },
                    new UILabel() { label = "UILabel with <b>html</b> (not trusted => encoded)" },
                    new UILabel() { label = "UILabel with <b>html</b> (IsTrusted = true)", IsTrusted = true }),

                Section("paragraph",
                    new UIParagraph("UIParagraph: short text."),
                    new UIParagraph("UIParagraph: a long text, that should wrap inside the tool pane. " +
                                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."),
                    new UIParagraph("UIParagraph with special chars: < > & \" ' äöüß €"),
                    new UIParagraph("UIParagraph with css 'webgis-info'") { css = "webgis-info" }),

                Section("literal / literal-bold / warning",
                    new UILiteral() { literal = "UILiteral: plain text. " },
                    new UILiteralBold() { literal = "UILiteralBold: bold text. " },
                    new UILiteral() { literal = "UILiteral (as_markdown): **bold**, *italic*, `code`", as_markdown = true },
                    new UILiteralWarning("UILiteralWarning: this is a warning")),

                Section("br",
                    new UILiteral() { literal = "before UIBreak(1)" },
                    new UIBreak(),
                    new UILiteral() { literal = "before UIBreak(3)" },
                    new UIBreak(3),
                    new UILiteral() { literal = "after UIBreak(3)" }),

                Section("div (collapsable)",
                    new UIDiv() { style = "border:1px dashed #aaa;padding:4px" }
                        .AddChild(new UIParagraph("plain UIDiv with style")),
                    new UIDiv() { title = "UIDiv: CollapseState = Expanded", CollapseState = UICollapsableElement.CollapseStatus.Expanded }
                        .AddChild(new UIParagraph("content of the expanded div")),
                    new UIDiv() { title = "UIDiv: CollapseState = Collapsed", CollapseState = UICollapsableElement.CollapseStatus.Collapsed }
                        .AddChild(new UIParagraph("content of the collapsed div")),
                    new UIDiv() { title = "UIDiv: Exclusive #1", CollapseState = UICollapsableElement.CollapseStatus.Collapsed, ExpandBehavior = UICollapsableElement.ExpandBehaviorMode.Exclusive }
                        .AddChild(new UIParagraph("exclusive #1: opening #2 should close this one")),
                    new UIDiv() { title = "UIDiv: Exclusive #2", CollapseState = UICollapsableElement.CollapseStatus.Collapsed, ExpandBehavior = UICollapsableElement.ExpandBehaviorMode.Exclusive }
                        .AddChild(new UIParagraph("exclusive #2: opening #1 should close this one"))),

                Section("columns",
                    new UIColumns().AddChildren(
                        new UIColumn(120).AddChildren(new UITitle() { label = "Column 1" }, new UIParagraph("width 120px")),
                        new UIColumn(120).AddChildren(new UITitle() { label = "Column 2" }, new UIParagraph("width 120px")))),

                Section("input element stack",
                    new UIInputElementStack(
                    [
                        new UIInputText() { id = "debug-ui-layout-stack-1", placeholder = "stacked input 1" }.AsToolParameter(),
                        new UIInputText() { id = "debug-ui-layout-stack-2", placeholder = "stacked input 2" }.AsToolParameter()
                    ]),
                    EchoButton()),

                Section("card",
                    new UICard("UICard with title").AddChild(new UIParagraph("card content")),
                    new UICard().AddChild(new UIParagraph("UICard without title"))),

                Section("image",
                    new UILabel() { label = "UIImage (url):" },
                    new UIImage($"{bridge.AppRootUrl}/content/api/img/tools/legend.png"),
                    new UILabel() { label = "UIImage (base64):" },
                    new UIImage(Base64Png, true) { style = "width:32px;height:32px" }),

                Section("checkable-div",
                    new UICheckableDiv() { id = "debug-ui-layout-checkable" }
                        .AsToolParameter()
                        .AddChild(new UIParagraph("Click the checkbox. The tool parameter value should be 'checked' or empty.")),
                    EchoButton()),

                Section("condition_div (ElementValue)",
                    new UISelect(["", "a", "b", "c"]) { id = ConditionSelectId }.AsToolParameter(),
                    new UIConditionDiv()
                    {
                        ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                        ContitionElementId = ConditionSelectId,
                        ConditionArguments = ["a"]
                    }.AddChild(new UIParagraph("visible if value == 'a'")),
                    new UIConditionDiv()
                    {
                        ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                        ContitionElementId = ConditionSelectId,
                        ConditionArguments = ["a"],
                        ConditionResult = false
                    }.AddChild(new UIParagraph("visible if value != 'a' (ConditionResult = false)")),
                    new UIConditionDiv()
                    {
                        ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                        ContitionElementId = ConditionSelectId,
                        ConditionArguments = ["*"]
                    }.AddChild(new UIParagraph("visible if any value is selected ('*')"))),

                Section("empty (close target)",
                    Hint("Opens a modal dialog. The button in the dialog returns an UIEmpty, which should close the dialog."),
                    new UIButton(UIButton.UIButtonType.servertoolcommand, OpenDialogCommand)
                    {
                        text = "Open dialog",
                        css = UICss.ToClass([UICss.DefaultButtonStyle])
                    }),

                new UIButtonContainer(EchoButton("Echo all tool parameters")));

        return Task.FromResult(response);
    }

    #region Commands

    [ServerToolCommand(OpenDialogCommand)]
    public ApiEventResponse OnOpenDialog(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUIElement(new UIDiv()
                .WithTarget(UIElementTarget.modaldialog)
                .WithTargetTitle("Debug dialog")
                .AddChildren(
                    new UIParagraph("Click the button to close this dialog with an UIEmpty response."),
                    new UIButton(UIButton.UIButtonType.servertoolcommand, CloseDialogCommand)
                    {
                        text = "Close (UIEmpty)",
                        css = UICss.ToClass([UICss.DefaultButtonStyle])
                    }));

    [ServerToolCommand(CloseDialogCommand)]
    public ApiEventResponse OnCloseDialog(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUIElement(new UIEmpty().WithTarget(UIElementTarget.modaldialog));

    #endregion
}
