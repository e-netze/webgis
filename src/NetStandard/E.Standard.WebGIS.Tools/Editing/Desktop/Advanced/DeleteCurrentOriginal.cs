using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Editing.Mobile.Advanced.Extentions;
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

namespace E.Standard.WebGIS.Tools.Editing.Desktop.Advanced;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(MapCrsDependent = true, ScaleDependent = true)]
public class DeleteCurrentOriginal : IApiServerTool, IApiChildTool
{
    #region IApiButton

    public string Name => "Original Feature Löschen";

    public string Container => string.Empty;

    public string Image => string.Empty;

    public string ToolTip => string.Empty;

    public bool HasUI => true;

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        int newFeatureCounter = int.Parse(e[EditEnvironment.EditTheme.EditNewFeatuerCounterId]);

        string originalFeaturesLabel = "Ursprüngliches Objekt:";
        string newFeaturesLabel = newFeatureCounter > 1 ? $"{newFeatureCounter} neue Objekte wurden erzeugt" : "ein neues Objekt wurde erzeugt:";

        UIDiv div = new UIDiv()
        {
            id = EditEnvironment.EditTheme.EditMaskContainerId,
            target = UIElementTarget.tool_modaldialog.ToString(),
            style = "margin:0px 10px 0px 10px"
        };

        var editEnvironment = bridge.GetEditEnvironment(e);

        div.elements = new IUIElement[]
        {
            new UIHidden()
            {
                id = editEnvironment.FeatureOidElementId,
                css = UICss.ToClass(new[]{ UICss.ToolParameter }),
                value = editEnvironment.GetFeatureOid(bridge, e)
            },
            new UIHidden()
            {
                id = editEnvironment.EditThemeDefintionElementId,
                css = UICss.ToClass(new[]{ UICss.ToolParameter }),
                value = e[editEnvironment.EditThemeDefintionElementId]
            },
            new UIHidden()
            {
                id = editEnvironment.EditThemeElementId,
                css = UICss.ToClass(new[]{ UICss.ToolParameter }),
                value = e[editEnvironment.EditThemeElementId]
            },
            new UIBreak(),
            new UILabel() { label = originalFeaturesLabel },
            new UIImage(e[EditEnvironment.EditTheme.EditOriginalFeaturePreviewDataId], true),
            new UILabel() { label=newFeaturesLabel },
            new UIImage(e[EditEnvironment.EditTheme.EditNewFeaturePreviewDataId], true),
            new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool )
            {
                text = "Ursprüngliches Objekt beibehalten",
                css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionButtonStyle})
            },
            new UIButton(UIButton.UIButtonType.servertoolcommand, "delete-original")
            {
                text = "Ursprüngliches Objekt löschen",
                css = UICss.ToClass(new string[] { UICss.DangerButtonStyle, UICss.OptionButtonStyle})
            }
        };

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] { div }
        };
    }

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        return null;
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
        var context = await bridge.QueryEditFeatureDefintionContext(e);

        var editEnvironment = context.editEnvironment;
        var editTheme = context.editTheme;
        var originalFeature = context.editFeatureDefinition?.Feature;
        if (originalFeature == null)
        {
            throw new Exception("Can't query source feature");
        }

        if (!await editEnvironment.DeleteFeature(editTheme, originalFeature))
        {
            throw new Exception("Löschen ist nicht möglich");
        }

        return new ApiEventResponse()
        {
            ClientCommands = new ApiClientButtonCommand[] { ApiClientButtonCommand.setparenttool, ApiClientButtonCommand.removequeryresults },
            RefreshServices = new string[] { context.editFeatureDefinition.ServiceId },
            UndoTool = !editEnvironment.HasUndoables ? null : new Edit(),
            ToolUndos = !editEnvironment.HasUndoables ?
                null :
                editEnvironment.Undoables.Select(u => new ToolUndoDTO(u, u.PreviewShape)
                {
                    Title = u.ToTitle(editTheme)
                }).ToArray()
            /*, RefreshSelection = true*/
        };
    }

    #endregion
}
