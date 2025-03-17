using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Services;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Desktop;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true,
                        ClientDeviceDependent = true,
                        MapCrsDependent = true,
                        MapImageSizeDependent = true)]
[ToolId("webgis.tools.editing.desktop.deletefeature")]
internal class DeleteFeature : IApiServerTool, IApiChildTool, IApiToolConfirmation, IApiPostRequestEvent
{
    private readonly UpdateFeatureService _updateFeatureService;

    public DeleteFeature()
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

    public async Task<ApiEventResponse> InitResponse(IBridge bridge, ApiToolEventArguments e, IApiTool sender)
    {
        UpdateFeatureService.ModifyEditMask modifyEditMask = (mask, feature) =>
        {
            mask.UIElements.First().target = UIElementTarget.tool_modaldialog.ToString();
        };

        var response = await _updateFeatureService.EditMaskResponse(bridge, e,
                                                                    EditOperation.Delete,
                                                                    sketckType: UpdateFeatureService.SketchType.ReadOnly,
                                                                    modifyEditMask: modifyEditMask);

        response.ToolCursor = ToolCursor.Custom_Pen;
        response.ActiveToolType = ToolType.sketchany;

        response.ActiveTool = new DeleteFeature()
        {
            ParentTool = sender
        };

        return response;
    }

    #region IApiTool Member

    public ToolType Type => ToolType.sketchany;

    public ToolCursor Cursor => ToolCursor.Custom_Pen;

    #endregion

    #region IApiButton Member

    public string Name
    {
        get { return "Objekt löschen"; }
    }

    public string Container
    {
        get { return String.Empty; }
    }

    public string Image
    {
        get { return String.Empty; }
    }

    public string ToolTip
    {
        get { return "Objekte in der Karte löschen"; }
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

    #region IApiToolConfirmation Member

    virtual public ApiToolConfirmation[] ToolConfirmations
    {
        get
        {
            List<ApiToolConfirmation> confirmations = new List<ApiToolConfirmation>(ApiToolConfirmation.CommandComfirmations(typeof(DeleteFeature)));

            return confirmations.ToArray();
        }
    }

    #endregion

    #region IApiPostRequestEvent

    public Task<ApiEventResponse> PostProcessEventResponseAsync(IBridge bridge, ApiToolEventArguments e, ApiEventResponse response)
    {
        return new Edit().PostProcessEventResponseAsync(bridge, e, response);
    }

    #endregion

    #region Commands

    [ServerToolCommand("delete")]
    //[ToolCommandConfirmation("Soll das Objekt wirklich gelöscht werden?", ApiToolConfirmationType.YesNo, ApiToolConfirmationEventType.ButtonClick)]
    async public Task<ApiEventResponse> OnDelete(IBridge bridge, ApiToolEventArguments e)
    {
        var response = await _updateFeatureService.DeleteFeature(bridge, e, new Edit(),
                                                                 removeFeatureFromTable: true);

        return response;
    }

    #endregion
}
