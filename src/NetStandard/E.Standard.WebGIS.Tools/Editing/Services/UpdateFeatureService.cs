using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Services;

internal class UpdateFeatureService
{
    private readonly IApiButton _sender;

    public UpdateFeatureService(IApiButton sender)
    {
        _sender = sender;
    }

    public delegate void ModifyEditMask(EditEnvironment.UIEditMask mask, WebMapping.Core.Feature feature);
    public enum SketchType { None, Editable, ReadOnly };

    async public Task<ApiEventResponse> EditMaskResponse(IBridge bridge,
                                                         ApiToolEventArguments e,
                                                         EditOperation editOperation,
                                                         string editFieldPrefix = null,
                                                         SketchType sketckType = SketchType.None,
                                                         //string uiTarget = "",
                                                         ModifyEditMask modifyEditMask = null)
    {
        var featureOid = e["feature-oid"];
        var editThemeName = e["edittheme"];
        var mapScale = e.MapScale.GetValueOrDefault().OrTake(e.GetDouble("edit-map-scale"));
        var mapCrsId = e.MapCrs.GetValueOrDefault().OrTake(e.GetInt("edit-map-crsid"));

        var oid = featureOid.ParseFeatureGlobalOid();
        var query = await bridge.GetQuery(oid.serviceId, oid.queryId);

        if (query == null)
        {
            throw new ArgumentException($"Unknown query {oid.serviceId}:{oid.queryId}");
        }

        var editThemeBridge = bridge.GetEditThemes(oid.serviceId).Where(a => a.ThemeId == editThemeName).FirstOrDefault();
        if (editThemeBridge == null)
        {
            throw new ArgumentException($"Unknown edittheme: {editThemeName}");
        }

        ApiOidFilter filter = new ApiOidFilter(oid.featureId)
        {
            QueryGeometry = sketckType != SketchType.None,
            Fields = QueryFields.All
        };

        var editFeatures = await bridge.QueryLayerAsync(oid.serviceId, editThemeBridge.LayerId, filter);

        if (editFeatures == null || editFeatures.Count == 0)
        {
            throw new Exception($"Feature from {query.Name} with id {oid.featureId} does not exists!");
        }

        if (editFeatures.Count > 1)
        {
            throw new Exception($"Query from {query.Name} with id {oid.featureId} doesn't return an unique feature!");
        }

        var editFeature = editFeatures[0];

        editFeature.GlobalOid = featureOid;
        var editFeatureDefinition = new EditFeatureDefinition()
        {
            ServiceId = oid.serviceId,
            LayerId = editThemeBridge.LayerId,
            FeatureOid = oid.featureId,
            EditThemeId = editThemeBridge.ThemeId,
            EditThemeName = editThemeBridge.Name,
            Feature = editFeature
        };

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e, editFeatureDefinition.ToEditThemeDefinition(),
                                                              editFieldPrefix: editFieldPrefix)
        {
            CurrentMapScale = mapScale,
            CurrentMapSrsId = mapCrsId
        };

        var editTheme = editEnvironment[editThemeName];

        var mask = await editTheme.ParseMask(bridge,
                                             editFeatureDefinition.ToEditThemeDefinition(),
                                             editOperation,
                                             editFeatureDefinition.Feature,
                                             false,
                                             onUpdateComboCallbackToolId: _sender.GetType().ToToolId());

        modifyEditMask?.Invoke(mask, editFeature);

        var response = new ApiEventResponse()
        {
            UIElements = mask.UIElements,
            UISetters = mask.UISetters
        };

        if (sketckType != SketchType.None)
        {
            response.Sketch = editFeature.Shape;
            if (response.Sketch != null)
            {
                int srsId = editTheme.SrsId(mapCrsId);
                response.Sketch.SrsId = srsId;

                if (!String.IsNullOrEmpty(editTheme.DbRights) && !editTheme.DbRights.Contains("g"))
                {
                    response.SketchReadonly = true;
                }

                //if ((srsId != mapCrsId || e.MapCrsIsDynamic) && filter.FeatureSpatialReference != null)
                //{
                //    response.Sketch.SrsP4Parameters = filter.FeatureSpatialReference.Proj4;
                //}

                if (srsId != mapCrsId || e.MapCrsIsDynamic)
                {
                    AddSrsP4Parameters(bridge, response.Sketch);
                }

                if (sketckType == SketchType.ReadOnly)
                {
                    response.CloseSketch = true;
                    response.SketchReadonly = true;
                }
                else
                {
                    response.CloseSketch = editFeature.Shape is Polyline;   // Polylinen nicht abschließen, damit man sie weiterzeichnen kann (Alice überfordert "Undo Sketch abschließen" nach dem auswählen)
                }
            }
        }

        return response;
    }

    async public Task<ApiEventResponse> SaveFeature(IBridge bridge,
                                                    ApiToolEventArguments e,
                                                    IApiTool newActiveTool = null)
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.MapScale.GetValueOrDefault().OrTake(e.GetDouble(Edit.EditMapScaleId)),
            CurrentMapSrsId = e.MapCrs.GetValueOrDefault().OrTake(e.GetInt(Edit.EditMapCrsId))
        };

        var feature = editEnvironment.GetFeature(bridge, e);
        var editTheme = editEnvironment[e];

        if (!editTheme.DbRights.Contains("g"))
        {
            feature.Shape = null;
        }

        await editEnvironment.UpdateFeature(editTheme, feature);

        var editThemeDef = editEnvironment.EditThemeDefinition;

        if (editEnvironment?.EditThemeDefinition != null)
        {
            await bridge.SetUserFavoritesItemAsync(new Edit(), "Edit", editEnvironment.EditThemeDefinition.ServiceId + "," + editEnvironment.EditThemeDefinition.LayerId + "," + editEnvironment.EditThemeDefinition.EditThemeId);
        }

        WebMapping.Core.Collections.FeatureCollection editedFeatures = null;
        IQueryBridge query = null;
        SpatialReference sRef = null;

        try
        {
            var oid = e[$"_editfield_globaloid"].ParseFeatureGlobalOid();
            ApiOidFilter filter = new ApiOidFilter(feature.Oid)
            {
                QueryGeometry = false,
                Fields = QueryFields.All,
                FeatureSpatialReference = sRef = bridge.CreateSpatialReference(4326)
            };

            editedFeatures = await bridge.QueryLayerAsync(editThemeDef.ServiceId, editThemeDef.LayerId, filter);
            query = await bridge.GetQuery(oid.serviceId, oid.queryId);
        }
        catch { }

        var refreshServices = new List<string>() { editThemeDef.ServiceId };
        refreshServices.AddRange(await bridge.GetAssociatedServiceIds(query));

        return new ApiEventResponse()
        {
            ActiveTool = newActiveTool,
            RefreshServices = refreshServices.Distinct().ToArray(),
            RefreshSelection = IsSelectedFeature(bridge, e, feature, editThemeDef),
            UndoTool = !editEnvironment.HasUndoables ? null : new Edit(),
            ToolUndos = !editEnvironment.HasUndoables ?
                null :
                editEnvironment.Undoables.Select(u => new ToolUndoDTO(u, AddSrsP4Parameters(bridge, u.PreviewShape))
                {
                    Title = u.ToTitle(editTheme)
                }).ToArray(),

            ReplaceFeaturesQueries = await bridge.GetLayerQueries(query),
            ReplaceQueryFeatures = editedFeatures,
            ReplaceFeatureSpatialReference = sRef
        };
    }

    async public Task<ApiEventResponse> DeleteFeature(IBridge bridge,
                                                      ApiToolEventArguments e,
                                                      IApiTool newActiveTool = null,
                                                      bool removeFeatureFromTable = false)
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e);
        var feature = editEnvironment.GetFeature(bridge, e);
        var editTheme = editEnvironment[e];

        await editEnvironment.DeleteFeature(editTheme, feature);
        var editThemeDef = editEnvironment.EditThemeDefinition;

        if (editEnvironment?.EditThemeDefinition != null)
        {
            await bridge.SetUserFavoritesItemAsync(new Edit(), "Edit", editEnvironment.EditThemeDefinition.ServiceId + "," + editEnvironment.EditThemeDefinition.LayerId + "," + editEnvironment.EditThemeDefinition.EditThemeId);
        }

        var query = await bridge.GetFirstLayerQuery(editThemeDef.ServiceId, editThemeDef.LayerId);
        var refreshServices = new List<string>() { editThemeDef.ServiceId };
        if (query != null)
        {
            refreshServices.AddRange(await bridge.GetAssociatedServiceIds(query));
        }

        var response = new ApiEventResponse()
        {
            ActiveTool = newActiveTool,
            RefreshServices = refreshServices.Distinct().ToArray(),
            RefreshSelection = true,
            UndoTool = !editEnvironment.HasUndoables ? null : new Edit(),
            ToolUndos = !editEnvironment.HasUndoables ?
                null :
                editEnvironment.Undoables.Select(u => new ToolUndoDTO(u, AddSrsP4Parameters(bridge, u.PreviewShape))
                {
                    Title = u.ToTitle(editTheme)
                }).ToArray()
        };

        if (removeFeatureFromTable)
        {
            response.RemoveFeaturesQueries = await bridge.GetLayerQueries(editThemeDef.ServiceId, editThemeDef.LayerId);
            response.RemoveQueryFeaturesById = new[] { feature.Oid };
        }
        else
        {
            if (IsSelectedFeature(bridge, e, feature, editThemeDef))
            {
                response.ClientCommands = new ApiClientButtonCommand[] { ApiClientButtonCommand.removequeryresults };
            }
        }

        return response;
    }

    async public Task<ApiEventResponse> DeleteSelectedFeatures(IBridge bridge,
                                                               ApiToolEventArguments e,
                                                               IApiTool newActiveTool = null)
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e);
        var editTheme = editEnvironment[e];

        var filter = new ApiOidsFilter(e.SelectionInfo.ObjectIds.ToArray())
        {
            QueryGeometry = false,
            Fields = QueryFields.Id
        };

        var features = await bridge.QueryLayerAsync(e.SelectionInfo.ServiceId, e.SelectionInfo.LayerId, filter);
        await editEnvironment.DeleteFeatures(editTheme, features);

        var editThemeDef = editEnvironment.EditThemeDefinition;

        var query = await bridge.GetFirstLayerQuery(editThemeDef.ServiceId, editThemeDef.LayerId);
        var refreshServices = new List<string>() { editThemeDef.ServiceId };
        if (query != null)
        {
            refreshServices.AddRange(await bridge.GetAssociatedServiceIds(query));
        }

        var response = new ApiEventResponse()
        {
            ActiveTool = newActiveTool,
            RefreshServices = refreshServices.Distinct().ToArray(),
            RefreshSelection = true,
            UndoTool = !editEnvironment.HasUndoables ? null : new Edit(),
            ToolUndos = !editEnvironment.HasUndoables ?
                null :
                editEnvironment.Undoables.Select(u => new ToolUndoDTO(u, AddSrsP4Parameters(bridge, u.PreviewShape))
                {
                    Title = u.ToTitle(editTheme)
                }).ToArray(),
            RemoveFeaturesQueries = await bridge.GetLayerQueries(editThemeDef.ServiceId, editThemeDef.LayerId),
            RemoveQueryFeaturesById = e.SelectionInfo.ObjectIds.ToArray(),
            ClientCommands = new ApiClientButtonCommand[] { ApiClientButtonCommand.removequeryresults }
        };

        return response;
    }

    #region Helper

    public bool IsSelectedFeature(IBridge bridge, ApiToolEventArguments e, WebMapping.Core.Feature feature, EditThemeDefinition editThemeDef)
    {
        if (e.SelectionInfo == null)
        {
            return false;
        }

        if (e.SelectionInfo.ObjectIds?.Where(o => feature.Oid == o).Count() == 0)
        {
            return false;
        }

        return e.SelectionInfo.ServiceId == editThemeDef.ServiceId &&
               e.SelectionInfo.LayerId == editThemeDef.LayerId;
    }

    public Shape AddSrsP4Parameters(IBridge bridge, Shape shape)
    {
        if (shape != null && shape.SrsId > 0 && String.IsNullOrWhiteSpace(shape.SrsP4Parameters))
        {
            var sRef = bridge.CreateSpatialReference(shape.SrsId);
            if (sRef != null)
            {
                shape.SrsP4Parameters = sRef.Proj4;
            }
        }

        return shape;
    }

    #endregion
}
