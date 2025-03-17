using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Advanced;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Mobile.Advanced.Extentions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Reflection;
using System;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Desktop.Advanced;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true, MapCrsDependent = true)]
public class ExplodeFeature : IApiServerToolAsync, IApiChildTool
{
    private const string ExplodeEditFieldPrefix = "explode_editfield";
    private readonly ExplodeFeatureService _explodeService;

    public ExplodeFeature()
    {
        _explodeService = new ExplodeFeatureService();
    }

    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        // current Feature and defintion
        var editFeatureContext = bridge.GetEditFeatureDefinitionContext(e);

        // environment with different fieldprefix (original/caller mask maybe still open in background)
        var explodeEditEnvironment = bridge.GetEditEnvironment(e, editFeatureContext.editThemeDefintion, editFieldPrefix: ExplodeEditFieldPrefix);
        var explodeEditTheme = explodeEditEnvironment[editFeatureContext.editThemeDefintion.EditThemeId];

        return await _explodeService.MaskResponse(bridge,
                                                  explodeEditTheme,
                                                  editFeatureContext.editFeatureDeftion,
                                                  editFeatureContext.editFeatureDeftion.Feature);
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
        var context = await bridge.QueryEditFeatureDefintionContext(e, editFieldPrefix: ExplodeEditFieldPrefix);

        var originalFeature = context.editFeatureDefinition?.Feature;
        if (originalFeature == null)
        {
            throw new Exception("Can't query source feature");
        }

        var originalShape = originalFeature.Shape;

        await _explodeService.ExplodeAndInsertAsync(context.editEnvironment, context.editTheme, originalFeature);

        return new ApiEventResponse()
        {
            ActiveTool = new DeleteCurrentOriginal()
            {
                ParentTool = new Edit()
            },
            RefreshServices = new string[] { context.editFeatureDefinition.ServiceId },
            UISetters = await bridge.RequiredDeleteOriginalSetters(originalShape.Multiparts, originalShape)
        };
    }

    #endregion
}
