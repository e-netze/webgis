using E.Standard.Extensions.Collections;
using E.Standard.Localization.Reflection;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Editing.Services;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using E.Standard.WebMapping.Core.Reflection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Desktop;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true,
                        ClientDeviceDependent = true,
                        MapCrsDependent = true,
                        UIElementFocus = Core.FocusableUIElements.TabPresentions | Core.FocusableUIElements.TabTools)]
[ToolId("webgis.tools.editing.desktop.insertfeature")]
[LocalizationNamespace("tools.editing.insertfeature")]
internal class InsertFeature : IApiServerToolAsync, IApiChildTool, IApiToolPersistenceContext, IApiPostRequestEvent
{
    private readonly NewFeatureService _newFeatureService;

    public InsertFeature()
    {
        _newFeatureService = new NewFeatureService();
    }

    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var editThemeDef = e.GetSelectedEditThemeDefinition();
        var mapCrsId = (int)e.MapCrs;

        if (String.IsNullOrEmpty(editThemeDef?.LayerId))
        {
            throw new Exception("Kein Edit-Thema ausgewählt");
        }

        editThemeDef.Init(bridge);

        ApiEventResponse response = await _newFeatureService
                                         .EditFeatureMaskReponse(bridge,
                                                                 this,
                                                                 editThemeDef,
                                                                 mapCrsId,
                                                                 e,
                                                                 UIElementTarget.@default.ToString()
                                                                 /*UIElementTarget.tool_modaldialog_noblocking.ToString()*/);

        response.ToolCursor = ToolCursor.Custom_Pen;
        response.ActiveToolType = ToolType.sketchany;

        response.UISetters = response.UISetters.TryAppendItems(new IUISetter[] { new UIPersistentParametersSetter(this) });

        return response;
    }

    public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        return Task.FromResult<ApiEventResponse>(null);
    }

    #endregion

    #region IApiTool Member

    public ToolType Type => ToolType.sketchany;

    public ToolCursor Cursor => ToolCursor.Custom_Pen;

    #endregion

    #region IApiButton Member

    public string Name => "create-new-object";

    public string Container => String.Empty;

    public string Image => String.Empty;

    public string ToolTip => "";

    public bool HasUI => true;

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
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId)
        };
        var feature = editEnvironment.GetFeature(bridge, e);
        if (feature == null)
        {
            throw new Exception("Es wurde keine Feature übergeben");
        }

        if (feature.Shape == null)
        {
            throw new Exception("Das Objekt besitzt noch keine Lage/Geometrie. Zum Speichern bitte Lage/Geometrie bearbeiten");
        }

        var editTheme = editEnvironment[e];
        var editThemeDef = editEnvironment.EditThemeDefinition;

        await editEnvironment.InsertFeature(editTheme, feature);

        //var response = new ApiEventResponse()
        //{
        //    ActiveTool = new Edit()
        //};

        var response = await OnButtonClick(bridge, e);

        if (e["_select"] == "true")
        {
            // Select feature
            response = await response.ToSelectFeaturesResponse(bridge, e, editEnvironment, editThemeDef);
        }

        var query = await bridge.GetFirstLayerQuery(editThemeDef.ServiceId, editThemeDef.LayerId);

        response.RefreshServices = (await bridge.GetAssociatedServiceIds(query))?.ToArray();
        response.SetEditLayerVisibility(editThemeDef);

        if (editEnvironment?.EditThemeDefinition != null)
        {
            await bridge.SetUserFavoritesItemAsync(new Edit(), "Edit", editEnvironment.EditThemeDefinition.ServiceId + "," + editEnvironment.EditThemeDefinition.LayerId + "," + editEnvironment.EditThemeDefinition.EditThemeId);
        }

        return response;
    }

    [ServerToolCommand("saveandselect")]
    async public Task<ApiEventResponse> OnSaveAndSelect(IBridge bridge, ApiToolEventArguments e)
    {
        e["_select"] = "true";
        return await OnSave(bridge, e);
    }

    [ServerToolCommand("save-and-keep-attributes")]
    async public Task<ApiEventResponse> OnSaveKeepAttributes(IBridge bridge, ApiToolEventArguments e)
    {
        var response = await OnSave(bridge, e);

        response.ActiveTool = this;

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e);
        var feature = editEnvironment.GetFeature(bridge, e);

        _newFeatureService.KeepAllAttributes(response, e, feature);

        return response;
    }

    [ServerToolCommand("save-and-continue-sketch")]
    async public Task<ApiEventResponse> OnSaveAndContinueSketch(IBridge bridge, ApiToolEventArguments e)
    {
        var response = await OnSave(bridge, e);

        response.ActiveTool = this;

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e);
        var feature = editEnvironment.GetFeature(bridge, e);

        _newFeatureService.AddLastSketchPoint(response, feature);

        return response;
    }

    [ServerToolCommand("save-and-continue-sketch-keep-attributes")]
    async public Task<ApiEventResponse> OnSaveAndContinueSketchKeepAttributes(IBridge bridge, ApiToolEventArguments e)
    {
        var response = await OnSave(bridge, e);

        response.ActiveTool = this;

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e);
        var feature = editEnvironment.GetFeature(bridge, e);

        _newFeatureService.AddLastSketchPoint(response, feature);
        _newFeatureService.KeepAllAttributes(response, e, feature);

        return response;
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

    #endregion
}
