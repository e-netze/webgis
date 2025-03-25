using E.Standard.Localization.Reflection;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Desktop.Advanced;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Editing.Services;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Reflection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Desktop;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true,
                        ClientDeviceDependent = true,
                        MapCrsDependent = true,
                        MapImageSizeDependent = true,
                        UIElementFocus = Core.FocusableUIElements.TabPresentions | Core.FocusableUIElements.TabTools)]
[ToolId("webgis.tools.editing.desktop.updatefeature")]
[LocalizationNamespace("tools.editing.updatefeature")]
public class UpdateFeature : IApiServerTool, IApiChildTool, IApiToolPersistenceContext, IApiPostRequestEvent
{
    private readonly UpdateFeatureService _updateFeatureService;

    public UpdateFeature()
    {
        _updateFeatureService = new UpdateFeatureService(this);
        //this.ParentTool = new Edit();
    }

    #region IApiServerTool Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        //return new ApiEmptyUIEventResponse();

        // neutrales Response zurückgeben -> UI bleibt unverändert, wird schon in InitResponse gesetzt
        // UI Response ist aber notwendig, damit das Werkeuzug als Aktives Werkzeug gesetzt wird!!
        //    Werkzeuge ohne UI werden nie aktiv gesetzt

        return new ApiNeutralUIEventresponse();
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
            var uiTools = new UIDiv();

            if (feature?.Shape?.IsMultipart == true)
            {
                uiTools.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "explodefeature")
                {
                    text = "Multipart auftrennen (explode)",
                    css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                    icon = UIButton.ToolResourceImage(typeof(Edit), "explode")
                });
            }

            if (uiTools.elements?.Count() > 0)
            {
                var maskUIElements = mask.UIElements;

                mask.UIElements = new IUIElement[]
                {
                    new UIDiv()
                    {
                        elements = new IUIElement[]
                        {
                            uiTools,
                            new UIDiv() { elements = mask.UIElements.ToArray() }
                        }
                    }
                };
            }

            mask.UIElements.First().target = UIElementTarget.@default.ToString(); //UIElementTarget.tool_modaldialog_noblocking.ToString();
        };

        // Try append Parameter needed later on 
        // PostProcessEventResponseAsync (snap/fix points)
        // ... an artifact from the (old) mobile edit environment 
        e.TryAppendEditThemeDefintion()
         .TryAppendEditThemeOid();

        var response = (await _updateFeatureService.EditMaskResponse(
                    bridge, e,
                    EditOperation.Update,
                    sketckType: UpdateFeatureService.SketchType.Editable,
                    modifyEditMask: modifyEditMask
                ))
                .TryAddApplyEditingThemeProperty(e);


        response.ToolCursor = ToolCursor.Custom_Pen;
        response.ActiveToolType = ToolType.sketchany;
        response.FocusSketch = true;

        response.ActiveTool = new UpdateFeature()
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
        get { return "Objekt Bearbeiten"; }
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
        get { return "Objekte in der Karte bearbeiten"; }
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

    #region IApiToolPersistenceContext Member

    public Type PersistenceContextTool
    {
        get
        {
            return typeof(Edit);
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

    [ServerToolCommand("save")]
    async public Task<ApiEventResponse> OnSave(IBridge bridge, ApiToolEventArguments e)
    {
        return await _updateFeatureService.SaveFeature(bridge, e, new Edit());
    }

    [ServerToolCommand("select-by-legend")]
    public ApiEventResponse SelectByLegend(IBridge bridge, ApiToolEventArguments e)
    {
        return new EditService().SelectByLegend(bridge, e);
    }

    [ServerToolCommand("show-select-by-legend")]
    async public Task<ApiEventResponse> ShowSelectByLegend(IBridge bridge, ApiToolEventArguments e)
    {
        return await new EditService().ShowSelectByLegend(bridge, e);
    }

    [ServerEventHandler(ServerEventHandlers.OnUpdateCombo)]
    public Task<ApiEventResponse> OnUpdateCombo(IBridge bridge, ApiToolEventArguments e)
    {
        return new EditService().OnUpdateCombo(bridge, e);
    }

    [ServerToolCommand("explodefeature")]
    public ApiEventResponse OnExplodeFeature(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new ExplodeFeature()
            {
                ParentTool = new Edit()
            }
        };
    }

    #endregion
}
