using E.Standard.Localization.Reflection;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Geometry.Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Desktop.Advanced;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true, MapCrsDependent = true)]
[LocalizationNamespace("tools.editing.cut")]
public class CutFeatures : IApiServerToolAsync, IApiChildTool
{
    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features == null || features.Count == 0)
        {
            throw new ArgumentException("No features selected!");
        }

        var editTheme = e.EditThemeFromSelection(bridge);
        if (editTheme == null)
        {
            throw new ArgumentException("Can't find edit theme for selelction");
        }

        var editThemeDef = e.EditFeatureDefinitionFromSelection(bridge, features[0]);
        if (editThemeDef == null)
        {
            throw new ArgumentException("Can't find edit theme for selelction");
        }

        var mask = await editTheme.ParseMask(bridge,
                                             editThemeDef.ToEditThemeDefinition(),
                                             EditOperation.Cut,
                                             features[0]);

        int mapCrsId = e.CalcCrs.Value;

        return new ApiEventResponse()
        {
            UIElements = mask.UIElements,
            UISetters = mask.UISetters,
            Sketch = new Polyline()
            {
                SrsId = mapCrsId,
                SrsP4Parameters = bridge.CreateSpatialReference(mapCrsId)?.Proj4
            }
        };
    }

    public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        return Task.FromResult<ApiEventResponse>(null);
    }

    #endregion

    #region IApiTool Member

    virtual public ToolType Type
    {
        get { return ToolType.sketch1d; }
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

    public string Name => "Objekt teilen";

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

    [ServerToolCommand("cut")]
    async public Task<ApiEventResponse> OnCut(IBridge bridge, ApiToolEventArguments e)
    {
        if (!(e.Sketch is Polyline))
        {
            throw new Exception("Bitte eine Schittlinie zeichnen");
        }

        if (((Polyline)e.Sketch).PathCount != 1)
        {
            throw new Exception("Die Schnittlinie darf keine Multipart-Geometrie aufweisen");
        }

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId),
            CurrentMapSrsId = e.MapCrs.HasValue ? e.MapCrs.Value : 0
        };

        var sRef = bridge.CreateSpatialReference(e.CalcCrs.HasValue ? e.CalcCrs.Value : 0);
        var tolerance = sRef != null && sRef.IsProjective ? 1e-3 : 1e-7;

        e.Sketch.TryTransform(sRef.Id);

        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features == null || features.Count == 0)
        {
            throw new ArgumentException("No features selected!");
        }

        var editTheme = e.EditThemeFromSelection(bridge);
        if (editTheme == null)
        {
            throw new ArgumentException("Can't find edit theme for selelction");
        }

        var editThemeDef = e.EditFeatureDefinitionFromSelection(bridge, features[0]);
        if (editThemeDef == null)
        {
            throw new ArgumentException("Can't find edit theme for selelction");
        }

        var cutter = (Polyline)e.Sketch;
        var newFeatures = new List<WebMapping.Core.Feature>();
        var affectedFeatures = new List<WebMapping.Core.Feature>();

        foreach (var originalFeature in features)
        {
            IEnumerable<Shape> newShapes = null;
            var originalShape = originalFeature.Shape;

            if (originalShape is Polyline)
            {
                newShapes = ((Polyline)originalShape).TryCut(cutter[0]);
            }
            else if (originalShape is Polygon)
            {

                //
                // extend cutter with tolerance,
                // may there was inaccurate snapping in editing process
                //
                var cutterPath = cutter[0].Extend(tolerance);

                //newShapes = ((Polygon)originalShape).Cut2(cutter[0], tolerance);
                newShapes = ((Polygon)originalShape).TryCut(cutterPath, tolerance);
            }
            else
            {
                throw new Exception("Cut not supported for this shape/geometry-type");
            }

            //
            // DoTo: Transaction? Was ist, wenn Update oder Insert nicht hinhauen!?
            // Vorher überprüfen ob Editthema insert und update Rechte hat...
            // Eventuell den Update erst als letztes machen, damit Orignal Feature erst am schluss überschrieben wird?
            //

            if (newShapes != null && newShapes.Count() > 0)
            {
                newFeatures.AddRange(newShapes.Select(shape =>
                {
                    var feature = originalFeature.Clone(false);
                    feature.Oid = 0;
                    feature.Shape = shape;

                    return feature;
                }));

                affectedFeatures.Add(originalFeature);
            }
        }

        if (newFeatures.Count() == 0)
        {
            throw new Exception("Der Verschnitt liefert kein Ergebnis");
        }

        if (!await editEnvironment.InserFeatures(editTheme, newFeatures))
        {
            throw new Exception("Fehler bei INSERT");
        }

        return new ApiEventResponse()
        {
            ActiveTool = new Advanced.DeleteSelectedSubsetOriginals()
            {
                ParentTool = new Edit()
            },
            RefreshServices = new string[] { editThemeDef.ServiceId },
            //RefreshSelection = true,

            UISetters = await bridge.RequiredDeleteOriginalSetters(
                newFeatures.Select(f => f.Shape),
                affectedFeatures.Select(f => f.Shape),
                affectedFeatures.Select(f => f.Oid))
        };
    }

    #endregion
}
