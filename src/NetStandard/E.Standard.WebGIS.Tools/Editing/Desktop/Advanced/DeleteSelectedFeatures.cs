using E.Standard.Localization.Abstractions;
using E.Standard.Localization.Reflection;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Services;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.GeoServices.Graphics.Extensions;
using gView.GraphicsEngine;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Desktop.Advanced;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true, MapCrsDependent = true)]
[LocalizationNamespace("tools.editing.deletefeatures")]
public class DeleteSelectedFeatures : IApiServerTool, IApiChildTool
{
    private readonly UpdateFeatureService _updateFeatureService;

    public DeleteSelectedFeatures()
    {
        _updateFeatureService = new UpdateFeatureService(this);
    }

    #region IApiServerTool Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEmptyUIEventResponse();
    }

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        return null;
    }

    #endregion

    public async Task<ApiEventResponse> InitResponse(IBridge bridge, ApiToolEventArguments e, IApiTool sender, ILocalizer<DeleteSelectedFeatures> localizer)
    {
        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features == null || features.Count == 0)
        {
            throw new ArgumentException("No features selected!");
        }

        var editTheme = e.EditThemeFromSelection(bridge);
        if (editTheme == null)
        {
            throw new ArgumentException("Can't find edit theme for selelction");
        }

        var editThemeDef = e.EditFeatureDefinitionFromSelection(bridge, features[0]);
        if (editThemeDef == null)
        {
            throw new ArgumentException("Can't find edit theme for selelction");
        }

        var featureShapes = features.Select(f => f.Shape).Where(s => s != null);
        var bbox = featureShapes.BoundingBox().UnionWith(featureShapes.BoundingBox());
        var previewData = await featureShapes.CreateImage(bridge, 320, 240, bbox, useColors: new ArgbColor[] { ArgbColor.Cyan });

        return new ApiEventResponse()
            .AddUIElement(new UIDiv()
                .AsDialog(UIElementTarget.tool_modaldialog)
                .WithTargetTitle(localizer.Localize("name"))
                .AddChildren(
                    new UIHidden()
                        .WithId(editTheme.EditEnvironment.EditThemeElementId)
                        .AsToolParameter()
                        .WithValue(editThemeDef.EditThemeId),
                    new UIHidden()
                        .WithId(editTheme.EditEnvironment.EditThemeDefintionElementId)
                        .AsToolParameter()
                        .WithValue(ApiToolEventArguments.ToArgument(editThemeDef.ToEditThemeDefinition())),

                    new UITitle()
                        .WithLabel(editThemeDef.EditThemeName),
                    new UIBreak(),

                    new UIImage(Convert.ToBase64String(previewData), true),

                    new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                        .WithText(localizer.Localize("cancel"))
                        .WithStyles(UICss.CancelButtonStyle, UICss.OptionButtonStyle),
                    new UIButton(UIButton.UIButtonType.servertoolcommand, "delete-selected")
                        .WithText(features.Count > 1 
                            ? String.Format(localizer.Localize("delete-features"), features.Count)
                            : localizer.Localize("delete-feature"))
                        .WithStyles(UICss.DangerButtonStyle, UICss.OptionButtonStyle)))
            .SetActiveTool(new DeleteSelectedFeatures()
            {
                ParentTool = sender
            });
    }

    #region IApiTool Member

    public ToolType Type => ToolType.none;

    public ToolCursor Cursor => ToolCursor.Custom_Pen;

    #endregion

    #region IApiButton Member

    public string Name => "Delete Selected Features";

    public string Container
    {
        get { return string.Empty; }
    }

    public string Image
    {
        get { return string.Empty; }
    }

    public string ToolTip
    {
        get { return string.Empty; }
    }

    public bool HasUI
    {
        get { return true; }
    }

    #endregion

    #region IApiChildTool Member

    public IApiTool ParentTool
    {
        get;
        internal set;
    }

    #endregion

    #region Command

    [ServerToolCommand("delete-selected")]
    async public Task<ApiEventResponse> OnDeleteSelected(IBridge bridge, ApiToolEventArguments e)
    {
        return await _updateFeatureService.DeleteSelectedFeatures(bridge, e, new Edit());
    }

    #endregion
}
