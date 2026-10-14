using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Editing;

namespace E.Standard.WebGIS.Tools.Debug.UI;

/// <summary>
/// Debug tool: buttons and menus
/// (button types/styles, client buttons, servertoolcommand_ext/callback buttons, validate-input, ctrl shortcuts,
/// imagebutton, buttongroup, button containers, optionscontainer, click-toggle, menu/menuitem,
/// undo buttons (with tool undos), sharelink-buttons).
/// </summary>
#if DEBUG
[Export(typeof(IApiButton))]
#endif
public class DebugUIButtons : DebugUIToolBase, IApiButtonResources, IApiUndoTool
{
    private const string IdPrefix = "debug-ui-buttons-";
    private const string ExtEchoCommand = "echo-ext";
    private const string ShortcutDialogCommand = "shortcut-dialog";
    private const string MenuDialogCommand = "menu-dialog";
    private const string CreateUndoCommand = "create-undo";

    public override string Name => "UI: Buttons & Menus";

    public override string ToolTip => "Debug: buttons, option containers, menus and undo buttons";

    public override IEnumerable<Type> DemonstratedTypes =>
    [
        typeof(UIButton), typeof(UICallbackButton), typeof(UIImageButton), typeof(UIButtonGroup),
        typeof(UIButtonContainer), typeof(UIStickyBottomButtonContainer), typeof(UIOptionContainer),
        typeof(UIClickToggleElement), typeof(UIMenu), typeof(UIMenuItem),
        typeof(UIToolUndoButton), typeof(UIImageToolUndoButton), typeof(UIShareLinkButtons)
    ];

    public override Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var response = new ApiEventResponse()
            .AddUIElements(
                SketchSection(),

                Section("button: types",
                    Hint("servertoolcommand: sent to the current map tool (this tool), with argument 'arg-1'."),
                    EchoButton("servertoolcommand", "arg-1"),
                    Hint("servertoolcommand_ext (UICallbackButton): sent to another tool => the echo dialog shows 'UI: Inputs' as tool."),
                    new UICallbackButton(new DebugUIInputs(), EchoCommand) { text = "servertoolcommand_ext => UI: Inputs", css = UICss.DefaultButtonStyle },
                    Hint("servertoolcommand_ext to this tool (other command)."),
                    new UICallbackButton(this, ExtEchoCommand) { text = "servertoolcommand_ext => this tool", css = UICss.DefaultButtonStyle },
                    Hint("ctrl_shortcut: only works in modal dialogs (.webgis-modal .webgis-button)."),
                    EchoButton("Open dialog with Ctrl+E button", command: ShortcutDialogCommand)),

                Section("button: client buttons",
                    Hint("Client commands, handled in the viewer without a server request."),
                    new UIButtonContainer([
                        ClientButton("refresh", ApiClientButtonCommand.refresh),
                        ClientButton("back", ApiClientButtonCommand.back),
                        ClientButton("fullextent", ApiClientButtonCommand.fullextent),
                        ClientButton("zoom2sketch", ApiClientButtonCommand.zoom2sketch),
                        ClientButton("removesketch", ApiClientButtonCommand.removesketch),
                        ClientButton("showlegend", ApiClientButtonCommand.showlegend)
                    ])),

                Section("button: styles",
                    new UIButtonContainer([
                        StyledButton("default", UICss.DefaultButtonStyle),
                        StyledButton("ok", UICss.OkButtonStyle),
                        StyledButton("cancel", UICss.CancelButtonStyle),
                        StyledButton("danger", UICss.DangerButtonStyle),
                        StyledButton("option", UICss.OptionButtonStyle),
                    ]),
                    Hint("option-rect + icon (tool resource image)"),
                    new UIButtonContainer([
                        StyledButton("pointer", UICss.CancelButtonStyle, UICss.OptionRectButtonStyle).WithIcon(this, "pointer"),
                        StyledButton("polygon", UICss.CancelButtonStyle, UICss.OptionRectButtonStyle).WithIcon(this, "polygon"),
                    ]),
                    Hint("line-break button"),
                    StyledButton("line-break", UICss.DefaultButtonStyle, UICss.LineBreakButton)),

                Section("button: validate-input",
                    Hint("A 'validate-input' button validates all inputs in the surrounding .webgis-validation-container before the command is sent (min. 3 characters)."),
                    new UIDiv()
                    {
                        css = UICss.ToClass([UICss.ValidationContainer]),
                        elements = [
                            new UIInputText() { id = Id("validate-input"), IsRequired = true, MinLength = 3, css = UICss.ToolParameter },
                            new UIButton(UIButton.UIButtonType.servertoolcommand, EchoCommand)
                            {
                                text = "Validate & echo",
                                css = UICss.ToClass([UICss.DefaultButtonStyle, UICss.ValidateInputButton])
                            }
                        ]
                    }),

                Section("imagebutton",
                    Hint("servertoolcommand imagebuttons with/without text. Click => echo."),
                    new UIDiv()
                    {
                        css = UICss.ImageButtonWithLabelsContaier,
                        elements = [
                            new UIImageButton(GetType(), "pointer", UIButton.UIButtonType.servertoolcommand, EchoCommand) { text = "with text", value = "pointer" },
                            new UIImageButton(GetType(), "rectangle", UIButton.UIButtonType.servertoolcommand, EchoCommand) { value = "rectangle" },
                        ]
                    }),

                Section("buttongroup",
                    Hint("The buttons share the width of the group."),
                    new UIButtonGroup() { elements = [StyledButton("one", UICss.DefaultButtonStyle), StyledButton("two", UICss.CancelButtonStyle)] },
                    new UIButtonGroup() { elements = [StyledButton("one", UICss.OkButtonStyle), StyledButton("two", UICss.CancelButtonStyle), StyledButton("three", UICss.DangerButtonStyle)] }),

                Section("optionscontainer",
                    Hint("Client imagebuttons (no request). The first option is selected automatically; the value is a tool parameter."),
                    OptionContainer("options", UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.unknown.ToString())
                        .AsToolParameter(UICss.OptionContainerWithLabels),
                    Hint("Persistent tool parameter: the selection is kept when the tool is reopened."),
                    OptionContainer("options-persistent", UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.unknown.ToString())
                        .AsPersistantToolParameter(UICss.OptionContainerWithLabels),
                    Hint("allow_null_values: nothing selected initially; servertoolcommand imagebuttons => echo on every click."),
                    OptionContainer("options-null", UIButton.UIButtonType.servertoolcommand, EchoCommand, allowNullValues: true)
                        .AsToolParameter(UICss.OptionContainerWithLabels),
                    EchoButton()),

                Section("click-toggle",
                    Hint("Click toggles the styles (background, color). The first three reset their siblings (only one toggled)."),
                    new UIDiv()
                    {
                        elements = [
                            ClickToggle("toggle 1 (reset siblings)", true),
                            ClickToggle("toggle 2 (reset siblings)", true),
                            ClickToggle("toggle 3 (reset siblings)", true),
                        ]
                    },
                    ClickToggle("independent toggle", false)),

                Section("menu / menuitem",
                    Hint("Menu item callbacks: toolevent => OnEvent, servertoolcommand, servertoolcommand_ext => UI: Inputs. The item value is sent as 'menuitem-value'."),
                    DemoMenu(e, "menu (header)", collapsable: false),
                    Hint("collapsable menu: click expands/collapses, the callback fires when an item is selected (collapsing)."),
                    DemoMenu(e, null, collapsable: true),
                    EchoButton("Open menu as modal dialog (closes on click)", command: MenuDialogCommand)),

                Section("undobutton",
                    Hint("Draw a sketch, then create undo entries (preview = sketch). The undo buttons list them; clicking an entry calls PerformUndo."),
                    EchoButton("Create undo entry", command: CreateUndoCommand),
                    new UIToolUndoButton(GetType(), "Undo (text)"),
                    new UIDiv()
                    {
                        css = UICss.ImageButtonWithLabelsContaier,
                        elements = [new UIImageToolUndoButton(GetType(), "undo") { text = "Undo (image)" }]
                    }),

                Section("sharelink-buttons",
                    Hint("One image button per available share option (webgis.shareOptions)."),
                    new UIShareLinkButtons("https://docs.webgiscloud.com") { subject = "WebGIS debug share link" }),

                new UIStickyBottomButtonContainer()
                {
                    elements = [
                        new UIButtonGroup()
                        {
                            elements = [
                                EchoButton("Echo (sticky bottom)"),
                                ClientButton("removesketch", ApiClientButtonCommand.removesketch)
                            ]
                        }
                    ]
                });

        return Task.FromResult(response);
    }

    #region Commands

    [ServerToolCommand(ExtEchoCommand)]
    public ApiEventResponse OnExtEcho(IBridge bridge, ApiToolEventArguments e)
        => EchoResponse(e);

    [ServerToolCommand(ShortcutDialogCommand)]
    public ApiEventResponse OnShortcutDialog(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUIElement(new UIDiv()
                .WithTarget(UIElementTarget.modaldialog)
                .WithTargetTitle("ctrl_shortcut")
                .AddChildren(
                    Hint("Press Ctrl+E (the 'E' of the button text is underlined)."),
                    new UIButton(UIButton.UIButtonType.servertoolcommand, EchoCommand)
                    {
                        text = "Echo",
                        ctrl_shortcut = "e",
                        css = UICss.DefaultButtonStyle
                    },
                    Hint("Ctrl+X: the 'X' is not in the text => appended as ' (X)'."),
                    new UIButton(UIButton.UIButtonType.servertoolcommand, EchoCommand)
                    {
                        text = "Echo",
                        ctrl_shortcut = "x",
                        buttoncommand_argument = "ctrl-x",
                        css = UICss.CancelButtonStyle
                    }));

    [ServerToolCommand(MenuDialogCommand)]
    public ApiEventResponse OnMenuDialog(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUIElement(DemoMenu(e, "menu in modal dialog", collapsable: false)
                .WithTarget(UIElementTarget.modaldialog)
                .WithTargetTitle("menu"));

    [ServerToolCommand(CreateUndoCommand)]
    public ApiEventResponse OnCreateUndo(IBridge bridge, ApiToolEventArguments e)
    {
        if (e.Sketch is null)
        {
            return EchoResponse(e, "Create undo", Hint("Draw a sketch first: it is used as undo preview."));
        }

        var title = $"Debug undo {DateTime.Now:HH:mm:ss} ({e.Sketch.GetType().Name})";

        return new ApiEventResponse()
        {
            ToolUndos = [
                new ToolUndoDTO(new EditUndoableDTO(SqlCommand.update, e.Sketch) { Data = title }, e.Sketch)
                {
                    Title = title
                }
            ]
        };
    }

    #endregion

    #region IApiUndoTool

    public Task<ApiEventResponse> PerformUndo(IBridge bridge, ToolUndoDTO toolUndo)
        => Task.FromResult(new ApiEventResponse()
            .AddUIElement(new UIDiv()
                .WithTarget(UIElementTarget.modaldialog)
                .WithTargetTitle("PerformUndo")
                .AddChildren(
                    new UILiteralBold() { literal = $"tool: {Name} ({GetType().Name})" },
                    new UIBreak(),
                    new UIParagraph($"title: {toolUndo?.Title}"),
                    new UIParagraph($"data: {toolUndo?.GetData<EditUndoableDTO>()?.Data}"))));

    #endregion

    #region IApiButtonResources

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("pointer", Properties.Resources.pointer);
        toolResourceManager.AddImageResource("rectangle", Properties.Resources.rectangle);
        toolResourceManager.AddImageResource("polygon", Properties.Resources.polygon);
        toolResourceManager.AddImageResource("circle", Properties.Resources.circle);
        toolResourceManager.AddImageResource("undo", Properties.Resources.undo);
    }

    #endregion

    #region Helpers

    static private string Id(string name) => IdPrefix + name;

    static private UIButton ClientButton(string text, ApiClientButtonCommand command)
        => new UIButton(UIButton.UIButtonType.clientbutton, command)
        {
            text = text,
            css = UICss.CancelButtonStyle
        };

    static private UIButton StyledButton(string text, params string[] styles)
        => new UIButton(UIButton.UIButtonType.servertoolcommand, EchoCommand)
        {
            text = text,
            css = UICss.ToClass(styles),
            buttoncommand_argument = text
        };

    private UIOptionContainer OptionContainer(string id, UIButton.UIButtonType buttonType, string command, bool allowNullValues = false)
        => new UIOptionContainer()
        {
            id = Id(id),
            AllowNullValues = allowNullValues ? true : null,
            elements = [
                new UIImageButton(GetType(), "pointer", buttonType, command) { value = "pointer", text = "pointer" },
                new UIImageButton(GetType(), "rectangle", buttonType, command) { value = "rectangle", text = "rectangle" },
                new UIImageButton(GetType(), "polygon", buttonType, command) { value = "polygon", text = "polygon" },
                new UIImageButton(GetType(), "circle", buttonType, command) { value = "circle", text = "circle" },
            ]
        };

    static private UIClickToggleElement ClickToggle(string text, bool resetSiblings)
        => new UIClickToggleElement("background,color", "#b5dbad,#000", resetSiblings)
        {
            style = "padding:6px;margin:2px 0;border:1px solid #aaa;cursor:pointer",
            elements = [new UILiteral() { literal = text }]
        };

    private UIMenu DemoMenu(ApiToolEventArguments e, string header, bool collapsable)
        => new UIMenu()
        {
            header = header,
            collapsable = collapsable,
            elements = [
                new UIMenuItem(this, e) { text = "toolevent => OnEvent", value = "item-toolevent" },
                new UIMenuItem(this, e, UIButton.UIButtonType.servertoolcommand, EchoCommand) { text = "servertoolcommand echo", value = "item-servertoolcommand" }
                    .WithText2("text2")
                    .WithSubText("subtext"),
                new UIMenuItem(new DebugUIInputs(), e, UIButton.UIButtonType.servertoolcommand_ext, EchoCommand) { text = "servertoolcommand_ext => UI: Inputs", value = "item-ext" },
                new UIMenuItem(this, e, UIButton.UIButtonType.servertoolcommand, EchoCommand)
                {
                    text = "with icon",
                    value = "item-icon",
                    icon = UIImageButton.ToolResourceImage(this, "polygon")
                },
                new UIMenuItem(this, e, UIButton.UIButtonType.servertoolcommand, EchoCommand)
                {
                    text = "removable (✖ => menuitem-item-command=remove-element)",
                    value = "item-removable"
                }.AsRemovable(),
                new UIMenuItem(this, e, UIButton.UIButtonType.servertoolcommand, EchoCommand)
                {
                    text = "icon_large",
                    value = "item-icon-large",
                    icon_large = UIImageButton.ToolResourceImage(this, "rectangle")
                }
            ]
        };

    #endregion
}

static file class DebugUIButtonExtensions
{
    static public UIButton WithIcon(this UIButton button, IApiButton owner, string imageName)
    {
        button.icon = UIImageButton.ToolResourceImage(owner, imageName);
        return button;
    }
}
