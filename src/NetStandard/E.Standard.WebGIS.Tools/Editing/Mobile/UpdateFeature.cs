using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebGIS.Tools.Editing.Services;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Mobile;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true,
                        ClientDeviceDependent = true,
                        MapCrsDependent = true,
                        MapImageSizeDependent = true)]
[ToolId("webgis.tools.editing.updatefeature")]
public class UpdateFeature : IApiServerTool, IApiChildTool, IApiToolConfirmation, IApiToolPersistenceContext, IApiPostRequestEvent
{
    internal static readonly string EditMaskContainerId = "webgis-edit-update-delete-edit-mask-holder";
    private readonly UpdateFeatureService _updateFeatureService;

    public UpdateFeature()
    {
        this.Operation = EditOperation.Update;
        _updateFeatureService = new UpdateFeatureService(this);
    }

    async internal static Task InitAsync(UpdateFeature tool,
                                         IBridge bridge,
                                         ApiToolEventArguments e,
                                         EditFeatureDefinition editFeatureDef,
                                         ApiEventResponse response,
                                         int mapCrsId = 4326,
                                         EditOperation editOperation = EditOperation.Update)
    {
        tool.Operation = editOperation;

        //EditEnvironment editEnvironment = new EditEnvironment(bridge, (EditThemeDefinition)null);
        EditEnvironment editEnvironment = new EditEnvironment(bridge, editFeatureDef.ToEditThemeDefinition());
        EditEnvironment.EditTheme editTheme = editEnvironment[editFeatureDef.EditThemeId];
        if (editTheme == null)
        {
            throw new ArgumentException("Can't find edit theme definition: " + editFeatureDef.EditThemeId);
        }

        #region Query Feature

        int srsId = editTheme.SrsId(mapCrsId);

        ApiOidFilter filter = new ApiOidFilter(editFeatureDef.FeatureOid)
        {
            QueryGeometry = true,
            FeatureSpatialReference = bridge.CreateSpatialReference(srsId),
            Fields = QueryFields.All
        };

        var editFeatures = await bridge.QueryLayerAsync(editFeatureDef.ServiceId, editFeatureDef.LayerId, filter);
        var query = await bridge.GetFirstLayerQuery(editFeatureDef.ServiceId, editFeatureDef.LayerId);

        if (editFeatures.Count == 1)
        {
            var editFeature = editFeatures[0];
            if (editFeature.Shape is Point)
            {
                tool.Type = ToolType.sketch0d;
            }
            else if (editFeature.Shape is Polyline)
            {
                tool.Type = ToolType.sketch1d;
            }
            else if (editFeature.Shape is Polygon)
            {
                tool.Type = ToolType.sketch2d;
            }
            else
            {
                throw new Exception("Unknown geometry type to edit");
            }

            response.Sketch = editFeature.Shape;
            if (response.Sketch != null)
            {
                response.Sketch.SrsId = srsId;

                if (!String.IsNullOrEmpty(editTheme.DbRights) && !editTheme.DbRights.Contains("g"))
                {
                    response.SketchReadonly = true;
                }

                if ((srsId != mapCrsId || e.MapCrsIsDynamic) && filter.FeatureSpatialReference != null)
                {
                    response.Sketch.SrsP4Parameters = filter.FeatureSpatialReference.Proj4;
                }

                if (tool is DeleteFeature)
                {
                    response.CloseSketch = true;
                    response.SketchReadonly = true;
                }
                else
                {
                    response.CloseSketch = tool.Type != ToolType.sketch1d;   // Polylinen nicht abschließen, damit man sie weiterzeichnen kann (Alice überfordert "Undo Sketch abschließen" nach dem auswählen)
                }
            }

            editFeature.SetGlobalOid(query);

            EditEnvironment.UIEditMask mask = await editTheme.ParseMask(bridge,
                                                                        editFeatureDef.ToEditThemeDefinition(),
                                                                        tool.Operation,
                                                                        editFeature,
                                                                        useMobileBehavoir: e.UseMobileBehavior());

            // For PostEvent!!
            e["_edittheme_oid"] = editFeature.Oid.ToString();

            if (e.UseMobileBehavior() == false)
            {
                mask.UIElements.First().target = UIElementTarget.tool_modaldialog_noblocking.ToString();
                //mask.UIElements[0].target = "#" + EditMaskContainerId;
            }

            response.UIElements = mask.UIElements;
            response.UISetters = mask.UISetters;

            response.ApplyEditingTheme = new EditingThemeDefDTO(editFeatureDef.EditThemeId, editFeatureDef.ServiceId);
        }
        else
        {
            throw new ArgumentException("Can't query edit feature");
        }

        tool.Name = editFeatureDef.EditThemeName + " bearbeiten";

        #endregion
    }

    protected EditOperation Operation { get; set; }

    #region IApiServerTool Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        //EditFeatureDefinition editFeatureDef = ApiToolEvent.FromArgument<EditFeatureDefinition>(e.InitalAgrument);

        List<IUIElement> uiElements = new List<IUIElement>(new IUIElement[]
            {
                new UIHidden(){
                    id=Edit.EditMapScaleId,
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapScale})
                },
                new UIHidden(){
                    id=Edit.EditMapCrsId,
                    css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapCrsId})
                },
                new UIButton(UIButton.UIButtonType.clientbutton,ApiClientButtonCommand.setparenttool) {
                    text="Abbrechen",
                    css=UICss.ToClass(new string[]{UICss.CancelButtonStyle})
                }
            });

        AddUIElements(uiElements);

        return new ApiEventResponse()
        {
            UIElements = uiElements.ToArray()
        };
    }

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        return null;
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
            return ToolCursor.Custom_Pen;
        }
    }

    #endregion

    #region Virtual Memebers

    protected virtual void AddUIElements(List<IUIElement> uiElements)
    {
        uiElements.AddRange(new IUIElement[]
        {
                //new UIButton(UIButton.UIButtonType.servertoolcommand, "delete") {
                //    text="Löschen"
                //},
                new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.showtoolmodaldialog) {
                    text="Sachdaten & Speichern...",
                    css=UICss.ToClass(new string[]{UICss.DefaultButtonStyle})
                }
        });
    }

    #endregion

    #region IApiButton Member

    public string Name
    {
        get;
        protected set;
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
        get { return String.Empty; }
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
        return await _updateFeatureService.SaveFeature(bridge, e, new EditSelectUpdateFeature()
        {
            ParentTool = new Edit()
        });
    }

    [ServerToolCommand("delete")]
    [ToolCommandConfirmation("Soll das Objekt wirklich gelöscht werden?", ApiToolConfirmationType.YesNo, ApiToolConfirmationEventType.ButtonClick)]
    async public Task<ApiEventResponse> OnDelete(IBridge bridge, ApiToolEventArguments e)
    {
        var response = await _updateFeatureService.DeleteFeature(bridge, e, new EditSelectUpdateFeature()
        {
            ParentTool = new Edit()
        });

        return response;
    }

    async public Task<ApiEventResponse> OnEditServiceDelete(IBridge bridge, ApiToolEventArguments e)
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e);
        var feature = editEnvironment.GetFeature(bridge, e);
        var editTheme = editEnvironment[e];

        await editEnvironment.DeleteFeature(editTheme, feature);

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

    internal const string EditAttributesFieldPrefix = "editattributesfield";

    [ServerToolCommand("editattributes")]
    async public Task<ApiEventResponse> OnEditAttributes(IBridge bridge, ApiToolEventArguments e)
    {
        return await _updateFeatureService.EditMaskResponse(bridge, e, EditOperation.UpdateAttribures, editFieldPrefix: Mobile.UpdateFeature.EditAttributesFieldPrefix);
    }

    [ServerToolCommand("editattributes-save")]
    async public Task<ApiEventResponse> OnEditAttributesSave(IBridge bridge, ApiToolEventArguments e)
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e, editFieldPrefix: EditAttributesFieldPrefix);

        var editTheme = editEnvironment[e];
        var feature = editEnvironment.GetFeature(bridge, e);
        var editThemeDef = editEnvironment.EditThemeDefinition;

        await editEnvironment.UpdateFeature(editTheme, feature);

        var oid = e[$"_{EditAttributesFieldPrefix}_globaloid"].ParseFeatureGlobalOid();

        ApiOidFilter filter = new ApiOidFilter(feature.Oid)
        {
            QueryGeometry = false,
            Fields = QueryFields.All
        };

        var editedFeatures = await bridge.QueryLayerAsync(editThemeDef.ServiceId, editThemeDef.LayerId, filter);
        var query = await bridge.GetQuery(oid.serviceId, oid.queryId);
        var sRef = bridge.CreateSpatialReference(4326);

        return new ApiEventResponse()
        {
            ReplaceQueryFeatures = editedFeatures.Count == 1 ? editedFeatures : null,
            ReplaceFeaturesQueries = await bridge.GetLayerQueries(query),
            ReplaceFeatureSpatialReference = sRef,
            RefreshServices = new string[] { editThemeDef.ServiceId },
            UIElements = new IUIElement[]
            {
                new UIEmpty() {
                    target = UIElementTarget.modaldialog.ToString(),
                }
            }
        };
    }

    #endregion

    #region IApiToolConfirmation Member

    virtual public ApiToolConfirmation[] ToolConfirmations
    {
        get
        {
            List<ApiToolConfirmation> confirmations = new List<ApiToolConfirmation>();
            confirmations.AddRange(ApiToolConfirmation.CommandComfirmations(typeof(UpdateFeature)));
            //confirmations.Add(new ApiToolConfirmation()
            //{
            //    Command = ApiClientButtonCommand.setparenttool.ToString(),
            //    Message = "Möchten Sie wirklich die Bearbeitung beenden? Alle gemachten Änderungen gehen verloren.",
            //    Type = ApiToolConfirmationType.YesNo,
            //    EventType = ApiToolConfirmationEventType.ButtonClick
            //});
            return confirmations.ToArray();
        }
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
}
