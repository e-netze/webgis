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
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Setters;

namespace E.Standard.WebGIS.Tools.Debug.UI;

/// <summary>
/// Debug tool: UI setters (responses, that change existing elements instead of rebuilding the UI)
/// (UISetter on different element types, UISelectOptionsSetter, UICssSetter, persistent parameter setters).
/// </summary>
#if DEBUG
[Export(typeof(IApiButton))]
#endif
public class DebugUISetters : DebugUIToolBase, IApiButtonResources
{
    private const string IdPrefix = "debug-ui-setters-";

    private const string SetValuesCommand = "set-values";
    private const string SelectOptionsCommand = "select-options";
    private const string SelectOptionsEmptyCommand = "select-options-empty";
    private const string CssCommand = "css";
    private const string PersistentApplyCommand = "persistent-apply";
    private const string PersistentUpdateCommand = "persistent-update";

    public override string Name => "UI: Setters";

    public override string ToolTip => "Debug: UI setters (set values, select options, css classes, persistent parameters)";

    public override IEnumerable<Type> DemonstratedTypes =>
    [
        typeof(UISetter), typeof(UISelectOptionsSetter), typeof(UICssSetter),
        typeof(UIApplyPersistentParametersSetter), typeof(UIUpdatePersistentParametersSetter)
    ];

    public override Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var response = new ApiEventResponse()
            .AddUIElements(
                SketchSection(),
                Hint("The buttons call server commands, that only return setters (no UI elements): the UI must not be rebuilt, only the values/classes below should change. Use 'Echo' to check, if the changed values are also sent as tool parameters."),

                Section("UISetter (value)",
                    Hint("Sets a new value (with timestamp) on every element below."),
                    new UILabel() { label = "input-text" },
                    new UIInputText() { id = Id("text"), value = "initial", css = UICss.ToolParameter },
                    new UILabel() { label = "input-textarea (edit it first, then set: is the new value shown?)" },
                    new UIInputTextArea() { id = Id("textarea"), value = "initial", css = UICss.ToolParameter },
                    new UILabel() { label = "select (a, b, c) => set to the next option" },
                    new UISelect(["a", "b", "c"]) { id = Id("select"), css = UICss.ToolParameter },
                    new UILabel() { label = "hidden (see echo)" },
                    new UIHidden() { id = Id("hidden"), value = "initial", css = UICss.ToolParameter },
                    new UILabel() { label = "div (html)" },
                    new UIDiv() { id = Id("div"), style = "border:1px dashed #aaa;padding:4px" }
                        .AddChild(new UILiteral() { literal = "initial div content" }),
                    new UILabel() { label = "optionscontainer => set to the next option" },
                    OptionContainer(Id("options")),
                    new UILabel() { label = "input-bbox / input-size" },
                    new UIBoundBoxInput() { id = Id("bbox"), css = UICss.ToolParameter },
                    new UISizeInput() { id = Id("size"), css = UICss.ToolParameter },
                    new UIButtonContainer([
                        EchoButton("Set values", command: SetValuesCommand),
                        EchoButton()
                    ])),

                Section("UISelectOptionsSetter",
                    Hint("Replaces the options of the selects and selects the 2nd new option. 'empty options' clears the selects. The 2nd select is no tool parameter."),
                    new UISelect(["initial option"]) { id = Id("options-select"), css = UICss.ToolParameter },
                    new UISelect(["initial option"]) { id = Id("options-select-no-parameter") },
                    new UIButtonContainer([
                        EchoButton("Set new options", command: SelectOptionsCommand),
                        EchoButton("Set empty options", command: SelectOptionsEmptyCommand),
                        EchoButton()
                    ])),

                Section("UICssSetter",
                    Hint("AddClass/RemoveClass toggle the hidden class (webgis-display-none) of the paragraph. SelectOption selects 'circle' in the optionscontainer: is only the css changed or also the value (see echo)?"),
                    new UIParagraph("I am the paragraph that is hidden/shown") { id = Id("css-target"), style = "background:#ffd" },
                    OptionContainer(Id("css-options")),
                    new UIButtonContainer([
                        EchoButton("AddClass (hide)", argument: "add", command: CssCommand),
                        EchoButton("RemoveClass (show)", argument: "remove", command: CssCommand),
                        EchoButton("SelectOption 'circle'", argument: "select", command: CssCommand),
                        EchoButton()
                    ])),

                Section("persistent parameter setters",
                    Hint("Change the persistent input first."),
                    Hint("Apply: the server sets 'set by server' AND sends UIApplyPersistentParametersSetter => the stored value is written back (expected: your value)."),
                    Hint("Update: the server sets 'set by server' AND sends UIUpdatePersistentParametersSetter => the stored value is updated (expected: 'set by server', also after switching the tool and back)."),
                    new UIInputText() { id = Id("persistent") }.AsPersistantToolParameter(),
                    new UIButtonContainer([
                        EchoButton("Set + Apply", command: PersistentApplyCommand),
                        EchoButton("Set + Update", command: PersistentUpdateCommand),
                        EchoButton()
                    ])));

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

    [ServerToolCommand(SetValuesCommand)]
    public ApiEventResponse OnSetValues(IBridge bridge, ApiToolEventArguments e)
    {
        var time = $"{DateTime.Now:HH:mm:ss}";

        return new ApiEventResponse()
            .AddUISetters(
                new UISetter(Id("text"), $"text {time}"),
                new UISetter(Id("textarea"), $"textarea {time}\nline 2"),
                new UISetter(Id("select"), Next(e[Id("select")], "a", "b", "c")),
                new UISetter(Id("hidden"), $"hidden {time}"),
                new UISetter(Id("div"), $"<b>div html</b> {time}"),
                new UISetter(Id("options"), Next(e[Id("options")], OptionValues)),
                new UISetter(Id("bbox"), "1,2,3,4"),
                new UISetter(Id("size"), "800,600"));
    }

    [ServerToolCommand(SelectOptionsCommand)]
    public ApiEventResponse OnSelectOptions(IBridge bridge, ApiToolEventArguments e)
    {
        var time = $"{DateTime.Now:HH:mm:ss}";
        UISelect.Option[] options =
        [
            new UISelect.Option() { value = "new-1", label = $"new option 1 ({time})" },
            new UISelect.Option() { value = "new-2", label = $"new option 2 ({time})" },
            new UISelect.Option() { value = "new-3", label = $"new option 3 ({time})" }
        ];

        return new ApiEventResponse()
            .AddUISetters(
                new UISelectOptionsSetter(Id("options-select"), "new-2", options),
                new UISelectOptionsSetter(Id("options-select-no-parameter"), "new-2", options));
    }

    [ServerToolCommand(SelectOptionsEmptyCommand)]
    public ApiEventResponse OnSelectOptionsEmpty(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUISetters(
                new UISelectOptionsSetter(Id("options-select"), null, []),
                new UISelectOptionsSetter(Id("options-select-no-parameter"), null, []));

    [ServerToolCommand(CssCommand)]
    public ApiEventResponse OnCss(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUISetter(e.ServerCommandArgument switch
            {
                "add" => new UICssSetter(UICssSetter.SetterType.AddClass, Id("css-target"), UICss.HiddenUIElement),
                "remove" => new UICssSetter(UICssSetter.SetterType.RemoveClass, Id("css-target"), UICss.HiddenUIElement),
                _ => new UICssSetter(UICssSetter.SetterType.SelectOption, Id("css-options"), "circle")
            });

    [ServerToolCommand(PersistentApplyCommand)]
    public ApiEventResponse OnPersistentApply(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUISetters(
                new UISetter(Id("persistent"), "set by server"),
                new UIApplyPersistentParametersSetter(this));

    [ServerToolCommand(PersistentUpdateCommand)]
    public ApiEventResponse OnPersistentUpdate(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
            .AddUISetters(
                new UISetter(Id("persistent"), "set by server"),
                new UIUpdatePersistentParametersSetter(this));

    #endregion

    #region Helpers

    private static readonly string[] OptionValues = ["pointer", "rectangle", "polygon", "circle"];

    static private string Id(string name) => IdPrefix + name;

    static private string Next(string current, params string[] values)
        => values[(Array.IndexOf(values, current) + 1) % values.Length];

    private UIOptionContainer OptionContainer(string id)
        => new UIOptionContainer()
        {
            id = id,
            elements = [
                .. Array.ConvertAll(OptionValues, v =>
                    (IUIElement)new UIImageButton(GetType(), v, UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.unknown.ToString()) { value = v, text = v })
            ]
        }.AsToolParameter(UICss.OptionContainerWithLabels);

    #endregion
}
