using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

using WebGIS.UI.Tests.Infrastructure;

namespace WebGIS.UI.Tests.UIBuilder;

/// <summary>
/// Characterization tests for the list elements
/// (package 4: checkable-div, option-list, option-list-item, optionscontainer, click-toggle, drop-list).
/// </summary>
[TestClass]
public class ListElementsTests : UIBuilderPageTest
{
    private static readonly object TestTool = new { id = "test-tool", type = "clientbutton" };

    #region checkable-div

    [TestMethod]
    public async Task CheckableDiv_Default()
    {
        await BuildAsync(new UICheckableDiv { id = "chk" });
        await AssertSnapshotAsync("checkable-div", "default");
    }

    [TestMethod]
    public async Task CheckableDiv_IdCssStyleValue()
    {
        await BuildAsync(new UICheckableDiv { id = "chk", css = "custom-css", style = "width:100px", value = "v1" });
        await AssertSnapshotAsync("checkable-div", "id-css-style-value");
    }

    [TestMethod]
    public async Task CheckableDiv_Invisible()
    {
        await BuildAsync(new UICheckableDiv { id = "chk", visible = false });
        await AssertSnapshotAsync("checkable-div", "invisible");
    }

    [TestMethod]
    public async Task CheckableDiv_WithTool()
    {
        await BuildAsync(new UICheckableDiv { id = "chk" }, options: new { tool = TestTool });
        await AssertSnapshotAsync("checkable-div", "with-tool");
    }

    [TestMethod]
    public async Task CheckableDiv_WithChildren()
    {
        await BuildAsync(new UICheckableDiv { id = "chk", elements = [new UILabel { label = "Inhalt" }] });
        await AssertSnapshotAsync("checkable-div", "with-children");
    }

    [TestMethod]
    public async Task CheckableDiv_Clicked()
    {
        await BuildAsync(new UICheckableDiv { id = "chk" });
        await TriggerAsync("div#chk", "click");
        await AssertSnapshotAsync("checkable-div", "clicked");

        // clicking the div again keeps it checked
        await TriggerAsync("div#chk", "click");
        await AssertSnapshotAsync("checkable-div", "clicked-twice");
    }

    [TestMethod]
    public async Task CheckableDiv_CheckboxClicked()
    {
        await BuildAsync(new UICheckableDiv { id = "chk" });
        await TriggerAsync("div#chk > .checkbox", "click");
        await AssertSnapshotAsync("checkable-div", "checkbox-clicked");

        await TriggerAsync("div#chk > .checkbox", "click");
        await AssertSnapshotAsync("checkable-div", "checkbox-clicked-twice");
    }

    #endregion

    #region option-list / option-list-item

    private static UIOptionList OptionList(params string[] values) => new()
    {
        id = "list",
        elements = values.Select(v => (IUIElement)new UIOptionList.Option
        {
            value = v,
            elements = [new UILabel { label = "Option " + v }]
        }).ToList()
    };

    [TestMethod]
    public async Task OptionList_Empty()
    {
        await BuildAsync(OptionList());
        await AssertSnapshotAsync("option-list", "empty");
    }

    [TestMethod]
    public async Task OptionList_WithItems()
    {
        // the first item is selected automatically
        await BuildAsync(OptionList("a", "b", "c"));
        await AssertSnapshotAsync("option-list", "with-items");
    }

    [TestMethod]
    public async Task OptionList_IdCssStyleValue()
    {
        var list = OptionList("a");
        list.css = "custom-css";
        list.style = "width:100px";
        list.value = "v1";

        await BuildAsync(list);
        await AssertSnapshotAsync("option-list", "id-css-style-value");
    }

    [TestMethod]
    public async Task OptionList_Invisible()
    {
        var list = OptionList("a");
        list.visible = false;

        await BuildAsync(list);
        await AssertSnapshotAsync("option-list", "invisible");
    }

    [TestMethod]
    public async Task OptionList_WithTool()
    {
        await BuildAsync(OptionList("a"), options: new { tool = TestTool });
        await AssertSnapshotAsync("option-list", "with-tool");
    }

    [TestMethod]
    public async Task OptionList_Collapsable()
    {
        await BuildJsonAsync(UIJson.SerializeWith(OptionList("a", "b"), new { iscollapsable = true, collapsestate = "expanded", title = "Liste" }));
        await AssertSnapshotAsync("option-list", "collapsable");
    }

    [TestMethod]
    public async Task OptionList_ItemClicked()
    {
        await BuildAsync(OptionList("a", "b", "c"));
        await TriggerAsync(".webgis-ui-option-list-item:nth-child(3)", "click");
        await AssertSnapshotAsync("option-list", "item-clicked");
    }

    [TestMethod]
    public async Task OptionListItem_IdCssStyleIgnored()
    {
        var list = new UIOptionList
        {
            id = "list",
            elements = [new UIOptionList.Option { id = "item", css = "custom-css", style = "width:100px", value = "a" }]
        };

        await BuildAsync(list, options: new { tool = TestTool });
        await AssertSnapshotAsync("option-list-item", "id-css-style-ignored");
    }

    [TestMethod]
    public async Task OptionListItem_ObjectValue()
    {
        var list = new UIOptionList
        {
            id = "list",
            elements = [new UIOptionList.Option { value = new { id = "a", name = "A" } }]
        };

        await BuildAsync(list);
        await AssertSnapshotAsync("option-list-item", "object-value");
    }

    [TestMethod]
    public async Task OptionListItem_Standalone()
    {
        // without a surrounding option-list: still selected, no value element to set
        await BuildAsync(new UIOptionList.Option { value = "a" });
        await AssertSnapshotAsync("option-list-item", "standalone");
    }

    #endregion

    #region optionscontainer

    private static UIOptionContainer OptionsContainer(object? value = null, string? css = null) => new()
    {
        id = "options",
        css = css,
        value = value,
        elements =
        [
            new UIImageButton("ui/a-26.png", UIButton.UIButtonType.clientbutton, "option") { id = "opt-a", text = "A", value = "a" },
            new UIImageButton("ui/b-26.png", UIButton.UIButtonType.clientbutton, "option") { id = "opt-b", text = "B", value = "b" }
        ]
    };

    [TestMethod]
    public async Task OptionsContainer_Default()
    {
        // first option is clicked after building
        await BuildAsync(OptionsContainer());
        await AssertSnapshotAsync("optionscontainer", "default");
    }

    [TestMethod]
    public async Task OptionsContainer_Empty()
    {
        await BuildAsync(new UIOptionContainer { id = "options" });
        await AssertSnapshotAsync("optionscontainer", "empty");
    }

    [TestMethod]
    public async Task OptionsContainer_IdCssStyle()
    {
        var container = OptionsContainer(css: "custom-css");
        container.style = "width:100px";

        await BuildAsync(container);
        await AssertSnapshotAsync("optionscontainer", "id-css-style");
    }

    [TestMethod]
    public async Task OptionsContainer_WithTool()
    {
        await BuildAsync(OptionsContainer(), options: new { tool = TestTool });
        await AssertSnapshotAsync("optionscontainer", "with-tool");
    }

    [TestMethod]
    public async Task OptionsContainer_Value()
    {
        // initial value => setValue clicks the matching option
        await BuildAsync(OptionsContainer("b"));
        await AssertSnapshotAsync("optionscontainer", "value");
    }

    [TestMethod]
    public async Task OptionsContainer_ObjectValue()
    {
        var container = new UIOptionContainer
        {
            id = "options",
            value = new { id = "b", name = "B (container)" },
            elements =
            [
                new UIImageButton("ui/a-26.png", UIButton.UIButtonType.clientbutton, "option") { id = "opt-a", text = "A", value = new { id = "a", name = "A" } },
                new UIImageButton("ui/b-26.png", UIButton.UIButtonType.clientbutton, "option") { id = "opt-b", text = "B", value = new { id = "b", name = "B" } }
            ]
        };

        await BuildAsync(container);
        await AssertSnapshotAsync("optionscontainer", "object-value");
    }

    [TestMethod]
    public async Task OptionsContainer_AllowNullValues()
    {
        // no option is clicked automatically
        var container = OptionsContainer();
        container.AllowNullValues = true;

        await BuildAsync(container);
        await AssertSnapshotAsync("optionscontainer", "allow-null-values");
    }

    [TestMethod]
    public async Task OptionsContainer_AllowNullValuesWithValue()
    {
        var container = OptionsContainer("b");
        container.AllowNullValues = true;

        await BuildAsync(container);
        await AssertSnapshotAsync("optionscontainer", "allow-null-values-with-value");
    }

    [TestMethod]
    [DataRow("options", "b", "default-from-usability")]
    [DataRow("other", "b", "default-from-usability-other-id")]
    [DataRow("options", "x", "default-from-usability-unknown-value")]
    public async Task OptionsContainer_DefaultFromUsability(string id, string value, string variant)
    {
        await BuildAsync(OptionsContainer(),
            setup: $"h => {{ webgis.usability.optionContainerDefault = [{{ id: '{id}', value: '{value}' }}]; }}");
        await AssertSnapshotAsync("optionscontainer", variant);
    }

    [TestMethod]
    public async Task OptionsContainer_Persistent()
    {
        // persistent value wins over the initial value
        await BuildAsync(OptionsContainer("a", css: "webgis-tool-parameter-persistent"),
            setup: "h => h.returns('map.getPersistentToolParameter', 'b')");
        await AssertSnapshotAsync("optionscontainer", "persistent");

        await ClearCallsAsync();
        await TriggerAsync("#opt-a", "click");
        await AssertSnapshotAsync("optionscontainer", "persistent-clicked");
    }

    [TestMethod]
    public async Task OptionsContainer_PersistentWithoutStoredValue()
    {
        await BuildAsync(OptionsContainer(css: "webgis-tool-parameter-persistent"));
        await AssertSnapshotAsync("optionscontainer", "persistent-no-stored-value");
    }

    [TestMethod]
    public async Task OptionsContainer_PersistentWithOnChange()
    {
        // onchange wins: no persistent onChange handler
        await BuildJsonAsync(UIJson.SerializeWith(OptionsContainer("a", css: "webgis-tool-parameter-persistent"), new { onchange = "options-changed" }),
            setup: "h => h.returns('map.getPersistentToolParameter', 'b')");
        await AssertSnapshotAsync("optionscontainer", "persistent-onchange");
    }

    [TestMethod]
    public async Task OptionsContainer_Collapsable()
    {
        // the collapsable title gets a selected-option preview
        var container = OptionsContainer();
        container.title = "Optionen";
        container.icon = "ui/edit-26.png";
        container.CollapseState = UICollapsableElement.CollapseStatus.Collapsed;

        await BuildAsync(container);
        await AssertSnapshotAsync("optionscontainer", "collapsable");
    }

    [TestMethod]
    public async Task OptionsContainer_SetValue()
    {
        await BuildAsync(OptionsContainer());
        await ClearCallsAsync();
        await Page.EvaluateAsync("() => { const e = document.getElementById('options'); e.value = 'b'; e.setValue('ignored'); }");
        await FlushTimersAsync();
        await AssertSnapshotAsync("optionscontainer", "set-value");
    }

    #endregion

    #region click-toggle

    private static UIClickToggleElement ClickToggle(string id, bool resetSiblings = false)
        => new("background-color,color", "red,white", resetSiblings) { id = id, elements = [new UILabel { label = id }] };

    [TestMethod]
    public async Task ClickToggle_Default()
    {
        await BuildAsync(ClickToggle("t1"));
        await AssertSnapshotAsync("click-toggle", "default");
    }

    [TestMethod]
    public async Task ClickToggle_IdCssStyleWithTool()
    {
        var toggle = ClickToggle("t1");
        toggle.css = "custom-css";
        toggle.style = "width:100px";

        await BuildAsync(toggle, options: new { tool = TestTool });
        await AssertSnapshotAsync("click-toggle", "id-css-style-with-tool");
    }

    [TestMethod]
    public async Task ClickToggle_Clicked()
    {
        await BuildAsync(ClickToggle("t1"));
        await TriggerAsync("#t1", "click");
        await AssertSnapshotAsync("click-toggle", "clicked");

        await TriggerAsync("#t1", "click");
        await AssertSnapshotAsync("click-toggle", "clicked-twice");
    }

    [TestMethod]
    [DataRow(false, "siblings")]
    [DataRow(true, "siblings-reset")]
    public async Task ClickToggle_Siblings(bool resetSiblings, string variant)
    {
        await BuildAsync([ClickToggle("t1", resetSiblings), ClickToggle("t2", resetSiblings)]);
        await TriggerAsync("#t1", "click");
        await TriggerAsync("#t2", "click");
        await AssertSnapshotAsync("click-toggle", variant);
    }

    [TestMethod]
    public async Task ClickToggle_MoreStylesThanValues()
    {
        await BuildAsync(new UIClickToggleElement("background-color,color", "red") { id = "t1" });
        await TriggerAsync("#t1", "click");
        await AssertSnapshotAsync("click-toggle", "more-styles-than-values-clicked");
    }

    #endregion

    #region drop-list

    private const string DropSetup = "h => { window.__drop = (selector, text) => $(selector).trigger($.Event('drop', { originalEvent: { preventDefault: () => {}, dataTransfer: { getData: () => text } } })); }";

    private async Task DropAsync(string selector, string text)
    {
        await Page.EvaluateAsync("([s, t]) => window.__drop(s, t)", new object[] { selector, text });
        await FlushTimersAsync();
        AssertNoPageErrors();
    }

    [TestMethod]
    public async Task DropList_Default()
    {
        await BuildAsync(new UIDropList { id = "drop" });
        await AssertSnapshotAsync("drop-list", "default");
    }

    [TestMethod]
    public async Task DropList_IdCssStyleValue()
    {
        await BuildAsync(new UIDropList { id = "drop", css = "custom-css", style = "width:100px", value = "x,y" });
        await AssertSnapshotAsync("drop-list", "id-css-style-value");
    }

    [TestMethod]
    public async Task DropList_WithTool()
    {
        await BuildAsync(new UIDropList { id = "drop" }, options: new { tool = TestTool });
        await AssertSnapshotAsync("drop-list", "with-tool");
    }

    [TestMethod]
    public async Task DropList_Dropped()
    {
        await BuildAsync(new UIDropList { id = "drop" }, setup: DropSetup);
        await ClearCallsAsync();
        await DropAsync("#drop_droplist", "a;b");
        await AssertSnapshotAsync("drop-list", "dropped");

        // already contained values are ignored
        await ClearCallsAsync();
        await DropAsync("#drop_droplist", "b;c");
        await AssertSnapshotAsync("drop-list", "dropped-again");
    }

    [TestMethod]
    public async Task DropList_ItemRemoved()
    {
        await BuildAsync(new UIDropList { id = "drop" }, setup: DropSetup);
        await DropAsync("#drop_droplist", "a;b;c");
        await ClearCallsAsync();
        await TriggerAsync("#drop_droplist [data-dropvalue='b'] .webgis-ui-droplist-item-x", "click");
        await AssertSnapshotAsync("drop-list", "item-removed");
    }

    [TestMethod]
    public async Task DropList_Dragover()
    {
        await BuildAsync(new UIDropList { id = "drop" });
        var prevented = await Page.EvaluateAsync<bool>("() => { const e = $.Event('dragover'); $('#drop_droplist').trigger(e); return e.isDefaultPrevented(); }");
        Assert.IsTrue(prevented);
    }

    #endregion
}
