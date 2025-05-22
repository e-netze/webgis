using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Geometry.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Services;

internal class NewFeatureService
{
    async public Task<ApiEventResponse> EditFeatureMaskReponse(IBridge bridge,
                                                               IApiTool sender,
                                                               EditThemeDefinition editThemeDef,
                                                               int mapCrsId,
                                                               ApiToolEventArguments e,
                                                               string uiTarget = "")
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, editThemeDef);
        EditEnvironment.EditTheme editTheme = editEnvironment[editThemeDef.EditThemeId];

        if (editTheme == null)
        {
            throw new ArgumentException("Can't find edit theme definition: " + editThemeDef.EditThemeId);
        }

        IServiceBridge service = await bridge.GetService(editThemeDef.ServiceId);

        if (service == null)
        {
            throw new Exception("Unknown service: " + editThemeDef.ServiceId);
        }

        ILayerBridge layer = service.FindLayer(editThemeDef.LayerId);

        if (layer == null)
        {
            throw new Exception("Unknown layer: " + editThemeDef.LayerId);
        }

        #region GeometryType and Sketch

        LayerGeometryType geometryType = layer.GeometryType;

        if (geometryType == LayerGeometryType.unknown)
        {
            geometryType = editTheme.GeometryType;
        }

        Shape sketch = null;

        switch (geometryType)
        {
            case LayerGeometryType.point:
                sketch = Point.Empty;
                break;
            case LayerGeometryType.line:
                sketch = new Polyline();
                break;
            case LayerGeometryType.polygon:
                sketch = new Polygon();
                break;
            default:
                throw new Exception("Unknown geometry type");
        }

        int srsId = editTheme.SrsId(mapCrsId);

        sketch.SrsId = srsId;
        if (e.RequireCrsP4Parameters(srsId))
        {
            var sRef = bridge.CreateSpatialReference(srsId);
            if (sRef != null)
            {
                sketch.SrsP4Parameters = sRef.Proj4;
            }
        }

        #endregion

        #region Mask

        EditEnvironment.UIEditMask mask = await editTheme.ParseMask(bridge,
                                                                    editThemeDef,
                                                                    EditOperation.Insert,
                                                                    useMobileBehavoir: e.UseMobileBehavior());

        if (!String.IsNullOrEmpty(uiTarget))
        {
            mask.UIElements.First().target = uiTarget;
        }

        #endregion

        return new ApiEventResponse()
        {
            UIElements = mask.UIElements,
            UISetters = mask.UISetters,
            Sketch = sketch,
            ApplyEditingTheme = new EditingThemeDefDTO(editThemeDef.EditThemeId, editThemeDef.ServiceId)
        };
    }

    public void AddLastSketchPoint(ApiEventResponse response, WebMapping.Core.Feature feature, bool snapPoint = true)
    {
        response.Sketch = feature.Shape.LastPoint();

        #region Snap Point

        if (snapPoint)
        {
            var point = SpatialAlgorithms.ShapePoints(response.Sketch, false).FirstOrDefault();
            if (point != null)
            {
                point.IsSnapped = true;
            }
        }

        #endregion
    }

    public void KeepAllAttributes(ApiEventResponse response, ApiToolEventArguments e, WebMapping.Core.Feature feature)
    {
        foreach (var propertyName in e.Properties.Where(p => p.StartsWith("editfield_")))
        {
            string featurePropertyName = propertyName.Substring("editfield_".Length);
            if (!String.IsNullOrEmpty(feature[featurePropertyName]))
            {
                if (response.UISetters == null)
                {
                    response.UISetters = new List<IUISetter>();
                }
                else if (response.UISetters.IsReadOnly)
                {
                    response.UISetters = new List<IUISetter>(response.UISetters);
                }
                response.UISetters.Add(new UISetter(propertyName, feature[featurePropertyName]));
            }
        }
    }
}
