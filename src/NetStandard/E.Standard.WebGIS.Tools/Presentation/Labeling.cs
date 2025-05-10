using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using E.Standard.WebMapping.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Presentation;

[Export(typeof(IApiButton))]
[ToolHelp("tools/presentation/labeling.html")]
[AdvancedToolProperties(LabelingDependent = true, ClientDeviceDependent = true)]
public class Labeling : IApiServerButtonLocalizable<Labeling>,
                        IApiButtonResources
{
    #region ApiServerButton

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Labeling> localizer)
    {
        if (!String.IsNullOrEmpty(e["labeling-theme"]))
        {
            return OnLabelingThemeChanged(bridge, e, localizer);
        }

        return new ApiEventResponse()
            .AddUIElement(
                new UILabelingCombo() { onchange = "labelingthemechanged" }
                    .WithId("labeling-theme")
                    .AsPersistantToolParameter(UICss.ToolInitializationParameterImportant));
    }

    public string Container => "Presentation";

    public bool HasUI => true;

    public string Image => UIImageButton.ToolResourceImage(this, "label_theme");

    public string Name => "Themen Beschriften";

    public string ToolTip => "Label Layers";

    #endregion

    #region Commands

    [ServerToolCommand("init")]
    [ServerToolCommand("labelingthemechanged")]
    public ApiEventResponse OnLabelingThemeChanged(IBridge bridge, ApiToolEventArguments e, ILocalizer<Labeling> localizer)
    {
        string labelingParameter = e["labeling-theme"];
        //string[] activeLabelings = e.GetArrayOrEmtpy<string>("labeling-active-labelings");

        var labeling = bridge.GetLabeling();

        return AppendUI(bridge, new ApiEventResponse(), localizer, labelingParameter, labeling.Where(l => l.Id == labelingParameter).Count() > 0);
    }

    private ApiEventResponse AppendUI(IBridge bridge, ApiEventResponse apiResponse, ILocalizer<Labeling> localizer, string labelingParameter, bool labelingIsActive)
    {
        var labeling = labelingParameter.Contains("~") ? bridge.ServiceLabeling(labelingParameter.Split('~')[0], labelingParameter.Split('~')[1]) : null;

        apiResponse.AddUIElement(
            new UILabelingCombo() { onchange = "labelingthemechanged" }
                .WithId("labeling-theme")
                .AsPersistantToolParameter());

        if (labeling != null)
        {
            apiResponse.AddUIElement(new UIBreak(2));

            apiResponse.AddUIElement(
                new UILabel()
                    .WithLabel(localizer.Localize("labeling-field")));

            var fieldOptions = new List<UISelect.Option>();
            var fieldAliases = labeling.FieldAliases;

            foreach (var fieldName in fieldAliases.Keys)
            {
                string fieldAlias = fieldAliases[fieldName];
                fieldOptions.Add(new UISelect.Option() { label = fieldAlias, value = fieldName });
            }

            apiResponse.AddUIElement(
                new UISelect()
                    .WithId("labeling-field")
                    .AsToolParameter()
                    .AddOptions(fieldOptions));


            apiResponse.AddUIElements(
                new UILabel()
                    .WithLabel(localizer.Localize("labeling-method")),
                new UISelect()
                    .WithId("labeling-howmanylabels")
                    .AsPersistantToolParameter()
                    .AddOptions(
                        new UISelect.Option()
                            .WithValue("one_label_per_part")
                            .WithLabel(localizer.Localize("one_label_per_part")),
                        new UISelect.Option()
                            .WithValue("one_label_per_name")
                            .WithLabel(localizer.Localize("one_label_per_name")),
                        new UISelect.Option()
                            .WithValue("one_label_per_shape")
                            .WithLabel(localizer.Localize("one_label_per_shape"))),

                new UIFontSizeSelector(localizer.Localize("font-size"), UIButton.UIButtonType.clientbutton)
                    .WithExpandBehavior(UICollapsableElement.ExpandBehaviorMode.Exclusive)
                    .WithId("labeling-fontsize")
                    .AsPersistantToolParameter()
                    .WithValue(12),

                new UIFontStyleSelector(localizer.Localize("font-style"), UIButton.UIButtonType.clientbutton)
                    .WithExpandBehavior(UICollapsableElement.ExpandBehaviorMode.Exclusive)
                    .WithId("labeling-fontstyle")
                    .AsPersistantToolParameter()
                    .WithValue("regular"),

                new UIColorSelector(localizer.Localize("font-color"), UIButton.UIButtonType.clientbutton, allowNoColor: false)
                    .WithExpandBehavior(UICollapsableElement.ExpandBehaviorMode.Exclusive)
                    .WithId("labeling-fontcolor")
                    .AsPersistantToolParameter()
                    .WithValue("#000000"),

                new UIColorSelector(localizer.Localize("border-color"), UIButton.UIButtonType.clientbutton, allowNoColor: false)
                    .WithExpandBehavior(UICollapsableElement.ExpandBehaviorMode.Exclusive)
                    .WithId("labeling-bordercolor")
                    .AsPersistantToolParameter()
                    .WithValue("#ffffff"));

            var buttonContainer = new UIButtonContainer();

            if (labelingIsActive)
            {
                buttonContainer.AddChild(
                    new UICallbackButton(this, "unsetlabel")
                        .WithText(localizer.Localize("remove"))
                        .WithStyles(UICss.CancelButtonStyle));
            }
            buttonContainer.AddChild(
                new UICallbackButton(this, "setlabel")
                    .WithText(localizer.Localize("apply"))
                    .WithId(this.GetType().ToToolId()));

            apiResponse.AddUIElement(buttonContainer);
        }

        return apiResponse.AddUISetter(new UIPersistentParametersSetter(this));
    }

    [ServerToolCommand("setlabel")]
    public ApiEventResponse SetLabel(IBridge bridge, ApiToolEventArguments e, ILocalizer<Labeling> localizer)
    {
        string labelingParameter = e["labeling-theme"];
        var labeling = labelingParameter.Contains("~") ? bridge.ServiceLabeling(labelingParameter.Split('~')[0], labelingParameter.Split('~')[1]) : null;

        if (labeling != null)
        {
            var labelingDefintion = new LabelingDefinitionDTO()
            {
                Id = labelingParameter,
                Field = e["labeling-field"],
                HowManyLabels = e["labeling-howmanylabels"],
                FontSize = e.GetInt("labeling-fontsize"),
                FontStyle = e["labeling-fontstyle"],
                FontColor = e["labeling-fontcolor"],
                BorderColor = e["labeling-bordercolor"]
            };

            var response = new ApiEventResponse()
                .DoRefreshServices(labelingParameter.Contains("~") ? labelingParameter.Split('~')[0] : "*")
                .AddLabeling(new LabelingDefinitionDTO[] { labelingDefintion });

            return AppendUI(bridge, response, localizer, labelingParameter, true);
        }

        return null;
    }

    [ServerToolCommand("unsetlabel")]
    public ApiEventResponse UnsetLabel(IBridge bridge, ApiToolEventArguments e, ILocalizer<Labeling> localizer)
    {
        string labelingParameter = e["labeling-theme"];

        var response = new ApiEventResponse()
            .DoRefreshServices(labelingParameter.Contains("~") ? labelingParameter.Split('~')[0] : "*")
            .RemoveLabeling(new LabelingDefinitionDTO()
            {
                Id = labelingParameter == "#" ? "" : labelingParameter
            });

        return AppendUI(bridge, response, localizer, labelingParameter, false);
    }

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("label_theme", Properties.Resources.label_theme);
    }

    #endregion
}
