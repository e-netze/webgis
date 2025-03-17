using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Reflection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Mobile.Advanced;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true, MapCrsDependent = true)]
public class MassAttributation : IApiServerToolAsync, IApiChildTool
{
    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {

        var editEnvironment = new EditEnvironment(bridge, (EditThemeDefinition)null);
        var editFeatureDef = editEnvironment.EditThemeDefinition;

        var editTheme = e.EditThemeFromSelection(bridge);
        if (editTheme == null)
        {
            throw new ArgumentException("Can't find edit theme definition");
        }

        int srsId = editTheme.SrsId(0);
        ApiOidFilter filter = new ApiOidFilter(e.SelectionInfo.ObjectIds.First())
        {
            QueryGeometry = true,
            FeatureSpatialReference = bridge.CreateSpatialReference(srsId),
            Fields = QueryFields.All
        };

        var editFeatures = await bridge.QueryLayerAsync(e.SelectionInfo.ServiceId, e.SelectionInfo.LayerId, filter);
        if (editFeatures.Count == 1)
        {
            var editFeature = editFeatures[0];
            var editFeatureDefinition = e.EditFeatureDefinitionFromSelection(bridge, editFeature);
            var editThemeDefination = editFeatureDefinition.ToEditThemeDefinition();

            var response = new ApiEventResponse();

            EditEnvironment.UIEditMask mask = await editTheme.ParseMask(bridge,

                                                                        editThemeDefination,
                                                                        EditOperation.MassAttributation,
                                                                        editFeature);
            response.UIElements = mask.UIElements;
            response.UISetters = mask.UISetters;

            return response;
        }

        return null;
    }

    public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        return Task.FromResult<ApiEventResponse>(null);
    }

    #endregion

    #region IApiTool Member

    virtual public ToolType Type
    {
        get;
        private set;
    }

    public ToolCursor Cursor
    {
        get
        {
            return ToolCursor.Crosshair;
        }
    }

    #endregion

    #region IApiButton Member

    public string Name => "Massenattributierung";

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

    #region Commands

    [ServerToolCommand("save")]
    async public Task<ApiEventResponse> OnSave(IBridge bridge, ApiToolEventArguments e)
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId),
            CurrentMapSrsId = e.GetInt(Edit.EditMapCrsId)
        };
        var feature = editEnvironment.GetFeature(bridge, e, checkApplyField: true);
        if (feature.Attributes == null || feature.Attributes.Count == 0)
        {
            throw new Exception("Keine Attribute für die Massenattributierung ausgewählt");
        }

        var editTheme = editEnvironment[e];
        var selection = e.SelectionInfo;

        await editEnvironment.MassAttributeFeatures(editTheme, feature, selection.ObjectIds);

        #region Updating QueryResults

        ApiOidsFilter filter = new ApiOidsFilter(selection.ObjectIds)
        {
            QueryGeometry = false,
            Fields = QueryFields.All
        };

        var updatedFeatures = await bridge.QueryLayerAsync(selection.ServiceId, selection.LayerId, filter);
        var query = await bridge.GetQuery(selection.ServiceId, selection.QueryId);
        var sRef = bridge.CreateSpatialReference(4326);

        #endregion

        var editThemeDef = editEnvironment.EditThemeDefinition;

        return new ApiEventResponse()
        {
            ClientCommands = new ApiClientButtonCommand[] { ApiClientButtonCommand.setparenttool },
            //ActiveTool = new Edit(),
            //ActiveTool = new EditSelectUpdateFeature()
            //{
            //    ParentTool = new Edit()
            //},
            RefreshServices = new string[] { editThemeDef.ServiceId },
            ReplaceQueryFeatures = updatedFeatures.Count > 0 ? updatedFeatures : null,
            ReplaceFeaturesQueries = await bridge.GetLayerQueries(query),
            ReplaceFeatureSpatialReference = sRef,
        };
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
