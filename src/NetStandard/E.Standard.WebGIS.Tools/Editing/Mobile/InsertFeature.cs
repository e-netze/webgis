using E.Standard.Extensions.Collections;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebGIS.Tools.Editing.Services;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using E.Standard.WebMapping.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Mobile;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true, ClientDeviceDependent = true, MapCrsDependent = true)]
[ToolId("webgis.tools.editing.insertfeature")]
public class InsertFeature : IApiServerToolAsync, IApiChildTool, IApiToolPersistenceContext, IApiPostRequestEvent
{
    private readonly string EditMaskContainerId = "webgis-edit-insert-edit-mask-holder";
    private readonly NewFeatureService _newFeatureService;

    public InsertFeature()
    {
        _newFeatureService = new NewFeatureService();
    }

    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        if (bridge == null)
        {
            throw new ArgumentNullException(nameof(bridge));
        }

        string editThemeId = e["edit-theme-insert"];
        int mapCrsId = e.GetInt(Edit.EditMapCrsId);

        ApiEventResponse editThemeResponse = null;
        if (!String.IsNullOrWhiteSpace(editThemeId))
        {
            EditThemeDefinition editThemeDef = ApiToolEventArguments.FromArgument<EditThemeDefinition>(editThemeId);
            editThemeDef.Init(bridge);
            editThemeResponse = await _newFeatureService
                                     .EditFeatureMaskReponse(bridge,
                                                             this,
                                                             editThemeDef,
                                                             mapCrsId,
                                                             e,
                                                             e.UseMobileBehavior() == false ? $"#{EditMaskContainerId}" : null);
        }

        List<UINameValue> customItems = new List<UINameValue>();
        var favItems = await bridge.GetUserFavoriteItemsAsync(new Edit(), "Edit");
        foreach (var favItem in favItems)
        {
            try
            {
                var editTheme = bridge.GetEditTheme(favItem.Split(',')[0], favItem.Split(',')[2]);

                if (editTheme != null)
                {
                    customItems.Add(new UINameValue()
                    {
                        value = favItem.Split(',')[0] + "," + editTheme.LayerId + "," + favItem.Split(',')[2],
                        name = editTheme.Name,
                        category = "Favoriten"
                    });
                }
            }
            catch { }
        }

        List<IUIElement> uiElements = new List<IUIElement>();
        uiElements.AddRange(new IUIElement[] {
                new UIHidden(){
                    id=Edit.EditAllThemesId,
                    css=UICss.ToClass(new string[]{UICss.ToolParameter,UICss.AutoSetterAllEditThemes})
                },
                new UIHidden(){
                    id=Edit.EditMapScaleId,
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapScale})
                },
                new UIHidden(){
                    id=Edit.EditMapCrsId,
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapCrsId})
                },
                new UIEditThemeCombo() {
                    id="edit-theme-insert",
                    css=UICss.ToClass(new string[]{UICss.ToolParameterPersistent, UICss.ToolInitializationParameter }),
                    customitems=customItems.ToArray(),
                    onchange="editthemechanged",
                    db_rights="i"
                }
            });

        if (e.UseMobileBehavior())
        {
            uiElements.AddRange(new IUIElement[]
            {
                new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setparenttool) {
                    text="Abbrechen",
                    css=UICss.ToClass(new string[]{UICss.CancelButtonStyle})
                },
                new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.showtoolmodaldialog) {
                    text="Sachdaten & Speichern...",
                    css=UICss.ToClass(new string[]{UICss.DefaultButtonStyle, UICss.EventTriggerOnSketchClosed})
                }
            });
        }
        else
        {
            uiElements.AddRange(new IUIElement[]{
                new UIDiv()
                {
                    id = EditMaskContainerId
                },
                new UIBreak(2),
                new UISketchInfoContainer()
                {

                }
            });
        }

        ApiEventResponse uiResponse = new ApiEventResponse()
        {
            UIElements = uiElements.ToArray(),
            UISetters = new IUISetter[] {
                new UIPersistentParametersSetter(this)
            }
        };

        if (e.UseMobileBehavior())
        {
            return uiResponse.Prepend(editThemeResponse);
        }
        else
        {
            return uiResponse.Append(editThemeResponse);
        }
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

    public string Name
    {
        get { return "Neues Objekt erstellen"; }
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
        get { return "Objekte in der Karte erstellen"; }
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

    #region Commands

    [ServerToolCommand("editthemechanged")]
    [ServerToolCommand("init")]
    async public Task<ApiEventResponse> OnEditThemeChanged(IBridge bridge, ApiToolEventArguments e)
    {
        string editThemeId = e["edit-theme-insert"];
        int mapCrsId = e.GetInt(Edit.EditMapCrsId);

        EditThemeDefinition editThemeDef = ApiToolEventArguments.FromArgument<EditThemeDefinition>(editThemeId);
        editThemeDef.Init(bridge);

        ApiEventResponse response = await _newFeatureService
                                         .EditFeatureMaskReponse(bridge,
                                                                 this,
                                                                 editThemeDef,
                                                                 mapCrsId,
                                                                 e,
                                                                 e.UseMobileBehavior() == false ? $"#{EditMaskContainerId}" : null);

        response.UISetters = response.UISetters.TryAppendItems(new IUISetter[] { new UIPersistentParametersSetter(this) });
        return response;
    }

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

        var response = await OnEditThemeChanged(bridge, e);

        if (e["_select"] == "true" && editEnvironment.CommitedObjectIds != null && editEnvironment.CommitedObjectIds.Count() > 0)
        {
            // Select feature
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

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e);
        var feature = editEnvironment.GetFeature(bridge, e);

        _newFeatureService.KeepAllAttributes(response, e, feature);

        return response;
    }

    [ServerToolCommand("save-and-continue-sketch")]
    async public Task<ApiEventResponse> OnSaveAndContinueSketch(IBridge bridge, ApiToolEventArguments e)
    {
        var response = await OnSave(bridge, e);

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e);
        var feature = editEnvironment.GetFeature(bridge, e);

        _newFeatureService.AddLastSketchPoint(response, feature);

        return response;
    }

    [ServerToolCommand("save-and-continue-sketch-keep-attributes")]
    async public Task<ApiEventResponse> OnSaveAndContinueSketchKeepAttributes(IBridge bridge, ApiToolEventArguments e)
    {
        var response = await OnSave(bridge, e);

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e);
        var feature = editEnvironment.GetFeature(bridge, e);

        _newFeatureService.AddLastSketchPoint(response, feature);
        _newFeatureService.KeepAllAttributes(response, e, feature);

        return response;
    }

    async public Task<ApiEventResponse> OnEditServiceSave(IBridge bridge, ApiToolEventArguments e)
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId)
        };
        var feature = editEnvironment.GetFeature(bridge, e);
        var editTheme = editEnvironment[e];

        if (editTheme == null)
        {
            throw new Exception($"Unknown edit-theme ({e["_editfield_edittheme_def"]})");
        }

        int fromSrsId = feature.Shape.SrsId;
        int toSrsId = editTheme.SrsId(feature.Shape.SrsId);

        if (fromSrsId > 0 && toSrsId > 0 && fromSrsId != toSrsId)
        {
            using (var transformer = bridge.GeometryTransformer(fromSrsId, toSrsId))
            {
                transformer.Transform(feature.Shape);
            }
        }

        await editEnvironment.InsertFeature(editTheme, feature);

        return new ApiRawJsonEventResponse(new
        {
            success = true,
            refreshservices = new string[]
            {
                e["serviceid"]
            }
        });
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

    #region Helper


    #endregion

    #region IApiPostRequestEvent

    public Task<ApiEventResponse> PostProcessEventResponseAsync(IBridge bridge, ApiToolEventArguments e, ApiEventResponse response)
    {
        return new Edit().PostProcessEventResponseAsync(bridge, e, response);
    }

    #endregion
}
