using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Mobile.Advanced;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true, MapCrsDependent = true)]
public class DeleteOriginal : IApiServerToolAsync, IApiChildTool
{
    #region IApiButton

    public string Name => "Original Feature Löschen";

    public string Container => string.Empty;

    public string Image => string.Empty;

    public string ToolTip => string.Empty;

    public bool HasUI => true;

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        int newFeatureCounter = int.Parse(e[EditEnvironment.EditTheme.EditNewFeatuerCounterId]);

        var selectedFeatures = await e.FeaturesFromSelectionAsync(bridge);
        if (selectedFeatures.Count == 0)
        {
            return new ApiEventResponse()
            {
                ClientCommands = new ApiClientButtonCommand[] { ApiClientButtonCommand.setparenttool },
                RefreshSelection = true
            };
        }

        string originalFeaturesLabel = selectedFeatures.Count > 1 ? $"{selectedFeatures.Count} ursprüngliche Objekte:" : "Ursprüngliches Objekt:";
        string newFeaturesLabel = newFeatureCounter > 1 ? $"{newFeatureCounter} neue Objekte wurden erzeugt" : "ein neues Objekt wurde erzeugt:";

        UIDiv div = new UIDiv()
        {
            id = EditEnvironment.EditTheme.EditMaskContainerId,
            target = UIElementTarget.tool_modaldialog.ToString(),
            style = "margin:0px 10px 0px 10px"
        };

        div.elements = new IUIElement[]
        {
            new UIHidden()
            {
                id="edit-cut-originalfeature-id",
                value=""// originalFeature.Oid.ToString()
            },

            new UIBreak(),
            new UILabel() { label = originalFeaturesLabel },
            new UIImage(e[EditEnvironment.EditTheme.EditOriginalFeaturePreviewDataId], true),
            new UILabel() { label=newFeaturesLabel },
            new UIImage(e[EditEnvironment.EditTheme.EditNewFeaturePreviewDataId], true),
            new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool )
            {
                text = selectedFeatures.Count > 1 ? $"{selectedFeatures.Count} ursprüngliche Objekte beibehalten" : "Ursprüngliches Objekt beibehalten",
                css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionButtonStyle})
            },
            new UIButton(UIButton.UIButtonType.servertoolcommand, "delete-original")
            {
                text = selectedFeatures.Count > 1 ? $"{selectedFeatures.Count } ursprüngliche Objekte löschen": "Ursprüngliches Objekt löschen",
                css = UICss.ToClass(new string[] { UICss.DangerButtonStyle, UICss.OptionButtonStyle})
            }
        };

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] { div }
        };
    }

    public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        return Task.FromResult<ApiEventResponse>(null);
    }

    #endregion

    #region IApiTool Member

    public ToolType Type
    {
        get { return ToolType.none; }
    }

    public ToolCursor Cursor
    {
        get
        {
            return ToolCursor.Pointer;
        }
    }

    #endregion

    #region IApiChildTool Member

    public IApiTool ParentTool
    {
        get;
        internal set;
    }

    #endregion

    #region Commands

    [ServerToolCommand("delete-original")]
    async public Task<ApiEventResponse> OnDeleteOriginal(IBridge bridge, ApiToolEventArguments e)
    {
        var features = await e.FeaturesFromSelectionAsync(bridge);

        var editFeatureDefinition = e.EditFeatureDefinitionFromSelection(bridge, features[0]);
        if (editFeatureDefinition == null)
        {
            throw new ArgumentException("Can't find edit theme for selelction");
        }

        EditEnvironment editEnvironment = new EditEnvironment(bridge, editFeatureDefinition.ToEditThemeDefinition());

        var editTheme = e.EditThemeFromSelection(bridge, editEnvironment);
        if (editTheme == null)
        {
            throw new ArgumentException("Can't find edit theme for selelction");
        }

        if (!await editEnvironment.DeleteFeatures(editTheme, features))
        {
            throw new Exception("Löschen ist nicht möglich");
        }

        return new ApiEventResponse()
        {
            ClientCommands = new ApiClientButtonCommand[] { ApiClientButtonCommand.setparenttool, ApiClientButtonCommand.removequeryresults },
            RefreshServices = new string[] { editFeatureDefinition.ServiceId },
            UndoTool = !editEnvironment.HasUndoables ? null : new Edit(),
            ToolUndos = !editEnvironment.HasUndoables ?
                null :
                editEnvironment.Undoables.Select(u => new ToolUndoDTO(u, u.Shape)
                {
                    Title = u.ToTitle(editTheme)
                }).ToArray()
            /*, RefreshSelection = true*/
        };
    }

    #endregion
}
