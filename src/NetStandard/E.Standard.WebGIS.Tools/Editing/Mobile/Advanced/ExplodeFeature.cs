using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Advanced;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Reflection;
using System;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Mobile.Advanced;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true, MapCrsDependent = true)]
public class ExplodeFeature : IApiServerToolAsync, IApiChildTool
{
    private readonly ExplodeFeatureService _explodeService;

    public ExplodeFeature()
    {
        _explodeService = new ExplodeFeatureService();
    }

    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features.Count != 1)
        {
            throw new ArgumentException("More than one feature selected!");
        }

        var editTheme = e.EditThemeFromSelection(bridge);
        if (editTheme == null)
        {
            throw new ArgumentException("Can't find edit theme for selelction");
        }

        var editFeatureDef = e.EditFeatureDefinitionFromSelection(bridge, features[0]);
        if (editFeatureDef == null)
        {
            throw new ArgumentException("Can't find edit theme for selelction");
        }

        return await _explodeService.MaskResponse(bridge,
                                                  editTheme,
                                                  editFeatureDef,
                                                  features[0]);
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

    public string Name => "Multipart auftrennen";

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

    [ServerToolCommand("explode")]
    async public Task<ApiEventResponse> OnExplode(IBridge bridge, ApiToolEventArguments e)
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId),
            CurrentMapSrsId = e.GetInt(Edit.EditMapCrsId)
        };

        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features.Count != 1)
        {
            throw new ArgumentException("More than one feature selected!");
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

        var originalFeature = features[0];
        var originalShape = originalFeature.Shape;

        await _explodeService.ExplodeAndInsertAsync(editEnvironment, editTheme, originalFeature);

        return new ApiEventResponse()
        {
            ActiveTool = new DeleteOriginal()
            {
                ParentTool = new Edit()
            },
            RefreshServices = new string[] { editThemeDef.ServiceId },
            UISetters = await bridge.RequiredDeleteOriginalSetters(originalShape.Multiparts, originalShape)
        };
    }

    #endregion
}
