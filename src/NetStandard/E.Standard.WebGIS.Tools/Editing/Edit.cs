using E.Standard.Localization.Abstractions;
using E.Standard.Localization.Reflection;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Geometry.Snapping;
using E.Standard.WebMapping.Core.Reflection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true,
                        VisFilterDependent = true,
                        ClientDeviceDependent = true,
                        MapCrsDependent = true,
                        ScaleDependent = true,
                        MapBBoxDependent = true,
                        MapImageSizeDependent = true,
                        UIElementDependency = true,
                        AllowCtrlBBox = true)]
[ToolHelp("tools/general/editing/index.html")]
[ToolId("webgis.tools.editing.edit")]
[ToolConfigurationSection("editing")]
[LocalizationNamespace("tools.editing")]
public class Edit : IApiServerToolLocalizableAsync<Edit>,
                    IApiButtonDependency,
                    IApiButtonResources,
                    IApiUndoTool,
                    IApiPostRequestEvent,
                    IServerCommandInstanceProvider<Edit>
{
    public const string EditAllThemesId = "edit-all-themes";
    public const string EditMapScaleId = "edit-map-scale";
    public const string EditMapCrsId = "edit-map-crsid";

    #region IApiServerTool Member

    public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Edit> localizer)
    {
        return e.EditToolServiceInstance(this, localizer).OnButtonClick(bridge, e);
    }

    public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<Edit> localizer)
    {
        return e.EditToolServiceInstance(this, localizer).OnEvent(bridge, e);
    }

    #endregion

    #region IApiTool Member

    public ToolType Type => WebMapping.Core.Api.ToolType.click;

    public ToolCursor Cursor => ToolCursor.Custom_Selector_Highlight;

    #endregion

    #region IApiButton Member

    public string Name => "Edit";

    public string Container => "Tools";

    public string Image => UIImageButton.ToolResourceImage(this, "edit");

    public string ToolTip => "Edit geo-objects on the map";

    public bool HasUI => true;

    #endregion

    #region IApiButtonDependency Member

    public VisibilityDependency ButtonDependencies
    {
        get
        {
            return VisibilityDependency.EditthemesExists;
        }
    }

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("edit", Properties.Resources.edit);

        toolResourceManager.AddImageResource("insert", Properties.Resources.edit_new);
        toolResourceManager.AddImageResource("update", Properties.Resources.edit_update);
        toolResourceManager.AddImageResource("delete", Properties.Resources.edit_delete);

        toolResourceManager.AddImageResource("undo", Properties.Resources.undo);

        toolResourceManager.AddImageResource("cut", Properties.Resources.edit_cut);
        toolResourceManager.AddImageResource("clip", Properties.Resources.edit_clip);
        toolResourceManager.AddImageResource("merge", Properties.Resources.edit_merge);
        toolResourceManager.AddImageResource("explode", Properties.Resources.edit_explode);
        toolResourceManager.AddImageResource("mass", Properties.Resources.edit_mass);

        toolResourceManager.AddImageResource("selecteiontool", Properties.Resources.identify);

        toolResourceManager.AddImageResource("pointer", Properties.Resources.pointer);
        toolResourceManager.AddImageResource("rectangle", Properties.Resources.rectangle);
        toolResourceManager.AddImageResource("rectangle-binoculars", Properties.Resources.rectangle_binoculars);
    }

    #endregion

    #region IApiUndoTool

    async public Task<ApiEventResponse> PerformUndo(IBridge bridge, ToolUndoDTO toolUndo)
    {
        var editUndoable = toolUndo.GetData<WebMapping.Core.Editing.EditUndoableDTO>();
        var editEnvironment = new EditEnvironment(bridge, editUndoable);

        var editTheme = editEnvironment[editEnvironment.EditThemeDefinition.EditThemeId];

        var updateResult = await editEnvironment.Undo(editTheme, editUndoable);
        if (updateResult.succeeded == false)
        {
            throw new Exception("Unable to undo: Unknown error");
        }

        return new ApiEventResponse()
        {
            RefreshServices = new string[] { editEnvironment.EditThemeDefinition.ServiceId },
            RefreshSelection = true,
            ReplaceQueryFeatures = updateResult.updatedFeatures,
            ReplaceFeaturesQueries = updateResult.updatedFeaturesQueries,
            UndoTool = !editEnvironment.HasUndoables ? null : new Edit(),
            ToolUndos = !editEnvironment.HasUndoables ?
                null :
                editEnvironment.Undoables.Select(u => new ToolUndoDTO(u, u.Shape)
                {
                    Title = u.ToTitle(editTheme, "Undo")
                }).ToArray()
        };
    }

    #endregion

    #region IApiPostRequestEvent

    async public Task<ApiEventResponse> PostProcessEventResponseAsync(IBridge bridge, ApiToolEventArguments e, ApiEventResponse response)
    {
        if (response.SketchReadonly == true)
        {
            return response;
        }

        if (response?.Sketch?.ShapeEnvelope != null &&
            response.Sketch.ShapeEnvelope.Area > 0D)
        {
            var shape = response.Sketch;

            await ProcessShape(bridge, shape, e, response);
        }
        else if (response.NamedSketches != null && response.NamedSketches.Count() > 0)
        {
            foreach (var namedSketch in response.NamedSketches)
            {
                var shape = namedSketch.Sketch;

                await ProcessShape(bridge, shape, e, response);
            }
        }

        return response;
    }

    private async Task ProcessShape(IBridge bridge, Shape shape, ApiToolEventArguments e, ApiEventResponse response)
    {
        if (shape == null)
        {
            return;
        }

        string editThemeDefString = e["_editfield_edittheme_def"];
        string oid = e["_edittheme_oid"];

        response.CloseSketch = !(shape is Polyline); // Polylinen nicht abschließen, damit man sie weiterzeichnen kann (Alice überfordert "Undo Sketch abschließen" nach dem auswählen)

        #region Vertices Fixieren

        if (!String.IsNullOrEmpty(editThemeDefString))
        {
            var editThemeDefinition = ApiToolEventArguments.FromArgument<EditThemeDefinition>(editThemeDefString);
            var editTheme = bridge.GetEditTheme(editThemeDefinition.ServiceId, editThemeDefinition.EditThemeId);

            if (editTheme != null)
            {
                var fixVerticesSnappings = await bridge.GetEditThemeFixToSnapping(editThemeDefinition.ServiceId, editThemeDefinition.EditThemeId);

                var queryShape = shape.ShapeEnvelope;
                queryShape.Raise(101);
                var sRef = bridge.CreateSpatialReference(shape.SrsId);
                double tolerance = sRef.IsProjective ? 1e-3 : 1e-9;

                foreach (var fixVerticesSnapping in fixVerticesSnappings)
                {
                    foreach (var layerId in fixVerticesSnapping.LayerIds)
                    {
                        var layer = fixVerticesSnapping.Service.Layers.FindByLayerId(layerId);

                        var filter = new WebMapping.Core.Filters.SpatialFilter(layer.IdFieldName, shape.ShapeEnvelope, 0, 0);
                        filter.FeatureSpatialReference = filter.FilterSpatialReference = sRef;
                        filter.SubFields = layer.IdFieldName;
                        filter.QueryGeometry = true;

                        var featureCollection = new WebMapping.Core.Collections.FeatureCollection();

                        if (await layer.GetFeaturesAsync(filter, featureCollection, bridge.RequestContext))
                        {
                            foreach (var feature in featureCollection)
                            {
                                if (fixVerticesSnapping.Service.Url == editThemeDefinition.ServiceId &&
                                    layer.ID == editThemeDefinition.LayerId &&
                                    feature.Oid.ToString() == oid)
                                {
                                    // dont snap on itself
                                    continue;
                                }

                                shape.SnapTo(feature.Shape, fixVerticesSnapping.SnappingTypes.ToSnappingTypes(), tolerance);
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }

    #endregion

    #region IServerCommandInstanceProvider

    public object ServerCommandInstance(IBridge bridge, ApiToolEventArguments e, ILocalizer<Edit> localizer)
    {
        return e.EditToolServiceInstance(this, localizer);
    }

    #endregion
}
