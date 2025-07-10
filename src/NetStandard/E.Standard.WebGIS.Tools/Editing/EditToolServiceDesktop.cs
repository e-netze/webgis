using E.Standard.Extensions.Collections;
using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Tools.Editing.Desktop.Advanced;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Identify;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing;

internal class EditToolServiceDesktop : IEditToolService
{
    public const string WebGisEditSelectionToolId = "webgis-edit-selection-tool";
    public const string WebGisEditSelectionThemeId = "webgis-edit-selection-theme";
    public const string WebGisEditSelectionThemeTags = "webgis-edit-selection-theme-tags";
    public const string WebGisCurrentSelectionToolId = "webgis-edit-current-selection-tool";
    public const string WebGisQueryBuilderId = "webgis-edit-querybuilder";

    public const string SelectionToolIdPointer = "pointer";
    public const string SelectionToolIdRectangle = "rectangle";
    public const string SelectionToolIdRectangleQueryBuilder = "rectangle_querybuilder";

    public const string IdentifyBoxUiPrefix = "editor";
    public const string SelectionFeatureLimit = "selection-feature-limit";

    public static string[] ShortCutKeys = [
        "e", // edit
        "d", // delete
        //"m", // merge
        //"c", // cut
        //"x", // clip
        //"a", // mass attributation
        //"f"  // features transfer
    ];

    private readonly IApiTool _sender;
    private readonly ILocalizer _localizer;

    public EditToolServiceDesktop(IApiTool sender, ILocalizer localizer)
    {
        _sender = sender;
        _localizer = localizer;
    }

    #region IEditToolService

    public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        List<IUIElement> uiElements = new List<IUIElement>(
           new IUIElement[] {
                new UIHidden(){
                    id = Edit.EditAllThemesId,
                    css = UICss.ToClass(new string[]{UICss.ToolParameter, /*UICss.AutoSetterAllEidtThemesInScale*/ UICss.AutoSetterAllEditThemes })
                },
                new UIHidden(){
                    id = Edit.EditMapScaleId,
                    css = UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapScale})
                },
                new UIHidden(){
                    id = Edit.EditMapCrsId,
                    css = UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapCrsId})
                },
                new UIHidden()
                {
                    id = WebGisCurrentSelectionToolId,
                    css = UICss.ToClass(new string[]{UICss.ToolParameter}),
                    value = SelectionToolIdPointer
                }
           });

        uiElements.Add(new UIOptionContainer()
        {
            id = WebGisEditSelectionToolId,
            css = UICss.ToClass(new string[] { UICss.OptionContainerWithLabels }),
            //value = "pointer",
            elements = new IUIElement[]
            {
                new UIImageButton(_sender.GetType(), "pointer", UIButton.UIButtonType.servertoolcommand, "pointer"){
                    value = SelectionToolIdPointer,
                    text = _localizer.Localize("desktop.point-selection")
                },
                new UIImageButton(_sender.GetType(), "rectangle", UIButton.UIButtonType.servertoolcommand, "rectangle"){
                    value = SelectionToolIdRectangle,
                    text = _localizer.Localize("desktop.rectangle-selection")
                },
                new UIImageButton(_sender.GetType(), "rectangle-binoculars", UIButton.UIButtonType.servertoolcommand, "rectangle_querybuilder"){
                    value = SelectionToolIdRectangleQueryBuilder,
                    text = _localizer.Localize("desktop.rectangle-selection-querybuilder")
                },
                new UIImageButton(_sender.GetType(), "insert", UIButton.UIButtonType.servertoolcommand, "newfeature"){
                    value = "newfeature",
                    text = _localizer.Localize("desktop.new-feature")
                },
                new UIImageToolUndoButton(_sender.GetType(), "undo"){
                    id = "webgis-edit-undo-button",
                    text = _localizer.Localize("desktop.undo")
                },
            }
        });

        uiElements.Add(new UIEditThemeTree()
        {
            id = WebGisEditSelectionThemeId,
            css = UICss.ToClass(new string[] { UICss.ToolParameter, UICss.ToolParameterPersistent })
        });

        return Task.FromResult(new ApiEventResponse()
        {
            UIElements = uiElements.ToArray()
        });
    }

    [ServerToolCommand("box")]
    async public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        var currentSelectionToolId = e[WebGisCurrentSelectionToolId];
        var currentKeyPressed = e.CurrentKeyPressed;
        var editThemeQuery = await e.GetSelectedEditThemeQuery(bridge);

        if (currentSelectionToolId == SelectionToolIdRectangleQueryBuilder)
        {
            var uiQueryBuilder = new UIQueryBuilder()
            {
                id = WebGisQueryBuilderId,
                ShowGeometryOption = true,
            };

            #region Collect Queries => LayerFields

            List<IQueryBridge> queries = new List<IQueryBridge>(
                editThemeQuery != null ?
                    new[] { editThemeQuery } :
                    await e.GetVisibleQueries(bridge, WebMapping.Core.ServiceLayerVisibility.Visible)
                );

            foreach (var query in queries)
            {
                var queryLayerFields = await bridge.GetServiceLayerFields(query.GetServiceId(), query.GetLayerId());

                if (queryLayerFields == null)
                {
                    continue;
                }

                foreach (var queryLayerField in queryLayerFields)
                {
                    uiQueryBuilder.TryAddField(queryLayerField.Name, queryLayerField.Type);
                }
            }

            #endregion

            return new ApiEventResponse()
            {
                UIElements = new IUIElement[]
                {
                    new UIDiv()
                    {
                        target =  UIElementTarget.modaldialog.ToString(),
                        targettitle = _localizer.Localize("desktop.querybuilder"),
                        targetwidth = "600px",
                        targetheight = "400px",
                        elements = new IUIElement[] { uiQueryBuilder }
                    }
                },
                ToolCursor = ToolCursor.Custom_Selector_Highlight,
                ActiveToolType = ToolType.click,
                UISetters = new IUISetter[]
                {
                    new UISetter(WebGisEditSelectionToolId, SelectionToolIdPointer),
                    new UISetter(WebGisCurrentSelectionToolId, SelectionToolIdPointer)
                }
            };
        }

        var identifyToolService = new IdentifyServiceDesktop();
        var queryBuilderResult = e.QueryBuilderResult(WebGisQueryBuilderId);

        (await e.AppendIdentifyAllQueriesString(bridge, true))
                .AppendIdentifyQueryThemeId(editThemeQuery, IdentifyConst.QueryVisibleIgnoreFavorites)
                .AppendIdentifyMultiResultTarget(UIElementTarget.tool_sidebar_top.ToString())
                .AppendIdentifyForceCheckboxes()
                .AppendIdentifyUiPrefix(IdentifyBoxUiPrefix)
                .AppendIdentifyFeatureLimit(e.GetConfigInt(SelectionFeatureLimit))
                .AppendIdentiyIgnoreQueryShape(queryBuilderResult?.GeometryOption == "ignore")
                .AppendIdentifyWhereClauseFromQueryBuilder(queryBuilderResult);

        ApiEventResponse response = null;

        try
        {
            response = await identifyToolService.OnEvent(_sender, bridge, e, bridge.GetLocalizer<IdentifyDefault>());
        }
        catch
        {
            response = new ApiEventResponse();
        }

        if (response is ApiFeaturesEventResponse featureResponse
            && featureResponse.Filter is ApiSpatialFilter spatialFilter)
        {
            if (ShortCutKeys.Any(s => s.Equals(currentKeyPressed, StringComparison.OrdinalIgnoreCase)))
            {
                featureResponse.ReduceToClosest(bridge);
            }
        }

        response.ToolCursor = ToolCursor.Custom_Selector_Highlight;
        response.ActiveToolType = ToolType.click;
        response.UISetters = response.UISetters ?? new List<IUISetter>();

        response.UISetters.Add(new UISetter(WebGisEditSelectionToolId, SelectionToolIdPointer));
        response.UISetters.Add(new UISetter(WebGisCurrentSelectionToolId, SelectionToolIdPointer));

        return response;
    }

    #endregion

    #region Commands

    [ServerToolCommand("pointer")]
    public ApiEventResponse OnPointerToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveToolType = WebMapping.Core.Api.ToolType.click,
            ToolCursor = ToolCursor.Custom_Selector_Highlight,
            UISetters = new IUISetter[]
            {
                new UISetter(WebGisCurrentSelectionToolId, SelectionToolIdPointer)
            }
        };
    }

    [ServerToolCommand("rectangle")]
    public ApiEventResponse OnRectangleToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveToolType = ToolType.box,
            ToolCursor = ToolCursor.Custom_Rectangle,
            UISetters = new IUISetter[]
            {
                new UISetter(WebGisCurrentSelectionToolId, SelectionToolIdRectangle)
            }
        };
    }

    [ServerToolCommand("rectangle_querybuilder")]
    public ApiEventResponse OnRectangleQueryBuilder(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveToolType = ToolType.box,
            ToolCursor = ToolCursor.Custom_Rectangle,
            UISetters = new IUISetter[]
            {
                new UISetter(WebGisCurrentSelectionToolId, SelectionToolIdRectangleQueryBuilder)
            }
        };
    }

    [ServerToolCommand("querybuilder")]
    async public Task<ApiEventResponse> OnQueryBuilder(IBridge bridge, ApiToolEventArguments e)
    {
        var queryBuilderResult = e.QueryBuilderResult(WebGisQueryBuilderId);

        var editThemeQuery = await e.GetSelectedEditThemeQuery(bridge);

        #region Collect all Queries with QueryDefs Fields

        List<IQueryBridge> allQueries = new List<IQueryBridge>(
                editThemeQuery != null ?
                    new[] { editThemeQuery } :
                    await e.GetVisibleQueries(bridge, WebMapping.Core.ServiceLayerVisibility.Visible)
                );

        List<IQueryBridge> queries = new List<IQueryBridge>();
        var queryDefFields = queryBuilderResult?.QueryDefs?.Select(q => q.Field).Distinct();

        if (queryDefFields.IsNullOrEmpty())
        {
            queries.AddRange(allQueries);
        }
        else
        {
            foreach (var query in allQueries)
            {
                var fields = await bridge.GetServiceLayerFields(query.GetServiceId(), query.GetLayerId());
                if (fields.Where(f => queryDefFields.Contains(f.Name)).Count() == queryDefFields.Count())
                {
                    queries.Add(query);
                }
            }
        }

        #endregion

        var identifyToolService = new IdentifyServiceDesktop();

        (await e.AppendIdentifyAllQueriesString(bridge))
                .AppendIdentifyQueryThemeId(editThemeQuery, IdentifyConst.QueryAllIgnoreFavorites)
                .AppendIdentifyMultiResultTarget(UIElementTarget.tool_sidebar_top.ToString())
                .AppendIdentifyForceCheckboxes()
                .AppendIdentifyUiPrefix(IdentifyBoxUiPrefix)
                .AppendIdentifyFeatureLimit(e.GetConfigInt(SelectionFeatureLimit))
                .AppendIdentiyIgnoreQueryShape(queryBuilderResult?.GeometryOption == "ignore")
                .AppendIdentifyWhereClauseFromQueryBuilder(queryBuilderResult);

        ApiEventResponse response = null;

        response = await identifyToolService.OnEvent(_sender, bridge, e, bridge.GetLocalizer<IdentifyDefault>());

        response.UIElements = new List<IUIElement>(response.UIElements);
        response.UIElements.Add(new UIEmpty()
        {
            target = UIElementTarget.modaldialog.ToString(),
        });

        return response;
    }

    [ServerToolCommand("newfeature")]
    public ApiEventResponse OnNewFeatureClick(IBridge bridge, ApiToolEventArguments e)
    {
        var editThemeDef = e.GetSelectedEditThemeDefinition();

        if (String.IsNullOrEmpty(editThemeDef?.LayerId))
        {
            throw new InfoException(_localizer.Localize("desktop.exception-select-edit-theme"));
        }

        return new ApiEventResponse()
        {
            ActiveTool = new Desktop.InsertFeature()
            {
                ParentTool = _sender
            }
        };
    }

    [ServerToolCommand("updatefeature")]
    public async Task<ApiEventResponse> OnUpdateFeatureClick(IBridge bridge, ApiToolEventArguments e)
    {
        //var featureOid = e["feature-oid"];
        //var editThemeName = e["edittheme"];

        //var oid = featureOid.ParseFeatureGlobalOid();
        //var editThemeBridge = bridge.GetEditThemes(oid.serviceId).Where(a => a.ThemeId == editThemeName).FirstOrDefault();

        //if (editThemeBridge != null && !editThemeBridge.DbRights.HasFlag(CMS.EditingRights.Geometry))
        //{
        //    return await (new Mobile.UpdateFeature().OnEditAttributes(bridge, e));
        //}

        return await (new Desktop.UpdateFeature()).InitResponse(bridge, e, _sender);
    }

    [ServerToolCommand("deletefeature")]
    public async Task<ApiEventResponse> OnDeleteFeatureClick(IBridge bridge, ApiToolEventArguments e)
    {
        return await (new Desktop.DeleteFeature()).InitResponse(bridge, e, _sender);
    }

    [ServerToolCommand("mergefeatures")]
    public ApiEventResponse OnMergeFeatures(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new MergeFeatures()
            {
                ParentTool = _sender
            }
        };
    }

    [ServerToolCommand("cutfeatures")]
    public ApiEventResponse OnCutFeature(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new CutFeatures()
            {
                ParentTool = _sender
            }
        };
    }

    [ServerToolCommand("clipfeatures")]
    public ApiEventResponse OnClipFeature(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new ClipFeatures()
            {
                ParentTool = _sender
            }
        };
    }

    [ServerToolCommand("deleteselectedfeatures")]
    async public Task<ApiEventResponse> OnDeleteSelectedFeatures(IBridge bridge, ApiToolEventArguments e, ILocalizer<DeleteSelectedFeatures> localizer)
    {
        return await (new Desktop.Advanced.DeleteSelectedFeatures()).InitResponse(bridge, e, _sender, localizer);
    }

    [ServerToolCommand("massattributation")]
    public ApiEventResponse OnMassatributation(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new MassAttributation()
            {
                ParentTool = _sender
            }
        };
    }

    [ServerToolCommand("features_transfer")]
    public ApiEventResponse OnCopySelectedFeatures(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new TransferFeatures()
            {
                ParentTool = _sender
            }
        };
    }

    #endregion
}

