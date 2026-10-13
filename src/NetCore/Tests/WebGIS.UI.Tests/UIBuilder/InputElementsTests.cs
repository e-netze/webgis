using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;

using WebGIS.UI.Tests.Infrastructure;

namespace WebGIS.UI.Tests.UIBuilder;

/// <summary>
/// Characterization tests for the input elements (package 1: input-text, input-textarea, input-date,
/// input-number, hidden, input-autocomplete), still built by if-branches in webgis_uibuilder.js insertElement.
/// </summary>
[TestClass]
public class InputElementsTests : UIBuilderPageTest
{
    private const string AutocompleteSource = "/__autocomplete/search?x=1";

    private static readonly object TestTool = new { id = "test-tool", type = "clientbutton" };

    #region Common variants (all input types)

    private static UIElement Create(string type) => type switch
    {
        "input-text" => new UIInputText(),
        "input-textarea" => new UIInputTextArea(),
        "input-date" => new UIDatePicker(),
        "input-number" => new UIInputNumber(),
        "hidden" => new UIHidden(),
        "input-autocomplete" => new UIInputAutocomplete(AutocompleteSource, 2),
        _ => throw new ArgumentException(type)
    };

    private async Task BuildInputAsync(string type, string elementsJson, string? setup = null, object? options = null)
    {
        await BuildJsonAsync(elementsJson, setup, options);
        if (type == "input-date")
        {
            // flatpickr is loaded on demand (webgis.require) => wait until the calendar exists
            await WaitForAsync("() => !!document.querySelector('.flatpickr-calendar')");
        }
    }

    private Task BuildInputAsync(string type, IUIElement element, string? setup = null, object? options = null)
        => BuildInputAsync(type, UIJson.Serialize(element), setup, options);

    [TestMethod]
    [DataRow("input-text")]
    [DataRow("input-textarea")]
    [DataRow("input-date")]
    [DataRow("input-number")]
    [DataRow("hidden")]
    [DataRow("input-autocomplete")]
    public async Task Input_Default(string type)
    {
        await BuildInputAsync(type, Create(type));
        await AssertSnapshotAsync(type, "default");
    }

    [TestMethod]
    [DataRow("input-text")]
    [DataRow("input-textarea")]
    [DataRow("input-date")]
    [DataRow("input-number")]
    [DataRow("hidden")]
    [DataRow("input-autocomplete")]
    public async Task Input_IdCssStyle(string type)
    {
        var element = Create(type);
        element.id = "my-input";
        element.css = "custom-css another";
        element.style = "width:100px";

        await BuildInputAsync(type, element);
        await AssertSnapshotAsync(type, "id-css-style");
    }

    [TestMethod]
    [DataRow("input-text")]
    [DataRow("input-textarea")]
    [DataRow("input-date")]
    [DataRow("input-number")]
    [DataRow("hidden")]
    [DataRow("input-autocomplete")]
    public async Task Input_ReadonlyPlaceholder(string type)
    {
        await BuildInputAsync(type, UIJson.SerializeWith(Create(type), new { @readonly = true, placeholder = "Bitte eingeben" }));
        await AssertSnapshotAsync(type, "readonly-placeholder");
    }

    [TestMethod]
    [DataRow("input-text")]
    [DataRow("input-textarea")]
    [DataRow("input-date")]
    [DataRow("input-number")]
    [DataRow("hidden")]
    [DataRow("input-autocomplete")]
    public async Task Input_LabelAndValue(string type)
    {
        // label and value both set: shows which one each type uses (and the data-value/value attributes)
        var value = type == "input-number" ? "42" : "Daten 'Wert'";
        await BuildInputAsync(type, UIJson.SerializeWith(Create(type), new { label = "Label-Wert", value }));
        await AssertSnapshotAsync(type, "label-value");
    }

    [TestMethod]
    [DataRow("input-text")]
    [DataRow("input-textarea")]
    [DataRow("input-date")]
    [DataRow("input-number")]
    [DataRow("hidden")]
    [DataRow("input-autocomplete")]
    public async Task Input_Invisible(string type)
    {
        var element = Create(type);
        element.visible = false;

        await BuildInputAsync(type, element);
        await AssertSnapshotAsync(type, "invisible");
    }

    [TestMethod]
    [DataRow("input-text")]
    [DataRow("input-textarea")]
    [DataRow("input-date")]
    [DataRow("input-number")]
    [DataRow("hidden")]
    [DataRow("input-autocomplete")]
    public async Task Input_VisDependency(string type)
    {
        await BuildInputAsync(type, UIJson.SerializeWith(Create(type), new { vis_dependency = "webgis-dependencies webgis-dependency-hasselection" }));
        await AssertSnapshotAsync(type, "vis-dependency");
    }

    [TestMethod]
    [DataRow("input-text")]
    [DataRow("input-textarea")]
    [DataRow("input-date")]
    [DataRow("input-number")]
    [DataRow("hidden")]
    [DataRow("input-autocomplete")]
    public async Task Input_ParameterServerCommands(string type)
    {
        var element = Create(type);
        element.ParameterServerCommands = ["cmd1", "cmd2"];

        await BuildInputAsync(type, element);
        await AssertSnapshotAsync(type, "parameterservercommands");
    }

    [TestMethod]
    [DataRow("input-text")]
    [DataRow("input-textarea")]
    [DataRow("input-date")]
    [DataRow("input-number")]
    [DataRow("hidden")]
    [DataRow("input-autocomplete")]
    public async Task Input_WithTool(string type)
    {
        // inputs call elementProperties(element) without options => currently no data-tool-id
        await BuildInputAsync(type, Create(type), options: new { tool = TestTool });
        await AssertSnapshotAsync(type, "with-tool");
    }

    [TestMethod]
    [DataRow("input-text")]
    [DataRow("input-textarea")]
    [DataRow("input-date")]
    [DataRow("input-number")]
    [DataRow("hidden")]
    [DataRow("input-autocomplete")]
    public async Task Input_Collapsable(string type)
    {
        await BuildInputAsync(type, UIJson.SerializeWith(Create(type), new { iscollapsable = true, collapsestate = "expanded", title = "Abschnitt" }));
        await AssertSnapshotAsync(type, "collapsable");
    }

    [TestMethod]
    [DataRow("input-text")]
    [DataRow("input-textarea")]
    [DataRow("input-date")]
    [DataRow("input-number")]
    [DataRow("hidden")]
    [DataRow("input-autocomplete")]
    public async Task Input_FormContainer_ChangeMarksDirty(string type)
    {
        var element = Create(type);
        element.id = "elem";

        await BuildInputAsync(type, element, setup: "h => $('#host').addClass('webgis-form-container')");
        await AssertSnapshotAsync(type, "form-container");

        await TriggerAsync("#elem", "change", "5");
        await AssertSnapshotAsync(type, "form-container-changed");
    }

    #endregion

    #region input-text

    [TestMethod]
    public async Task InputText_PrintTextElement_FromPersistentParameter()
    {
        await BuildAsync(
            new UIInputText { id = "header", css = "webgis-print-textelement" },
            setup: "h => h.returns('map.getPersistentToolParameter', 'Gespeicherter Titel')");
        await AssertSnapshotAsync("input-text", "print-textelement-persistent");
    }

    [TestMethod]
    public async Task InputText_PrintTextElement_LabelWins()
    {
        await BuildAsync(
            new UIInputText { id = "header", css = "webgis-print-textelement", label = "Vorgabe" },
            setup: "h => h.returns('map.getPersistentToolParameter', 'Gespeicherter Titel')");
        await AssertSnapshotAsync("input-text", "print-textelement-label");
    }

    [TestMethod]
    public async Task InputText_OnChange_ServerButton()
    {
        await BuildJsonAsync(
            UIJson.SerializeWith(new UIInputText { id = "elem" }, new { onchange = "refresh" }),
            setup: "h => h.returns('map.getTool', { id: 'server-tool', type: 'serverbutton' })",
            options: new { tool = TestTool });
        await AssertSnapshotAsync("input-text", "onchange");

        await ClearCallsAsync();
        await TriggerAsync("#elem", "change", "Neu");
        await AssertSnapshotAsync("input-text", "onchange-serverbutton-changed");
    }

    [TestMethod]
    public async Task InputText_OnChange_ActiveClientTool()
    {
        await BuildJsonAsync(
            UIJson.SerializeWith(new UIInputText { id = "elem" }, new { onchange = "refresh" }),
            setup: "h => { h.returns('map.getTool', null); h.returns('map.getActiveTool', { id: 'active-tool', type: 'clientbutton' }); }");

        await ClearCallsAsync();
        await TriggerAsync("#elem", "change", "Neu");
        await AssertSnapshotAsync("input-text", "onchange-activetool-changed");
    }

    [TestMethod]
    public async Task InputText_OnChange_ValueUnchanged()
    {
        await BuildJsonAsync(
            UIJson.SerializeWith(new UIInputText { id = "elem" }, new { onchange = "refresh" }),
            setup: "h => { h.returns('map.getTool', { id: 'server-tool', type: 'serverbutton' }); h.returns('map.getPersistentToolParameter', 'Neu'); }");

        await ClearCallsAsync();
        await TriggerAsync("#elem", "change", "Neu");
        await AssertSnapshotAsync("input-text", "onchange-unchanged");
    }

    [TestMethod]
    public async Task InputText_ToolParameterPersistent()
    {
        await BuildAsync(
            new UIInputText { id = "elem", css = "webgis-tool-parameter-persistent" },
            setup: "h => h.returns('map.getActiveTool', { id: 'active-tool', type: 'clientbutton' })");
        await AssertSnapshotAsync("input-text", "parameter-persistent");

        await ClearCallsAsync();
        await TriggerAsync("#elem", "change", "Neu");
        await AssertSnapshotAsync("input-text", "parameter-persistent-changed");
    }

    [TestMethod]
    public async Task InputText_PersistentTopic_WithTool()
    {
        // the topic handler requires data-tool-id, which inputs currently never get
        await BuildAsync(
            new UIInputText { id = "elem", css = "webgis-tool-persistent-topic" },
            options: new { tool = TestTool });
        await AssertSnapshotAsync("input-text", "persistent-topic-with-tool");
    }

    [TestMethod]
    public async Task InputText_Validation()
    {
        await BuildAsync(new UIInputText { id = "elem", IsRequired = true, MinLength = 3, Regex = "^[a-z]+$" });
        await AssertSnapshotAsync("input-text", "validation");

        await TriggerAsync("#elem", "input", "ab");
        await AssertSnapshotAsync("input-text", "validation-minlen");

        await TriggerAsync("#elem", "input", "abc1");
        await AssertSnapshotAsync("input-text", "validation-regex");

        await TriggerAsync("#elem", "change", "abcd");
        await AssertSnapshotAsync("input-text", "validation-valid");
    }

    [TestMethod]
    public async Task InputText_Validation_CustomErrorMessage()
    {
        await BuildAsync(new UIInputText { id = "elem", IsRequired = true, ValidationErrorMessage = "Pflichtfeld!" });
        await AssertSnapshotAsync("input-text", "validation-custom-error");
    }

    #endregion

    #region input-number

    [TestMethod]
    public async Task InputNumber_MinMaxStep()
    {
        await BuildAsync(new UIInputNumber { MinValue = 1, MaxValue = 10, StepWidth = 0.5 });
        await AssertSnapshotAsync("input-number", "min-max-step");
    }

    [TestMethod]
    public async Task InputNumber_MinMaxStepMissing()
    {
        // hand written JSON without minValue/maxValue/stepWidth => attributes become 'undefined'
        await BuildJsonAsync("[{\"type\":\"input-number\"}]");
        await AssertSnapshotAsync("input-number", "min-max-step-missing");
    }

    [TestMethod]
    public async Task InputNumber_NumericValue()
    {
        await BuildJsonAsync("[{\"type\":\"input-number\",\"value\":7}]");
        await AssertSnapshotAsync("input-number", "numeric-value");
    }

    #endregion

    #region input-date

    [TestMethod]
    [DataRow(false, "date-time")]
    [DataRow(true, "date-only")]
    public async Task InputDate_SelectDay(bool dateOnly, string variant)
    {
        await BuildInputAsync("input-date", new UIDatePicker { id = "elem", date_only = dateOnly });
        await AssertSnapshotAsync("input-date", variant);

        await Page.EvaluateAsync("() => document.querySelector('#elem')._flatpickr.open()");
        await FlushTimersAsync();
        await AssertSnapshotAsync("input-date", variant + "-open");

        await Page.EvaluateAsync(@"() => {
            const day = [...document.querySelectorAll('.flatpickr-day:not(.prevMonthDay):not(.nextMonthDay)')]
                .find(d => d.textContent.trim() === '20');
            day.dispatchEvent(new MouseEvent('mousedown', { bubbles: true, button: 0 }));
        }");
        await FlushTimersAsync();
        AssertNoPageErrors();
        StringAssert.StartsWith(await Page.EvaluateAsync<string>("() => $('#elem').val()"), "20.01.2024");
        await AssertSnapshotAsync("input-date", variant + "-selected");
    }

    #endregion

    #region hidden

    private const string SpyAnonymousUserId =
        "h => { const ls = webgis.localStorage, g = ls.getAnonymousUserId, s = ls.setAnonyousUserId; " +
        "h.spy('webgis.localStorage.getAnonymousUserId', g); h.spy('webgis.localStorage.setAnonyousUserId', s); }";

    [TestMethod]
    public async Task Hidden_AnonymousUserId_New()
    {
        await BuildAsync(new UIHidden { id = "anon", css = "webgis-map-anonymous-user-id", value = "new-id" }, setup: SpyAnonymousUserId);
        await AssertSnapshotAsync("hidden", "anonymous-user-id-new");
        Assert.AreEqual("new-id", await Page.EvaluateAsync<string>("() => localStorage.getItem('_anonymousUserId')"));
    }

    [TestMethod]
    public async Task Hidden_AnonymousUserId_Existing()
    {
        await BuildAsync(
            new UIHidden { id = "anon", css = "webgis-map-anonymous-user-id", value = "new-id" },
            setup: "h => { localStorage.setItem('_anonymousUserId', 'existing-id'); (" + SpyAnonymousUserId + ")(h); }");
        await AssertSnapshotAsync("hidden", "anonymous-user-id-existing");
        Assert.AreEqual("existing-id", await Page.EvaluateAsync<string>("() => localStorage.getItem('_anonymousUserId')"));
    }

    #endregion

    #region input-autocomplete

    [TestMethod]
    public async Task InputAutocomplete_Validation()
    {
        await BuildAsync(new UIInputAutocomplete(AutocompleteSource) { id = "elem", IsRequired = true });
        await AssertSnapshotAsync("input-autocomplete", "validation");
    }

    [TestMethod]
    public async Task InputAutocomplete_TypingQueriesSource()
    {
        var requests = new List<string>();
        await Page.RouteAsync("**/__autocomplete/**", async route =>
        {
            requests.Add(new Uri(route.Request.Url).PathAndQuery);
            await route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "application/json; charset=utf-8",
                Body = "[{\"id\":\"1\",\"label\":\"Graz\",\"value\":\"Graz\"},{\"id\":\"2\",\"label\":\"Gratkorn\",\"value\":\"Gratkorn\"}]"
            });
        });

        await BuildAsync(new UIInputAutocomplete(AutocompleteSource, 2) { id = "elem" });
        await AssertSnapshotAsync("input-autocomplete", "source-minlength");

        await Page.EvaluateAsync("() => $('#elem').trigger('focus').val('Gra').trigger('input')");
        await FlushTimersAsync();
        await WaitForAsync("() => document.querySelectorAll('.tt-suggestion').length === 2");

        Assert.AreEqual(1, requests.Count, string.Join(Environment.NewLine, requests));
        StringAssert.StartsWith(requests[0], AutocompleteSource);
        StringAssert.Contains(requests[0], "term=Gra");

        await AssertSnapshotAsync("input-autocomplete", "suggestions");
    }

    #endregion
}
