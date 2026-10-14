using System;
using System.Collections.Generic;
using System.Linq;
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
/// Debug tool: element targets (modal dialogs, tool dialogs, dock panels, #id selectors, persistent topic),
/// closing targets (UIEmpty), target options (title, size, onclose) and info/validation boxes.
/// </summary>
#if DEBUG
[Export(typeof(IApiButton))]
#endif
public class DebugUITargets : DebugUIToolBase
{
    private const string IdPrefix = "debug-ui-targets-";
    private const string PlaceholderId = IdPrefix + "placeholder";

    private const string OpenCommand = "open";
    private const string CloseCommand = "close";
    private const string OpenSizedCommand = "open-sized";
    private const string OpenWithOnCloseCommand = "open-onclose";
    private const string OnClosedCommand = "on-closed";
    private const string IdTargetCommand = "id-target";
    private const string IdTargetAppendCommand = "id-target-append";
    private const string MultipleTargetsCommand = "multiple-targets";

    /// <summary>
    /// "tool_aside" is handled by the JS UI builder, but is not part of <see cref="UIElementTarget"/> (used as string by LiveShareMap).
    /// </summary>
    private const string ToolAsideTarget = "tool_aside";

    private static readonly string[] ModalTargets =
        [nameof(UIElementTarget.modaldialog), nameof(UIElementTarget.modaldialog_hidden)];

    private static readonly string[] ToolModalTargets =
    [
        nameof(UIElementTarget.tool_modaldialog), nameof(UIElementTarget.tool_modaldialog_hidden),
        nameof(UIElementTarget.tool_modaldialog_noblocking), nameof(UIElementTarget.tool_modaldialog_noblocking_closable)
    ];

    private static readonly string[] DockTargets =
    [
        nameof(UIElementTarget.tool_sidebar_bottom), nameof(UIElementTarget.tool_sidebar_top),
        nameof(UIElementTarget.tool_sidebar_left), ToolAsideTarget
    ];

    public override string Name => "UI: Targets & Dialogs";

    public override string ToolTip => "Debug: element targets (dialogs, dock panels, #id), closing targets, info and validation boxes";

    public override IEnumerable<Type> DemonstratedTypes =>
    [
        typeof(UIEmpty), typeof(UIInfoBox), typeof(UICollapsableHelp),
        typeof(UIValidationError), typeof(UIValidationErrorSummary)
    ];

    public override Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var response = new ApiEventResponse()
            .AddUIElements(
                SketchSection(),
                Hint("Every 'open' button returns a div with the selected target. The target content contains an echo button (callbacks from the target) and a 'close' button (UIEmpty with the same target)."),

                Section("modal dialogs",
                    Hint("modaldialog_hidden: the dialog is created, but not shown => nothing visible expected."),
                    TargetButtons(ModalTargets),
                    new UIButtonContainer([
                        EchoButton("modaldialog with size 600x250", command: OpenSizedCommand),
                        EchoButton("modaldialog with onclose (servertoolcommand)", command: OpenWithOnCloseCommand)
                    ])),

                Section("tool modal dialogs",
                    Hint("tool dialogs are docked left. noblocking: the map can be used while the dialog is open. closable: the dialog has a close button. hidden: nothing visible expected."),
                    TargetButtons(ToolModalTargets)),

                Section("dock panels",
                    Hint($"'{ToolAsideTarget}' is no UIElementTarget enum value (only a string target, used by LiveShareMap)."),
                    TargetButtons(DockTargets)),

                Section("#id target",
                    Hint("Target '#id': the element is inserted into an existing element (replace: emptied first, append: added)."),
                    new UIDiv() { id = PlaceholderId, style = "border:1px dashed #aaa;min-height:24px;padding:4px" }
                        .AddChild(new UILiteral() { literal = "placeholder (initial content)" }),
                    new UIButtonContainer([
                        EchoButton("into #id (replace)", command: IdTargetCommand),
                        EchoButton("into #id (append)", command: IdTargetAppendCommand)
                    ])),

                Section("multiple targets",
                    Hint("One response (append mode) with elements for: default (appended to this tool UI), modaldialog and tool_sidebar_bottom."),
                    EchoButton("multiple targets", command: MultipleTargetsCommand)),

                Section("info / validation boxes",
                    new UIInfoBox("UIInfoBox(text): a simple info box"),
                    new UILabel() { label = "UIInfoBox(targetId, conditions): shows the text matching the select value" },
                    new UISelect(["a", "b", "c"]) { id = Id("info-select"), css = UICss.ToolParameter },
                    new UIInfoBox(Id("info-select"), new Dictionary<string, string>()
                    {
                        { "a", "info for value 'a'" },
                        { "b", "info for value 'b'" },
                        { "c", "info for value 'c'" }
                    }),
                    new UICollapsableHelp("UICollapsableHelp (click to expand)", "help content of the collapsable help"),
                    new UIValidationError("UIValidationError: a single error"),
                    new UIValidationErrorSummary(["UIValidationErrorSummary: error 1", "error 2", ""]),
                    Hint("Empty messages: UIValidationError(\"\") and UIValidationErrorSummary([]) render as empty divs (nothing visible expected)."),
                    new UIValidationError(""),
                    new UIValidationErrorSummary(Array.Empty<string>())));

        return Task.FromResult(response);
    }

    #region Commands

    [ServerToolCommand(OpenCommand)]
    public ApiEventResponse OnOpen(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUIElement(TargetContent(e.ServerCommandArgument));

    [ServerToolCommand(CloseCommand)]
    public ApiEventResponse OnClose(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUIElement(new UIEmpty().WithTarget(e.ServerCommandArgument));

    [ServerToolCommand(OpenSizedCommand)]
    public ApiEventResponse OnOpenSized(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUIElement(TargetContent(nameof(UIElementTarget.modaldialog)).WithSize(600, 250));

    [ServerToolCommand(OpenWithOnCloseCommand)]
    public ApiEventResponse OnOpenWithOnClose(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUIElement(TargetContent(nameof(UIElementTarget.modaldialog))
                .WithTargetTitle("modaldialog with onclose => closing it calls 'on-closed'")
                .WithTargetOnClose(OnClosedCommand, CommandType.ServerToolCommand));

    [ServerToolCommand(OnClosedCommand)]
    public ApiEventResponse OnClosed(IBridge bridge, ApiToolEventArguments e)
        => EchoResponse(e, "Echo: dialog closed (targetonclose)");

    [ServerToolCommand(IdTargetCommand)]
    public ApiEventResponse OnIdTarget(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUIElement(new UIParagraph($"replaced at {DateTime.Now:HH:mm:ss}").WithTarget($"#{PlaceholderId}"));

    [ServerToolCommand(IdTargetAppendCommand)]
    public ApiEventResponse OnIdTargetAppend(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .UIElementsBehavoir(AppendUIElementsMode.Append)
            .AddUIElement(new UIParagraph($"appended at {DateTime.Now:HH:mm:ss}").WithTarget($"#{PlaceholderId}"));

    [ServerToolCommand(MultipleTargetsCommand)]
    public ApiEventResponse OnMultipleTargets(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .UIElementsBehavoir(AppendUIElementsMode.Append)
            .AddUIElements(
                new UIParagraph($"default target (appended) {DateTime.Now:HH:mm:ss}") { style = "background:#ffd" },
                TargetContent(nameof(UIElementTarget.modaldialog)),
                TargetContent(nameof(UIElementTarget.tool_sidebar_bottom)));

    #endregion

    #region Helpers

    static private string Id(string name) => IdPrefix + name;

    static private UIButtonContainer TargetButtons(IEnumerable<string> targets)
        => new UIButtonContainer(targets
            .Select(t => (IUIElement)EchoButton(t, argument: t, command: OpenCommand))
            .ToArray());

    static private UIDiv TargetContent(string target)
        => new UIDiv()
            .WithTarget(target)
            .WithTargetTitle($"target: {target}")
            .AddChildren(
                new UIParagraph($"content of target '{target}' ({DateTime.Now:HH:mm:ss})"),
                new UIButtonContainer([
                    EchoButton("Echo (from target)", argument: target),
                    new UIButton(UIButton.UIButtonType.servertoolcommand, CloseCommand)
                    {
                        text = "Close (UIEmpty)",
                        css = UICss.ToClass([UICss.CancelButtonStyle]),
                        buttoncommand_argument = target
                    }
                ]));

    #endregion
}
