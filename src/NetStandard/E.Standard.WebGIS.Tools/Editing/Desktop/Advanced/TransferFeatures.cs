using E.Standard.Extensions.Collections;
using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebGIS.Tools.Editing.Services;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.GeoServices.Graphics.Extensions;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Desktop.Advanced;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true,
                        MapCrsDependent = true)]
public class TransferFeatures : IApiServerToolAsync,
                                IApiChildTool
{
    private const string WebGisEditCopyToTargetId = "webgis-edit-transferfeatures-target";
    private const string EditMaskContainerId = "webgis-edit-transferfeatures-mask-holder";

    private readonly UpdateFeatureService _updateFeatureService;

    public TransferFeatures()
    {
        _updateFeatureService = new UpdateFeatureService(this);
    }

    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var service = (await bridge.GetService(e.SelectionInfo?.ServiceId))
                        .ThrowIfNull(() => $"Unknown service: {e.SelectionInfo?.ServiceId}");

        var query = (await bridge.GetQuery(service.Id, e.SelectionInfo?.QueryId))
                        .ThrowIfNull(() => $"Unknown Query: {e.SelectionInfo?.QueryId}");

        var selectedLayer = service.FindLayer(e.SelectionInfo.LayerId)
                                   .ThrowIfNull(() => $"Selected layer not found in service");

        var featureTransfers = (await bridge.GetQueryFeatureTransfers(service.Id, e.SelectionInfo.QueryId))
                                    .ThrowIfNullOrEmpty(() => $"No feature transfers found for query {query.Name}");


        var features = (await e.FeaturesFromSelectionAsync(bridge))
                               .ThrowIfNullOrEmpty(() => "No features selected!");

        var featureShapes = features.Select(f => f.Shape).Where(s => s != null);
        var bbox = featureShapes.BoundingBox().UnionWith(featureShapes.BoundingBox());
        var previewData = await featureShapes.CreateImage(bridge, 320, 240, bbox, useColors: new ArgbColor[] { ArgbColor.Cyan });

        var uiElements = new List<IUIElement>();

        e[WebGisEditCopyToTargetId] = featureTransfers.First().Id;

        uiElements.AddRange(new IUIElement[]
        {
            new UILabel() { label="Aktuelle Auswahl:" },
            new UIBreak(1),
            new UIImage(Convert.ToBase64String(previewData), true),
            new UILabel() { label="Ziel:" },
            new UIBreak(1),
            new UISelect()
            {
                options = featureTransfers.Select(f => new UISelect.Option() { value=f.Id, label = f.Name }).ToArray(),
                id = WebGisEditCopyToTargetId,
                css = UICss.ToClass(new []{ UICss.ToolParameter }),
                changetype = UIButton.UIButtonType.servertoolcommand_ext.ToString(),
                changetool = this.GetType().ToToolId(),
                changecommand = "transfer_target_changed"
            },
            new UIDiv()
            {
                id = EditMaskContainerId,
                elements = (await OnTransferTargetChanged(bridge,e)).UIElements?.FirstOrDefault()?.elements?.ToArray()
            }
        });

        var response = new ApiEventResponse()
        {
            UIElements = new IUIElement[]{
                new UIDiv()
                    {
                        target = UIElementTarget.tool_modaldialog.ToString(),
                        targettitle = DialogTitle,
                        elements = uiElements.ToArray()
                }
            }
        };

        return response;
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

    public string Name => "Copy Features";

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

    #region Properties

    private string DialogTitle => "Copy/Move Selelected Features";

    private string ApplyButtonTitle(FeatureTransferMethod method, int count) => $"{count} Objekte {(method == FeatureTransferMethod.Move ? "verschieben" : "kopieren")}";

    #endregion

    #region ServerCommands

    [ServerToolCommand("transfer_target_changed")]
    async public Task<ApiEventResponse> OnTransferTargetChanged(IBridge bridge, ApiToolEventArguments e)
    {
        var featureTransfer = (await bridge.GetQueryFeatureTransfers(e.SelectionInfo?.ServiceId, e.SelectionInfo?.QueryId))
                                    .Where(f => f.Id == e[WebGisEditCopyToTargetId])
                                    .FirstOrDefault()
                                    .ThrowIfNull(() => $"Can't find feature transfer for current query with id {e[WebGisEditCopyToTargetId]}");
        featureTransfer.Targets.ThrowIfNullOrEmpty(() => $"No targets are available in feature transfer {featureTransfer.Name}");

        var uiElements = new List<IUIElement>();
        var ignoreFields = featureTransfer.FieldSetters?.Select(f => f.Field).ToArray();

        if (featureTransfer.Method == CMS.FeatureTransferMethod.Move)
        {
            var query = (await bridge.GetQuery(e.SelectionInfo?.ServiceId, e.SelectionInfo?.QueryId))
                            .ThrowIfNull(() => $"Query for current selection not found");

            var firstFeature = (await e.FirstFeatureFromSelection(bridge))
                                   .ThrowIfNull(() => "No features selected!");

            var editTheme = e.EditThemeFromSelection(bridge)
                             .ThrowIfNull(() => $"Can't find edit theme for selelction. You only can move features to another target, if the source layer ({query?.Name}) is editable!");

            var editThemeDef = e.EditFeatureDefinitionFromSelection(bridge, firstFeature)
                                    .ThrowIfNull(() => "Can't find edit theme for selelction");

            uiElements.AddRange(new IUIElement[]
            {
                new UIHidden()
                {
                    id = editTheme.EditEnvironment.EditThemeElementId,
                    value = editThemeDef.EditThemeId,
                    css = UICss.ToClass(new string[] { UICss.ToolParameter })
                },
                new UIHidden()
                {
                    id = editTheme.EditEnvironment.EditThemeDefintionElementId,
                    value = ApiToolEventArguments.ToArgument(editThemeDef.ToEditThemeDefinition()),
                    css = UICss.ToClass(new string[] { UICss.ToolParameter })
                },
            });
        }



        foreach (var target in featureTransfer.Targets)
        {
            var editThemeBridge = bridge.GetEditTheme(target.ServiceId, target.EditThemeId)
                                        .ThrowIfNull(() => $"EditTheme {target.EditThemeId} not found in service {target.ServiceId}");

            var editEnvironment = new EditEnvironment(bridge,
                    editThemeDefinition: new EditThemeDefinition()
                    {
                        ServiceId = target.ServiceId,
                        LayerId = editThemeBridge.LayerId,
                        EditThemeId = editThemeBridge.ThemeId
                    },
                    editFieldPrefix: target.EditThemeId);

            var editTheme = editEnvironment[editThemeBridge.ThemeId]
                                .ThrowIfNull(() => $"Can't create EditTheme for {editThemeBridge.ThemeId}");

            EditEnvironment.UIEditMask mask = await editTheme.ParseMask(bridge,
                                                                        editEnvironment.EditThemeDefinition,
                                                                        EditOperation.FeatureTransfer,
                                                                        useMobileBehavoir: false,
                                                                        editThemeNameAsCategory: true,
                                                                        ignoreFields: ignoreFields);

            uiElements.AddRange(mask.UIElements.FindRecursive<UICollapsableElement>());
        }

        uiElements.AddRange(new[]
        {
            new UIButtonGroup()
            {
                elements = new IUIElement[]
                {
                    new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                    {
                        text = "Abbrechen",
                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionButtonStyle })
                    },
                    new UIButton(UIButton.UIButtonType.servertoolcommand, "apply")
                    {
                        text = ApplyButtonTitle(featureTransfer.Method, e.SelectionInfo.ObjectCount()),
                        css = UICss.ToClass(new string[] { UICss.DefaultButtonStyle, UICss.OptionButtonStyle, UICss.ValidateInputButton })
                    }
                }
            }
        });

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                new UIDiv()
                {
                    target = $"#{EditMaskContainerId}",
                    elements = uiElements.ToArray()
                }
            }
        };
    }

    [ServerToolCommand("apply")]
    virtual async public Task<ApiEventResponse> OnApply(IBridge bridge, ApiToolEventArguments e)
    {
        var features = await e.FeaturesFromSelectionAsync(bridge);

        var featureTransfer = (await bridge.GetQueryFeatureTransfers(e.SelectionInfo?.ServiceId, e.SelectionInfo?.QueryId))
                                    .Where(f => f.Id == e[WebGisEditCopyToTargetId])
                                    .FirstOrDefault()
                                    .ThrowIfNull(() => $"Can't find feature transfer for current query with id {e[WebGisEditCopyToTargetId]}");

        featureTransfer.Targets.ThrowIfNullOrEmpty(() => $"No targets are available in feature transfer {featureTransfer.Name}");

        var refreshServiceIds = new List<string>();

        if (featureTransfer.Method == CMS.FeatureTransferMethod.Move)
        {
            refreshServiceIds.Add(e.SelectionInfo.ServiceId);
        }

        foreach (var target in featureTransfer.Targets)
        {
            var editThemeBridge = bridge.GetEditTheme(target.ServiceId, target.EditThemeId)
                .ThrowIfNull(() => $"EditTheme {target.EditThemeId} not found in service {target.ServiceId}");

            var editEnvironment = new EditEnvironment(bridge,
                    editThemeDefinition: new EditThemeDefinition()
                    {
                        ServiceId = target.ServiceId,
                        LayerId = editThemeBridge.LayerId,
                        EditThemeId = editThemeBridge.ThemeId
                    });

            var editTheme = editEnvironment[editThemeBridge.ThemeId]
                                .ThrowIfNull(() => $"Can't create EditTheme for {editThemeBridge.ThemeId}");

            var targetFeatures = new List<WebMapping.Core.Feature>();
            var service = await bridge.GetService(target.ServiceId)
                                      .ThrowIfNull(() => $"Unknown service: {e.SelectionInfo?.ServiceId}");

            var targetLayer = service.Layers.Where(l => l.Id == editThemeBridge.LayerId)
                                            .FirstOrDefault()
                                            .ThrowIfNull(() => $"Service {service.Name} does not contain a layer with id {editThemeBridge.LayerId}");

            var targetLayerFields = (await bridge.GetServiceLayerFields(service.Id, targetLayer.Id))
                                            .ThrowIfNull(() => "Target layer has no fields to copy");

            foreach (var feature in features)
            {
                if (feature.Shape == null)
                {
                    continue;
                }

                var targetFeature = new WebMapping.Core.Feature();
                targetFeature.Shape = feature.Shape;

                #region Set Idendent Attributes

                foreach (var attribute in feature.Attributes.OrEmptyArray())
                {
                    if (attribute.Name == targetLayer.IdFieldname)
                    {
                        continue;
                    }

                    var targetField = targetLayerFields.FindField(attribute.Name);

                    if (targetField != null)
                    {
                        targetFeature.Attributes.Add(new WebMapping.Core.Attribute(targetField.Name, attribute.Value));
                    }
                }

                #endregion

                #region Set UI (Massattribute) Attributes

                foreach (var targetField in targetLayerFields)
                {
                    if (!e[$"_{editThemeBridge.ThemeId}_applyfield_{targetField.Name}"].ApplyField())
                    {
                        continue;
                    }

                    string value = e[$"{editThemeBridge.ThemeId}_{targetField.Name}"];

                    if (!String.IsNullOrEmpty(value))
                    {
                        targetFeature.Attributes.SetOrAdd(targetField.Name, value);
                    }
                }

                #endregion

                #region Set Setter Attributes

                foreach (var fieldSetter in featureTransfer.FieldSetters.OrEmptyArray())
                {
                    if (fieldSetter.IsDefaultValue)
                    {
                        // if this attribute already set end not empty => continue;
                        if (!String.IsNullOrEmpty(targetFeature.Attributes.GetCaseInsensitiv(fieldSetter.Field)?.Value))
                        {
                            continue;
                        }
                    }

                    var value = WebGIS.CMS.Globals.SolveExpression(feature, fieldSetter.ValueExpression);
                    var targetField = targetLayerFields.FindField(fieldSetter.Field);

                    if (fieldSetter.IsRequired)
                    {
                        if (String.IsNullOrEmpty(value))
                        {
                            throw new Exception($"Value for required field {fieldSetter.Field} is empty");
                        }
                        if (targetField == null)
                        {
                            throw new Exception($"Required field {fieldSetter.Field} not exists in target edit theme {editTheme.Name} feature class");
                        }
                    }

                    if (targetField != null)
                    {
                        targetFeature.Attributes.SetOrAdd(targetField.Name, value);
                    }
                }

                #endregion

                targetFeatures.Add(targetFeature);
            }

            await editEnvironment.TransferFeatures(editTheme, targetFeatures,
                                                   target.PipelineSuppressAutovalues,
                                                   target.PipelineSuppressValidation);

            refreshServiceIds.Add(target.ServiceId);
        }

        ApiEventResponse response =
            featureTransfer.Method == CMS.FeatureTransferMethod.Move ?
            await _updateFeatureService.DeleteSelectedFeatures(bridge, e, new Edit()) :
            new ApiEventResponse() { ActiveTool = new Edit() };

        response.RefreshServices = refreshServiceIds.Distinct().ToArray();

        return response;
    }

    // if there are cascading combos in the mask => handel this hier
    [ServerEventHandler(ServerEventHandlers.OnUpdateCombo)]
    async public Task<ApiEventResponse> OnUpdateCombo(IBridge bridge, ApiToolEventArguments e)
    {
        var featureTransfer = (await bridge.GetQueryFeatureTransfers(e.SelectionInfo?.ServiceId, e.SelectionInfo?.QueryId))
                                   .Where(f => f.Id == e[WebGisEditCopyToTargetId])
                                   .FirstOrDefault();

        if (featureTransfer?.Targets == null)
        {
            return null;
        }

        var response = new ApiEventResponse();

        foreach (var target in featureTransfer.Targets)
        {
            var editThemeBridge = bridge.GetEditTheme(target.ServiceId, target.EditThemeId)
                .ThrowIfNull(() => $"EditTheme {target.EditThemeId} not found in service {target.ServiceId}");

            var editEnvironment = new EditEnvironment(bridge,
                    editThemeDefinition: new EditThemeDefinition()
                    {
                        ServiceId = target.ServiceId,
                        LayerId = editThemeBridge.LayerId,
                        EditThemeId = editThemeBridge.ThemeId
                    },
                    editFieldPrefix: editThemeBridge.ThemeId);

            var editTheme = editEnvironment[editThemeBridge.ThemeId]
                                .ThrowIfNull(() => $"Can't create EditTheme for {editThemeBridge.ThemeId}");

            var updateComboResponse = await new EditService().OnUpdateCombo(bridge, e,
                                                                            editEnvironment: editEnvironment,
                                                                            editTheme: editTheme,
                                                                            editFieldPrefix: target.EditThemeId);
            if (updateComboResponse?.UISetters != null)
            {
                response.AddUISetters(updateComboResponse.UISetters.ToArray());
            }
        }

        return response;
    }

    #endregion
}
