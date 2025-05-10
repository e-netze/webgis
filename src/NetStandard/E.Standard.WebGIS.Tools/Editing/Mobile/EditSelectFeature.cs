using E.Standard.Localization.Abstractions;
using E.Standard.Localization.Reflection;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebGIS.Tools.Editing.Sorting;
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
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static E.Standard.WebMapping.Core.CoreApiGlobals;

namespace E.Standard.WebGIS.Tools.Editing.Mobile;

[AdvancedToolProperties(MapCrsDependent = true)]
public abstract class EditSelectFeature<T> : IApiServerToolLocalizableAsync<Edit>,
                                             IApiChildTool,
                                             IApiToolPersistenceContext,
                                             IApiPostRequestEvent
{
    internal readonly string EditMaskContainerId = "webgis-edit-update-delete-edit-mask-holder";
    internal readonly string EditWarningsContainerId = "webgis-edit-upate-edit-warnings-holder";
    private const string EditFeatureSelectionContainerId = "webgis-edit-update-edit-feature-container-holder";

    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Edit> localizer)
    {
        List<UINameValue> customItems = new List<UINameValue>(new UINameValue[]{
                        new UINameValue(){
                            name="Sichtbare Themen", value="#"
                        }
        });

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
                        category = localizer.Localize("favorites")
                    });
                }
            }
            catch { }
        }

        var editWarnings = new UIDiv()
        {
            id = EditWarningsContainerId
        };

        var editThemeId = e["edit-theme-select"];
        if (!String.IsNullOrEmpty(editThemeId) && editThemeId != "#")
        {
            EditThemeDefinition editThemeDef = ApiToolEventArguments.FromArgument<EditThemeDefinition>(editThemeId);
            editThemeDef.Init(bridge);
            Environment.EditEnvironment.AppendLayerWarnings(editWarnings, editThemeDef, localizer);
        }

        List<IUIElement> uiElements = new List<IUIElement>();
        uiElements.AddRange(new IUIElement[] {
                new UIHidden(){
                    id=Edit.EditAllThemesId,
                    css=UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.AutoSetterAllEditThemes })
                },
                new UIHidden(){
                    id=Edit.EditMapScaleId,
                    css=UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.AutoSetterMapScale})
                },
                new UIHidden(){
                    id=Edit.EditMapCrsId,
                    css=UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.AutoSetterMapCrsId})
                },
                new UIEditThemeCombo() {
                    id="edit-theme-select",
                    //css=UICss.ToClass(new string[]{UICss.ToolParameter}),
                    css=UICss.ToClass(new string[]{ UICss.ToolParameterPersistent, UICss.ToolInitializationParameter }),
                    customitems=customItems.ToArray(),
                    onchange = "editthemechanged",
                    db_rights = this.DbRightsString()
                },
                editWarnings,
                new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                {
                    text = localizer.Localize("cancel"),
                    css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.ButtonIcon, UICss.OptionButtonStyle }),
                    style="width:300px;margin-top:10px"
                },
                new UIToolUndoButton(new Edit().GetType(), localizer.Localize("mobile.undo"))
                {
                    id="webgis-edit-undo-button"
                }
            });

        if (e.UseMobileBehavior() == false)
        {
            uiElements.Add(new UIDiv()
            {
                id = EditMaskContainerId
            });

            uiElements.Add(new UIDiv()
            {
                id = EditFeatureSelectionContainerId
            });
        }

        return new ApiEventResponse()
        {
            UIElements = uiElements.ToArray(),
            UISetters = new IUISetter[] {
                new UIPersistentParametersSetter(this)
            }
        };
    }

    async public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<Edit> localizer)
    {
        int crsId = e.GetInt(Edit.EditMapCrsId);
        var eventResult = await DoOnEventAsync(this, bridge, e);
        ApiEventResponse response = eventResult.response;
        EditFeatureDefinition editFeatureDef = eventResult.editFeatureDef;
        if (editFeatureDef != null)
        {
            // für etwaige Postevents wichtig
            e["_editfield_edittheme_def"] = ApiToolEventArguments.ToArgument(editFeatureDef.ToEditThemeDefinition());
        }

        if (editFeatureDef != null)
        {
            if (typeof(T) == typeof(DeleteFeature))
            {
                var tool = new DeleteFeature();
                await DeleteFeature.InitAsync(tool, bridge, e, editFeatureDef, response, localizer, crsId);
                tool.ParentTool = this;
                response.ActiveTool = tool;
            }
            else if (typeof(T) == typeof(UpdateFeature))
            {
                var tool = new UpdateFeature();
                await UpdateFeature.InitAsync(tool, bridge, e, editFeatureDef, response, localizer, crsId);
                tool.ParentTool = this;
                response.ActiveTool = tool;
            }
        }

        return response;
    }

    #endregion

    #region IApiTool Member

    public ToolType Type
    {
        get { return WebMapping.Core.Api.ToolType.click; }
    }

    public ToolCursor Cursor
    {
        get
        {
            return ToolCursor.Custom_Selector;
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

    #region IApiChildTool Member

    public IApiTool ParentTool
    {
        get;
        internal set;
    }

    #endregion

    #region IApiButton Member

    abstract public string Name
    {
        get;
    }

    public string Container
    {
        get { return String.Empty; }
    }

    public string Image
    {
        get { return String.Empty; }
    }

    abstract public string ToolTip
    {
        get;
    }

    public bool HasUI
    {
        get { return true; }
    }

    #endregion

    #region IApiPostRequestEvent

    async public Task<ApiEventResponse> PostProcessEventResponseAsync(IBridge bridge, ApiToolEventArguments e, ApiEventResponse response)
    {
        if (typeof(T).GetInterfaces().Any(i => i == typeof(IApiPostRequestEvent)))
        {
            var postRequestEvent = Activator.CreateInstance<T>() as IApiPostRequestEvent;
            return await postRequestEvent.PostProcessEventResponseAsync(bridge, e, response);
        }

        return response;
    }

    #endregion

    #region Commands

    [ServerToolCommand("init")]
    [ServerToolCommand("editthemechanged")]
    public ApiEventResponse OnEditThemeChanged(IBridge bridge, ApiToolEventArguments e, ILocalizer<Edit> localizer)
    {
        string editThemeId = e["edit-theme-select"];

        EditThemeDefinition editThemeDef = ApiToolEventArguments.FromArgument<EditThemeDefinition>(editThemeId);
        editThemeDef.Init(bridge);

        var editWarnings = new UIDiv()
        {
            id = EditWarningsContainerId,
            target = $"#{EditWarningsContainerId}"
        };

        if (editThemeId != "#")
        {
            Environment.EditEnvironment.AppendLayerWarnings(editWarnings, editThemeDef, localizer);
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] { editWarnings }
            //,UISetters = new IUISetter[] { new UIPersistentParametersSetter(this) }
        };
    }

    [ServerEventHandler(ServerEventHandlers.OnUpdateCombo)]
    public Task<ApiEventResponse> OnUpdateCombo(IBridge bridge, ApiToolEventArguments e)
    {
        return new EditService().OnUpdateCombo(bridge, e);
    }

    #endregion

    #region Helper

    private string DbRightsString()
    {
        if (typeof(T) == typeof(DeleteFeature))
        {
            return "d";
        }
        else if (typeof(T) == typeof(UpdateFeature))
        {
            return "u";
        }

        return null;
    }

    #endregion

    #region Static Members 

    async internal static Task<(ApiEventResponse response, EditFeatureDefinition editFeatureDef)> DoOnEventAsync(IApiTool sender, IBridge bridge, ApiToolEventArguments e)
    {
        double mapScale = e.GetDouble(Edit.EditMapScaleId);
        string allThemes = e[Edit.EditAllThemesId];
        string editTheme = e["edit-theme-select"];
        int crsId = e.GetInt(Edit.EditMapCrsId);

        string menuItemValue = e.MenuItemValue;
        EditFeatureDefinition editFeatureDef = null;

        var isMobileDevice = e.UseMobileBehavior();

        if (!String.IsNullOrWhiteSpace(menuItemValue))
        {
            #region Edit Menu Selected Feature

            editFeatureDef = ApiToolEventArguments.FromArgument<EditFeatureDefinition>(menuItemValue);

            #endregion
        }
        else
        {
            #region Edit Clicked Feature

            var click = e.ToMapProjectedClickEvent();

            #region QueryFilter

            Envelope env = null;

            if (click.Sketch is Envelope)
            {
                env = new Envelope((Envelope)click.Sketch);
            }
            else
            {
                double toleranceX, toleranceY;
                toleranceX = toleranceY = 15.0 * mapScale / (96.0 / 0.0254);
                if (click.SRef.IsProjective == false)
                {
                    toleranceX = toleranceX * ToDeg / WorldRadius * Math.Cos(click.Latitude * ToRad);
                    toleranceY = toleranceY * ToDeg / WorldRadius;
                }
                env = new Envelope(
                    click.WorldX - toleranceX, click.WorldY - toleranceY,
                    click.WorldX + toleranceX, click.WorldY + toleranceY);
            }

            ApiSpatialFilter filter = new ApiSpatialFilter()
            {
                QueryShape = env,
                FilterSpatialReference = click.SRef,
                QueryGeometry = true,
                FeatureSpatialReference = click.SRef,
            };

            #endregion

            #region Collect Edit Themes

            string[] editThemeStrings = new string[0];
            if (editTheme == "#")
            {
                foreach (string editThemeString in allThemes.Split(';'))
                {
                    if (editThemeString.EndsWith(",1"))
                    {
                        Array.Resize<string>(ref editThemeStrings, editThemeStrings.Length + 1);
                        editThemeStrings[editThemeStrings.Length - 1] = editThemeString;
                    }
                }
            }
            else if (editTheme.Contains(","))
            {
                editThemeStrings = new string[] { editTheme };
            }

            #endregion

            List<EditFeatureDefinition> featuresDef = new List<EditFeatureDefinition>();

            #region Query Themes

            foreach (string editThemeString in editThemeStrings)
            {
                EditThemeDefinition editThemeDef = ApiToolEventArguments.FromArgument<EditThemeDefinition>(editThemeString);
                editThemeDef.Init(bridge);

                var themeFeatures = await bridge.QueryLayerAsync(editThemeDef.ServiceId, editThemeDef.LayerId, filter);
                foreach (var themeFeature in themeFeatures)
                {
                    featuresDef.Add(
                        new EditFeatureDefinition()
                        {
                            ServiceId = editThemeDef.ServiceId,
                            LayerId = editThemeDef.LayerId,
                            FeatureOid = themeFeature.Oid,
                            EditThemeId = editThemeDef.EditThemeId,
                            EditThemeName = editThemeDef.EditThemeName,
                            Feature = themeFeature
                        });
                }
            }

            #endregion

            if (featuresDef.Count > 1)
            {
                featuresDef.Sort(new EditFeatureDefintionComparer(new Point(click.WorldX, click.WorldY)));

                if (!isMobileDevice)
                {
                    #region Transform Feature Geometry

                    foreach (var fdef in featuresDef.Where(f => f.Feature?.Shape != null))
                    {
                        using (var transformer = new GeometricTransformerPro(click.SRef, bridge.CreateSpatialReference(4326)))
                        {
                            var shape = fdef.Feature.Shape;
                            if (shape != null)
                            {
                                transformer.Transform(shape);
                            }
                        }
                    }

                    #endregion
                }

                List<UIElement> menuItems = new List<UIElement>();
                foreach (var fdef in featuresDef)
                {
                    string icon = null, legendValue = String.Empty;
                    var query = await bridge.GetFirstLayerQuery(fdef.ServiceId, fdef.LayerId);
                    if (query != null)
                    {
                        var legendImageUrl = await query.LegendItemImageUrlAsync(fdef.Feature, out legendValue);
                        if (!String.IsNullOrEmpty(legendImageUrl))
                        {
                            icon = bridge.AppRootUrl + legendImageUrl;
                        }
                    }

                    var menuItem = new UIMenuItem(sender, e)
                    {
                        text = fdef.EditThemeName,
                        text2 = legendValue,
                        subtext = FeatureSubtext(fdef.Feature),
                        value = ApiToolEventArguments.ToArgument(fdef),
                        icon = icon
                    };



                    if (!isMobileDevice && fdef.Feature?.Shape != null)
                    {
                        fdef.Feature.Attributes.Clear();  // Do not send all attributes to client with "highlight_feature"
                        menuItem.highlight_feature = bridge.ToGeoJson(new WebMapping.Core.Collections.FeatureCollection(fdef.Feature));
                    }

                    menuItems.Add(menuItem);
                }

                if (isMobileDevice)
                {
                    return (response: new ApiEventResponse()
                    {
                        UIElements = new IUIElement[] {
                            new UIMenu()
                                {
                                    target = isMobileDevice ? UIElementTarget.modaldialog.ToString() : $"#{ EditFeatureSelectionContainerId }",
                                    elements = menuItems.ToArray(),
                                    header = "Objekt auswahlen:"
                                }
                        }
                    }, editFeatureDef: null);
                }
                else
                {
                    return (response: new ApiEventResponse()
                    {
                        UIElements = new IUIElement[] {
                        new UIDiv()
                        {
                            target = isMobileDevice ? UIElementTarget.modaldialog.ToString() : $"#{ EditFeatureSelectionContainerId }",
                            elements = new IUIElement[]{
                                new UIDiv()
                                {
                                    css = UICss.ToClass(new string[] { "webgis-info" }),
                                    target = $"#{ EditFeatureSelectionContainerId }",
                                    elements = new IUIElement[]
                                    {
                                        new UILiteral() { literal="Die Abfrage ergab mehrere Treffer bei den bearbeitbaren Themen. Bitte wählen Sie ein Objekt aus, um mit der Bearbeitung fortzufahren." },
                                    },

                                },
                                new UIMenu()
                                {
                                    elements = menuItems.ToArray(),
                                    header = "Objekt auswahlen:"
                                }
                            }
                        }
                    },
                    }, editFeatureDef: null);
                }
            }
            else if (featuresDef.Count == 1)
            {
                editFeatureDef = featuresDef[0];
            }
            else if (featuresDef.Count == 0)
            {
                if (!isMobileDevice)
                {
                    return (response: new ApiEventResponse()
                    {
                        UIElements = new IUIElement[] {
                        new UIDiv()
                        {
                            css = UICss.ToClass(new string[] { "webgis-info" }),
                            target = $"#{ EditFeatureSelectionContainerId }",
                            elements = new IUIElement[]
                            {
                                new UILiteral() { literal="Die Abfrage ergab keine Treffer bei den bearbeitbaren Themen."}
                            }
                        }
                    },
                    }, editFeatureDef: null);
                }
            }
            #endregion
        }

        ApiEventResponse response = new ApiEventResponse();

        return (response: response, editFeatureDef: editFeatureDef);
    }

    static private string FeatureSubtext(WebMapping.Core.Feature feature, int maxAttributes = 3)
    {
        if (feature == null || feature.Attributes == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        int count = 0;
        foreach (var attribute in feature.Attributes)
        {
            if (String.IsNullOrWhiteSpace(attribute.Value))
            {
                continue;
            }

            sb.Append(attribute.Name + ": " + attribute.Value + " ");

            count++;
            if (count >= maxAttributes)
            {
                break;
            }
        }
        return sb.ToString();
    }

    #endregion
}

[Export(typeof(IApiButton))]
[AdvancedToolProperties(VisFilterDependent = true, ClientDeviceDependent = true, MapCrsDependent = true)]
[ToolId("webgis.tools.editing.editselectupdatefeature")]
[LocalizationNamespace("tools.editing.updatefeature")]
public class EditSelectUpdateFeature : EditSelectFeature<UpdateFeature>
{
    public override string Name => "Update Existing Object";
    public override string ToolTip => "Edit objects on the map";
}

[Export(typeof(IApiButton))]
[AdvancedToolProperties(VisFilterDependent = true, ClientDeviceDependent = true, MapCrsDependent = true)]
[ToolId("webgis.tools.editing.editselectdeletefeature")]
[LocalizationNamespace("tools.editing.deletefeature")]
public class EditSelectDeleteFeature : EditSelectFeature<DeleteFeature>
{
    public override string Name => "Delete Existing Object";
    public override string ToolTip => "Delete objects on the map";
}
