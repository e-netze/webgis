using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Editing.Mobile.Advanced;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.Identify;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing;

public class EditToolServiceMobile : IEditToolService
{
    private readonly IApiTool _sender;

    public EditToolServiceMobile(IApiTool sender)
    {
        _sender = sender;
    }

    #region IEditService

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        List<IUIElement> uiElements = new List<IUIElement>(
            new IUIElement[] {
                new UIButton(UIButton.UIButtonType.servertoolcommand,"newfeature") {
                    text="Neues Objekt anlegen",
                    css=UICss.ToClass(new string[]{ UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                    icon = UIButton.ToolResourceImage(typeof(Edit), "insert")
                },
                new UIButton(UIButton.UIButtonType.servertoolcommand,"updatefeature") {
                    text="Bestehendes Objekt bearbeiten",
                    css=UICss.ToClass(new string[]{  UICss.CancelButtonStyle, UICss.OptionRectButtonStyle}),
                    icon = UIButton.ToolResourceImage(typeof(Edit), "update")
                },
                new UIButton(UIButton.UIButtonType.servertoolcommand,"deletefeature") {
                    text="Bestehendes Objekt löschen",
                    css=UICss.ToClass(new string[]{  UICss.CancelButtonStyle, UICss.OptionRectButtonStyle}),
                    icon = UIButton.ToolResourceImage(typeof(Edit), "delete")
                },
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
                //new UIHidden(){
                //    id="edit-theme-select",
                //    value="#",  // all visible edit themes
                //    css=UICss.ToClass(new string[]{UICss.ToolParameter })
                //},
            });

        var selectionDiv = new UIDiv()
        {
            style = "background-color: #ccffff;padding: 5px 2px;margin: 5px -2px;border-radius: 5px;"
        };
        uiElements.Add(selectionDiv);

        bool editableSelection = false;
        if (e.SelectionInfo != null)
        {
            var editTheme = e.EditThemeFromSelection(bridge);
            if (editTheme != null)
            {
                editableSelection = true;
                var features = await e.FeaturesFromSelectionAsync(bridge, QueryFields.Shape);

                if (e.SelectionInfo.GeometryType == "point")
                {
                    #region Point (simple)

                    if (features.Count == 1 && features[0].Shape != null)
                    {
                        if (features.Count > 0)
                        {
                            selectionDiv.AddChild(new UITitle() { label = "Ausgewählte Objekte" });

                            var editThemeDef = e.EditFeatureDefinitionFromSelection(bridge, features[0]);
                            if (!String.IsNullOrWhiteSpace(editThemeDef?.EditThemeName))
                            {
                                selectionDiv.AddChild(new UILabel() { label = editThemeDef.EditThemeName + " [" + features.Count + "]:" });
                                selectionDiv.AddChild(new UIBreak(2));
                            }
                        }

                        selectionDiv.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "updateselectedfeature")
                        {
                            text = "Objekt bearbeiten",
                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                            icon = UIButton.ToolResourceImage(typeof(Edit), "update")
                        });
                        selectionDiv.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "deleteselectedfeature")
                        {
                            text = "Objekt löschen",
                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                            icon = UIButton.ToolResourceImage(typeof(Edit), "delete")
                        });
                    }

                    #endregion
                }
                else
                {
                    #region Other Geometries

                    if (features.Count > 0)
                    {
                        selectionDiv.AddChild(new UITitle() { label = "Ausgewählte Objekte" });

                        var editThemeDef = e.EditFeatureDefinitionFromSelection(bridge, features[0]);
                        if (!String.IsNullOrWhiteSpace(editThemeDef?.EditThemeName))
                        {
                            selectionDiv.AddChild(new UILabel() { label = editThemeDef.EditThemeName + " [" + features.Count + "]:" });
                            selectionDiv.AddChild(new UIBreak(2));
                        }
                    }

                    if (features.Count == 1 && features[0].Shape != null)
                    {
                        selectionDiv.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "updateselectedfeature")
                        {
                            text = "Objekt bearbeiten",
                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                            icon = UIButton.ToolResourceImage(typeof(Edit), "update")
                        });
                        selectionDiv.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "deleteselectedfeature")
                        {
                            text = "Objekt löschen",
                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                            icon = UIButton.ToolResourceImage(typeof(Edit), "delete")
                        });

                        var shape = features[0].Shape;
                        if (shape.IsMultipart)
                        {
                            selectionDiv.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "explodefeature")
                            {
                                text = "Multipart auftrennen (explode)",
                                css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                                icon = UIButton.ToolResourceImage(typeof(Edit), "explode")
                            });
                        }
                        selectionDiv.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "cutfeature")
                        {
                            text = "Objekt teilen (cut)",
                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                            icon = UIButton.ToolResourceImage(typeof(Edit), "cut")
                        });
                        selectionDiv.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "clipfeature")
                        {
                            text = "Objekt Ausschneiden (clip)",
                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                            icon = UIButton.ToolResourceImage(typeof(Edit), "clip")
                        });
                    }
                    else if (features.Count > 1)
                    {
                        selectionDiv.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "mergefeatures")
                        {
                            text = "Zusammenführen (merge)",
                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                            icon = UIButton.ToolResourceImage(typeof(Edit), "merge")
                        });
                    }

                    #endregion
                }

                if (features.Count > 1 && editTheme.DbRights.Contains("m"))  // Massenattributierung
                {
                    selectionDiv.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "massattributation")
                    {
                        text = "Massenattributierung",
                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                        icon = UIButton.ToolResourceImage(typeof(Edit), "mass")
                    });
                }
            }
        }

        if (!editableSelection)
        {
            selectionDiv.AddChild(new UILabel()
            {
                label = "Zur Zeit sind keine Objekte ausgewählt. Für ausgwählte Objekte stehen noch weitere Bearbeitunsfunktionen zur Verfügung (merge, explode, cut)",
                style = "padding:5px"
            });
        }

        selectionDiv.AddChild(new UIBreak(2));
        selectionDiv.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "selection-tool")
        {
            text = "mit Auswahlwerkzeug auswählen...",
            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
            icon = UIButton.ToolResourceImage(typeof(Edit), "selecteiontool")
        });
        selectionDiv.AddChild(new UIBreak());
        selectionDiv.AddChild(new UILabel()
        {
            label = "Oder einfach in die Karte klicken, um ein Objekt auszuwählen",
            style = "padding:5px"
        });

        uiElements.Add(new UIToolUndoButton(this.GetType(), "Rückgängig: Bearbeitungsschritt")
        {
            id = "webgis-edit-undo-button"
        });

        return new ApiEventResponse()
        {
            UIElements = uiElements.ToArray()
        };
    }

    [ServerToolCommand("box")]
    async public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        int crsId = e.GetInt(Edit.EditMapCrsId);

        e["edit-theme-select"] = "#";  // alle Themen

        var eventResult = await Mobile.EditSelectFeature<Edit>.DoOnEventAsync(_sender, bridge, e);
        ApiEventResponse response = eventResult.response;
        EditFeatureDefinition editFeatureDef = eventResult.editFeatureDef;

        if (editFeatureDef != null)
        {
            // Select feature
            var service = await bridge.GetService(editFeatureDef.ServiceId);
            var layer = service?.Layers?.Where(l => l.Id == editFeatureDef.LayerId).FirstOrDefault();
            var query = await bridge.GetFirstLayerQuery(editFeatureDef.ServiceId, editFeatureDef.LayerId);

            if (query != null && layer != null)
            {
                var sRef = bridge.CreateSpatialReference(crsId);

                var filter = new ApiQueryFilter();
                filter.FeatureSpatialReference = sRef;
                filter.QueryItems["#oid#"] = editFeatureDef.FeatureOid.ToString();

                var selectedFeature = await bridge.QueryLayerAsync(editFeatureDef.ServiceId, editFeatureDef.LayerId, filter);

                if (selectedFeature.Count == 1)
                {
                    response = new ApiFeaturesEventResponse()
                    {
                        Features = selectedFeature,
                        Query = query,
                        Filter = filter,
                        FeatureSpatialReference = sRef,
                        ZoomToResults = false,
                        SelectResults = true,
                        ActiveTool = _sender
                    };
                }
            }
        }

        if (response.UIElements != null)
        {
            var uiMenu = response.UIElements.FindRecursive<UIMenu>().FirstOrDefault();
            if (uiMenu != null)
            {
                //
                // Nur das Auswahlmenü als Dialog anzeigen
                //
                uiMenu.target = UIElementTarget.modaldialog.ToString();

                response.UIElements = new IUIElement[] { uiMenu };
            }
        }

        return response;
    }

    #endregion

    #region Commands

    [ServerToolCommand("autocomplete")]
    public Task<ApiEventResponse> OnAutocomplete(IBridge bridge, ApiToolEventArguments e)
    {
        return new EditService().OnAutocomplete(bridge, e);
    }

    [ServerEventHandler(ServerEventHandlers.OnUpdateCombo)]
    public Task<ApiEventResponse> OnUpdateCombo(IBridge bridge, ApiToolEventArguments e)
    {
        return new EditService().OnUpdateCombo(bridge, e);
    }

    [ServerToolCommand("newfeature")]
    public ApiEventResponse OnNewFeature(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new Mobile.InsertFeature()
            {
                ParentTool = _sender
            }
        };
    }

    [ServerToolCommand("updatefeature")]
    public ApiEventResponse OnUpdateFeature(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new Mobile.EditSelectUpdateFeature()
            {
                ParentTool = _sender
            }
        };
    }

    [ServerToolCommand("updateselectedfeature")]
    async public Task<ApiEventResponse> OnUpdateSelectedFeature(IBridge bridge, ApiToolEventArguments e)
    {
        int crsId = e.GetInt(Edit.EditMapCrsId);

        var editFeatureDef = await e.GetEditFeatureDefinitionFromSelection(bridge);

        var response = new ApiEventResponse();
        var tool = new Mobile.UpdateFeature();
        await Mobile.UpdateFeature.InitAsync(tool, bridge, e, editFeatureDef, response, crsId);
        tool.ParentTool = _sender;
        response.ActiveTool = tool;

        return response;
    }

    [ServerToolCommand("deletefeature")]
    public ApiEventResponse OnDeleteFeature(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new Mobile.EditSelectDeleteFeature()
            {
                ParentTool = _sender
            }
        };
    }

    [ServerToolCommand("deleteselectedfeature")]
    async public Task<ApiEventResponse> OnDeleteSelectedFeature(IBridge bridge, ApiToolEventArguments e)
    {
        int crsId = e.GetInt(Edit.EditMapCrsId);

        var editFeatureDef = await e.GetEditFeatureDefinitionFromSelection(bridge);

        var response = new ApiEventResponse();
        var tool = new Mobile.DeleteFeature();
        await Mobile.DeleteFeature.InitAsync(tool, bridge, e, editFeatureDef, response, crsId);
        tool.ParentTool = _sender;
        response.ActiveTool = tool;

        return response;
    }

    [ServerToolCommand("explodefeature")]
    public ApiEventResponse OnExplodeFeature(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new ExplodeFeature()
            {
                ParentTool = _sender
            }
        };
    }

    [ServerToolCommand("cutfeature")]
    public ApiEventResponse OnCutFeature(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new CutFeature()
            {
                ParentTool = _sender
            }
        };
    }

    [ServerToolCommand("clipfeature")]
    public ApiEventResponse OnClipFeature(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new ClipFeature()
            {
                ParentTool = _sender
            }
        };
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

    [ServerToolCommand("selection-tool")]
    public ApiEventResponse OnSelectionTool(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveTool = new IdentifyDefault()
        };
    }

    //[ServerToolCommand("editservice-featureinfo")]
    async public Task<ApiEventResponse> OnEditServiceGetCapabilities(IBridge bridge, ApiToolEventArguments e)
    {
        string serviceId = e["serviceid"];

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e);

        List<EditEnvironment.ClassCapability> classCapabilities = new List<EditEnvironment.ClassCapability>();

        foreach (var themeId in e["themeid"].Split(','))
        {
            var editTheme = editEnvironment[themeId];

            //#region Falls Editthema im CMS definiert wurde, ist eine defaultEditThemeDefinition notwendig, damit das Thema auch gefunden wird.

            //if (editTheme == null && editEnvironment.EditThemeDefinition == null)
            //{
            //    editEnvironment.EditThemeDefinition = new EditThemeDefinition()
            //    {
            //        ServiceId = serviceId,
            //        EditThemeId = e["themeid"]
            //    };

            //    editTheme = editEnvironment[themeId];
            //    editEnvironment.EditThemeDefinition = null;
            //}

            //#endregion

            if (editTheme == null)
            {
                continue; //throw new Exception("Unknown edit theme");
            }

            classCapabilities.Add(await editTheme.GetFeatureInfo(bridge, serviceId));
        }

        return new ApiRawJsonEventResponse(new
        {
            classes = classCapabilities.ToArray()
        });
    }

    [ServerToolCommand("fileupload")]
    public ApiEventResponse OnFileUpload(IBridge bridge, ApiToolEventArguments e)
    {
        return null;
    }

    #endregion
}
