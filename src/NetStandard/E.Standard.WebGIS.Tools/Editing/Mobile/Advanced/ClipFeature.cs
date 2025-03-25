using E.Standard.Extensions.Compare;
using E.Standard.Localization.Reflection;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Geometry.Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Mobile.Advanced;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true, MapCrsDependent = true)]
[LocalizationNamespace("tools.editing.clip")]
public class ClipFeature : IApiServerToolAsync, IApiChildTool, IApiButtonResources
{
    const string EditClipFeatureMethodId = "editing-clip-feature-method";

    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features.Count != 1)
        {
            throw new ArgumentException("More than one feature selected!");
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
                                             EditOperation.Clip,
                                             features[0]);

        int calcCrsId = e.CalcCrs.OrTake(e.MapCrs) ?? 0;

        return new ApiEventResponse()
        {
            UIElements = mask.UIElements,
            UISetters = mask.UISetters,
            Sketch = new Polygon()
            {
                SrsId = calcCrsId,
                SrsP4Parameters = bridge.CreateSpatialReference(calcCrsId)?.Proj4
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
        get { return ToolType.sketch2d; }
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

    public string Name => "Objekt ausschneiden (Clip)";

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

    [ServerToolCommand("clip")]
    async public Task<ApiEventResponse> OnClip(IBridge bridge, ApiToolEventArguments e)
    {
        if (!(e.Sketch is Polygon))
        {
            throw new Exception("Bitte zuerst eine Verschnittfläche zeichnen");
        }

        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features.Count != 1)
        {
            throw new ArgumentException("More than one feature selected!");
        }

        var originalFeature = features[0];
        var originalShape = originalFeature.Shape;

        string[] imageUrls = null, imageLabels = null;
        if (originalShape is Polyline)
        {
            imageUrls = new string[]
            {
                UIImageButton.ToolResourceImage(this, "construct_clip_line_both_128"),
                UIImageButton.ToolResourceImage(this, "construct_clip_line_intersect_128"),
                UIImageButton.ToolResourceImage(this, "construct_clip_line_difference_128")
            };
            imageLabels = new string[]
            {
                "Schnittmenge + Differenzmenge",
                "Schnittmenge",
                "Differenzmenge"
            };
        }
        else if (originalShape is Polygon)
        {
            imageUrls = new string[]
            {
                UIImageButton.ToolResourceImage(this, "construct_clip_polygon_both_128"),
                UIImageButton.ToolResourceImage(this, "construct_clip_polygon_intersect_128"),
                UIImageButton.ToolResourceImage(this, "construct_clip_polygon_difference_128"),
                UIImageButton.ToolResourceImage(this, "construct_clip_polygon_xor_128")
            };
            imageLabels = new string[]
            {
                "Schnittmenge + Differenzmenge",
                "Schnittmenge",
                "Differenzmenge",
                "Symmetrische Differenzmenge"
            };
        }
        else
        {
            throw new Exception($"Unsupported geometry type: {originalShape?.GetType()}");
        }

        var apiResponse = new ApiEventResponse()
        {
            UIElements = new IUIElement[]
                {
                    new UIDiv()
                    {
                        target = UIElementTarget.modaldialog.ToString(),
                        targettitle = "Clip Methode",
                        targetwidth = "340px",
                        elements = new IUIElement[]
                        {
                            new UIImageSelector()
                            {
                                id = EditClipFeatureMethodId,
                                ImageUrls = imageUrls,
                                ImageLabels = imageLabels,
                                ImageWidth=128, ImageHeight=128,
                                MultiSelect=false,
                                value = imageUrls[0],
                                css = UICss.ToClass(new []{ UICss.ToolParameter, UICss.ToolParameterRequiredClientside }),
                                required_message = "Bitte eine Methode auswählen"
                            },
                            new UIButton(UIButton.UIButtonType.servertoolcommand, "perform_clip")
                            {
                                text="Feature Clippen"
                            }
                        }
                    }
                }
        };

        return apiResponse;
    }

    [ServerToolCommand("perform_clip")]
    async public Task<ApiEventResponse> OnPerformClip(IBridge bridge, ApiToolEventArguments e)
    {
        if (!(e.Sketch is Polygon))
        {
            throw new Exception("Bitte zuerst eine Verschnittfläche zeichnen");
        }

        //if (((Polyline)e.Sketch).PathCount != 1)
        //{
        //    throw new Exception("Die Schnittlinie darf keine Multipart-Geometrie aufweisen");
        //}

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId),
            CurrentMapSrsId = e.MapCrs.HasValue ? e.MapCrs.Value : 0
        };

        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features.Count != 1)
        {
            throw new ArgumentException("More than one feature selected!");
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

        var originalFeature = features[0];
        var originalShape = originalFeature.Shape;

        var clipPolygon = (Polygon)e.Sketch;
        var method = e[EditClipFeatureMethodId];

        List<Shape> newShapes = new List<Shape>();
        if (originalShape is Polyline)
        {
            var clipResult = ((Polyline)originalShape).Clip(clipPolygon);

            if (method == UIImageButton.ToolResourceImage(this, "construct_clip_line_both_128"))
            {
                if (clipResult.intersect != null)
                {
                    newShapes.Add(clipResult.intersect);
                }

                if (clipResult.difference != null)
                {
                    newShapes.Add(clipResult.difference);
                }
            }
            else if (method == UIImageButton.ToolResourceImage(this, "construct_clip_line_intersect_128"))
            {
                if (clipResult.intersect != null)
                {
                    newShapes.Add(clipResult.intersect);
                }
            }
            else if (method == UIImageButton.ToolResourceImage(this, "construct_clip_line_difference_128"))
            {
                if (clipResult.difference != null)
                {
                    newShapes.Add(clipResult.difference);
                }
            }
        }
        else if (originalShape is Polygon)
        {
            if (method == UIImageButton.ToolResourceImage(this, "construct_clip_polygon_both_128"))
            {
                newShapes.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctIntersection));
                newShapes.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctDifference));
            }
            else if (method == UIImageButton.ToolResourceImage(this, "construct_clip_polygon_intersect_128"))
            {
                newShapes.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctIntersection));
            }
            else if (method == UIImageButton.ToolResourceImage(this, "construct_clip_polygon_difference_128"))
            {
                newShapes.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctDifference));
            }
            else if (method == UIImageButton.ToolResourceImage(this, "construct_clip_xor_128"))
            {
                newShapes.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctXor));
            }
            else
            {
                throw new Exception($"Unbekannte Clip Methode: {method}");
            }
        }
        else
        {
            throw new Exception("Cut not supported for this shape/geometry-type");
        }

        if (newShapes.Count() > 0)
        {
            var newFeatures = newShapes.Select(shape =>
              {
                  var feature = originalFeature.Clone(false);
                  feature.Oid = 0;
                  feature.Shape = shape;

                  return feature;
              });

            if (!await editEnvironment.InserFeatures(editTheme, newFeatures))
            {
                throw new Exception("Fehler bei INSERT");
            }
        }
        else
        {
            throw new Exception("Clip liefert kein Ergebnis");
        }

        return new ApiEventResponse()
        {
            ActiveTool = new Advanced.DeleteOriginal()
            {
                ParentTool = new Edit()
            },
            RefreshServices = new string[] { editThemeDef.ServiceId },
            //RefreshSelection = true,
            UIElements = new IUIElement[]
            {
                new UIEmpty()  // close dialog
                {
                    target = UIElementTarget.modaldialog.ToString()
                }
            },
            UISetters = await bridge.RequiredDeleteOriginalSetters(newShapes, originalShape)
        };
    }

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("construct_clip_line_intersect_128", Properties.Resources.construct_clip_line_intersect_128);
        toolResourceManager.AddImageResource("construct_clip_line_difference_128", Properties.Resources.construct_clip_line_difference_128);
        toolResourceManager.AddImageResource("construct_clip_line_both_128", Properties.Resources.construct_clip_line_both_128);

        toolResourceManager.AddImageResource("construct_clip_polygon_intersect_128", Properties.Resources.construct_clip_polygon_intersect_128);
        toolResourceManager.AddImageResource("construct_clip_polygon_difference_128", Properties.Resources.construct_clip_polygon_difference_128);
        toolResourceManager.AddImageResource("construct_clip_polygon_both_128", Properties.Resources.construct_clip_polygon_both_128);
        toolResourceManager.AddImageResource("construct_clip_polygon_xor_128", Properties.Resources.construct_clip_polygon_xor_128);
    }

    #endregion
}
