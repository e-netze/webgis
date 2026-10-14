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
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebGIS.Tools.Debug.UI;

/// <summary>
/// Debug tool: input elements
/// (input-text, input-textarea, input-number, validation, input-autocomplete, input-date, select, hidden,
/// option-list, drop-list, input-bbox, input-size, upload-file) and tool parameter behaviour
/// (persistent parameters, parameters restricted to server commands).
/// </summary>
#if DEBUG
[Export(typeof(IApiButton))]
#endif
public class DebugUIInputs : DebugUIToolBase
{
    private const string IdPrefix = "debug-ui-inputs-";
    private const string AutocompleteCommand = "autocomplete";
    private const string UploadCommand = "upload";
    private const string RestrictedEchoCommand = "echo-restricted";
    private const string UploadId = IdPrefix + "upload";

    private static readonly string[] AutocompleteValues =
        ["Apple", "Apricot", "Banana", "Blackberry", "Cherry", "Grape", "Lemon", "Mango", "Orange", "Pear"];

    private static readonly string[] OptionListValues = ["first", "second", "third"];

    public override string Name => "UI: Inputs";

    public override string ToolTip => "Debug: input UI elements and tool parameters";

    public override IEnumerable<Type> DemonstratedTypes =>
    [
        typeof(UIInputText), typeof(UIInputTextField), typeof(UIInputTextArea), typeof(UIInputNumber),
        typeof(UIValidation), typeof(UIInputAutocomplete), typeof(UIDatePicker), typeof(UISelect),
        typeof(UIHidden), typeof(UIOptionList), typeof(UIOptionList.Option), typeof(UIDropList),
        typeof(UIBoundBoxInput), typeof(UISizeInput), typeof(UIUploadFile)
    ];

    public override Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var response = new ApiEventResponse()
            .AddUIElements(
                Hint("All inputs (except the 'not a parameter' ones) are tool parameters. 'Echo' sends them to the server."),

                SketchSection(),

                Section("input-text",
                    new UILabel() { label = "value" },
                    new UIInputText().WithId(Id("text-value")).WithValue("initial value").AsToolParameter(),
                    new UILabel() { label = "label (historically used as initial value)" },
                    new UIInputText() { label = "label as value" }.WithId(Id("text-label")).AsToolParameter(),
                    new UILabel() { label = "placeholder" },
                    new UIInputText() { placeholder = "placeholder..." }.WithId(Id("text-placeholder")).AsToolParameter(),
                    new UILabel() { label = "readonly" },
                    new UIInputText(readOnly: true).WithId(Id("text-readonly")).WithValue("readonly value").AsToolParameter(),
                    new UILabel() { label = "not a parameter (no tool parameter css => not in echo)" },
                    new UIInputText().WithId(Id("text-no-parameter")).WithValue("not sent"),
                    new UIInputTextField()
                        .WithLabelText("UIInputTextField (label + input, rendering only)"),
                    EchoButton()),

                Section("input-textarea / input-number",
                    new UILabel() { label = "textarea (value)" },
                    new UIInputTextArea().WithId(Id("textarea")).WithValue("line 1\nline 2").AsToolParameter(),
                    new UILabel() { label = "textarea (readonly, placeholder)" },
                    new UIInputTextArea(readOnly: true) { placeholder = "readonly placeholder..." }.WithId(Id("textarea-readonly")).AsToolParameter(),
                    new UILabel() { label = "number (min 0, max 10, step 0.5, value 5)" },
                    new UIInputNumber() { MinValue = 0, MaxValue = 10, StepWidth = 0.5 }.WithId(Id("number")).WithValue(5).AsToolParameter(),
                    new UILabel() { label = "number (readonly, placeholder)" },
                    new UIInputNumber(readOnly: true) { placeholder = "readonly" }.WithId(Id("number-readonly")).AsToolParameter(),
                    EchoButton()),

                Section("validation",
                    Hint("Type into the inputs: an error message should appear below the input while it is invalid."),
                    new UILabel() { label = "required" },
                    new UIInputText() { IsRequired = true }.WithId(Id("validation-required")).AsToolParameter(),
                    new UILabel() { label = "min length 3" },
                    new UIInputText() { MinLength = 3 }.WithId(Id("validation-minlen")).AsToolParameter(),
                    new UILabel() { label = "regex ^[0-9]+$ (custom error message)" },
                    new UIInputText() { Regex = "^[0-9]+$", ValidationErrorMessage = "Custom error: only digits allowed" }.WithId(Id("validation-regex")).AsToolParameter(),
                    EchoButton()),

                Section("input-autocomplete",
                    Hint($"Source: rest/toolmethod => server command '{AutocompleteCommand}' (values: {string.Join(", ", AutocompleteValues)})."),
                    new UILabel() { label = "min length 1" },
                    new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge, this.GetType(), AutocompleteCommand)).WithId(Id("autocomplete")).AsToolParameter(),
                    new UILabel() { label = "min length 0 (suggestions on focus)" },
                    new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge, this.GetType(), AutocompleteCommand), 0).WithId(Id("autocomplete-0")).AsToolParameter(),
                    EchoButton()),

                Section("input-date",
                    new UILabel() { label = "date and time" },
                    new UIDatePicker().WithId(Id("date-time")).AsToolParameter(),
                    new UILabel() { label = "date only, initial value" },
                    new UIDatePicker() { date_only = true }.WithId(Id("date-only")).WithValue("24.12.2026").AsToolParameter(),
                    EchoButton()),

                Section("select",
                    new UILabel() { label = "options, default value 'b'" },
                    new UISelect(["a", "b", "c"]) { defaultvalue = "b" }.WithId(Id("select")).AsToolParameter(),
                    new UILabel() { label = "options with labels (value != label)" },
                    new UISelect()
                    {
                        options = [
                            new UISelect.Option() { value = "1", label = "One (1)" },
                            new UISelect.Option() { value = "2", label = "Two (2)" }
                        ]
                    }.WithId(Id("select-labels")).AsToolParameter(),
                    new UILabel() { label = $"change => server command '{EchoCommand}'" },
                    new UISelect(UIButton.UIButtonType.servertoolcommand, EchoCommand)
                    {
                        options = SelectOptions("", "x", "y", "z")
                    }.WithId(Id("select-change")).AsToolParameter(),
                    new UILabel() { label = "allow_addvalues (extendable number combo)" },
                    new UISelect() { allow_addvalues = true, options = SelectOptions("100", "500", "1000") }.WithId(Id("select-addvalues")).AsToolParameter(),
                    new UILabel() { label = "allow_pro_behaviour (select2, only if webgis.usability.select_pro_behaviour = 'select2')" },
                    new UISelect() { allow_pro_behaviour = true, options = SelectOptions(AutocompleteValues) }.WithId(Id("select-pro")).AsToolParameter(),
                    EchoButton()),

                Section("hidden / persistent / restricted parameters",
                    Hint("hidden: invisible, but sent as tool parameter."),
                    new UIHidden().WithId(Id("hidden")).WithValue("hidden value").AsToolParameter(),
                    new UILabel() { label = "persistent parameter (value is kept after closing/reopening the tool)" },
                    new UIInputText().WithId(Id("persistent")).AsPersistantToolParameter(),
                    new UILabel() { label = $"only sent with server command '{RestrictedEchoCommand}'" },
                    new UIInputText()
                        .WithId(Id("restricted"))
                        .WithValue("restricted value")
                        .AsToolParameter()
                        .WithParameterForServerCommands(RestrictedEchoCommand),
                    EchoButton($"Echo ('{EchoCommand}' => without restricted)"),
                    EchoButton($"Echo ('{RestrictedEchoCommand}' => with restricted)", command: RestrictedEchoCommand)),

                Section("option-list",
                    Hint("Click an item: it gets selected, the value is sent as tool parameter. The first item is selected automatically."),
                    new UIOptionList()
                        .WithId(Id("option-list"))
                        .AsToolParameter()
                        .AddChildren(
                            OptionListValues
                                .Select(v => new UIOptionList.Option()
                                    .WithValue(v)
                                    .AddChildren(
                                        new UILiteralBold().WithLiteral($"Item: "),
                                        new UILiteral().WithLiteral(v)))),
                    EchoButton()),

                Section("drop-list",
                    Hint("Select some text (e.g. this hint) and drag it into the box below. 'X' removes an item. Value: comma separated items."),
                    new UIDropList() { style = "min-height:40px;border:1px dashed #aaa" }.WithId(Id("drop-list")).AsToolParameter(),
                    EchoButton()),

                Section("input-bbox / input-size",
                    new UILabel() { label = "input-bbox (minx, miny, maxx, maxy)" },
                    new UIBoundBoxInput().WithId(Id("bbox")).AsToolParameter(),
                    new UILabel() { label = "input-bbox (readonly)" },
                    new UIBoundBoxInput(readOnly: true).WithId(Id("bbox-readonly")).AsToolParameter(),
                    new UILabel() { label = "input-size (width, height)" },
                    new UISizeInput().WithId(Id("size")).AsToolParameter(),
                    new UILabel() { label = "input-size (readonly)" },
                    new UISizeInput(readOnly: true).WithId(Id("size-readonly")).AsToolParameter(),
                    EchoButton()),

                Section("upload-file",
                    Hint($"Select a file: it is uploaded to server command '{UploadCommand}', which shows the file info."),
                    new UIUploadFile(this.GetType(), UploadCommand).WithId(UploadId).AsToolParameter()),

                EchoButton());

        return Task.FromResult(response);
    }

    #region Commands

    [ServerToolCommand(RestrictedEchoCommand)]
    public ApiEventResponse OnRestrictedEcho(IBridge bridge, ApiToolEventArguments e)
        => EchoResponse(e);

    [ServerToolCommand(AutocompleteCommand)]
    public ApiEventResponse OnAutocomplete(IBridge bridge, ApiToolEventArguments e)
    {
        var term = (e["term"] ?? string.Empty).ToLower();

        return new ApiRawJsonEventResponse(
            AutocompleteValues
                .Where(v => v.ToLower().Contains(term))
                .ToArray());
    }

    [ServerToolCommand(UploadCommand)]
    public ApiEventResponse OnUpload(IBridge bridge, ApiToolEventArguments e)
    {
        var file = e.GetFile(UploadId);

        return EchoResponse(e, "Upload",
            new UITitle() { label = "File" },
            file is null
                ? Hint($"no file '{UploadId}' received")
                : new UIParagraph($"name: {file.FileName}, content type: {file.ContentType}, size: {file.Data?.Length ?? 0} bytes"),
            new UIBreak());
    }

    #endregion

    #region Helpers

    static private string Id(string name) => IdPrefix + name;

    static private UISelect.Option[] SelectOptions(params string[] values)
        => values.Select(v => new UISelect.Option() { value = v, label = v == "" ? "(select...)" : v }).ToArray();

    #endregion
}
