using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.I18n;
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

namespace E.Standard.WebGIS.Tools.Editing.Desktop.Advanced;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true, MapCrsDependent = true)]
public class ClipFeatures : IApiServerToolAsync, IApiChildTool, IApiButtonResources
{
    const string EditClipFeatureMethodId = "editing-clip-feature-method";
    const string EditClipAffectedFeaturesMethodId = "editing-clip-affectedFeatures-method";
    const string EditClipAutoExplodeMultipartFeaturesId = "editing-clip-auto-explode-multipart-features";

    private enum AffectedFeaturesMethod
    {
        ApplyClipToIntersected = 0,
        ApplyClipToAll = 1
    }

    private enum MultipartBehavior
    {
        DisolveMultipartFeatures = 0,
        ClippedFeaturesStayMultiparts = 1
    }

    private enum ClipMethod
    {
        Intersect,
        Difference,
        Both,
        Xor
    }

    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features == null || features.Count == 0)
        {
            throw new ArgumentException("No features selected!");
        }

        var editTheme = e.EditThemeFromSelection(bridge)
                         .ThrowIfNull(() => "Can't find edit theme for selelction");

        var editThemeDef = e.EditFeatureDefinitionFromSelection(bridge, features[0])
                            .ThrowIfNull(() => "Can't find edit theme for selelction");

        var uiElements = new List<IUIElement>(new IUIElement[]
        {
            new UIInfoBox(bridge.LocalizeString(this, L10nKeys.ClipDescription)),
            new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removesketch)
            {
                text = bridge.LocalizeString(this, L10nKeys.RemoveSketch),
                css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionButtonStyle })
            },
            new UIBreak(1),
            new UISelect()
            {
                 id = EditClipAffectedFeaturesMethodId,
                 css = UICss.ToClass(new []{ UICss.ToolParameter}),
                 options = EnumExtensions.ToUISelectOptions<AffectedFeaturesMethod>(bridge)
            },
            new UIInfoBox(EditClipAffectedFeaturesMethodId, EnumExtensions.ToDescriptionDictionary<AffectedFeaturesMethod>(bridge)),
            new UISelect()
            {
                id = EditClipAutoExplodeMultipartFeaturesId,
                css =UICss.ToClass(new[] {UICss.ToolParameter}),
                options = EnumExtensions.ToUISelectOptions<MultipartBehavior>(bridge)
            },
            new UIInfoBox(EditClipAutoExplodeMultipartFeaturesId, EnumExtensions.ToDescriptionDictionary<MultipartBehavior>(bridge)),
            new UIButtonGroup()
            {
                elements = new[]
                {
                    new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                    {
                        text = bridge.LocalizeString(this, L10nKeys.Cancel),
                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.ButtonIcon })
                    },
                    new UIButton(UIButton.UIButtonType.servertoolcommand, "clip")
                    {
                        text = bridge.LocalizeString(this, L10nKeys.Clip),
                        css = UICss.ToClass(new string[] { UICss.OkButtonStyle, UICss.ButtonIcon })
                    }
                }
            }
        });

        int calcCrsId = e.CalcCrs.OrTake(e.MapCrs) ?? 0;

        return new ApiEventResponse()
        {
            UIElements = uiElements,
            Sketch = new Polygon()
            {
                SrsId = calcCrsId,
                SrsP4Parameters = bridge.CreateSpatialReference(calcCrsId)?.Proj4
            }
        };
    }

    public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        return OnPerformClip(bridge, e);
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

    public string Name => L10nKeys.ClipObjects;

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

    #region Commands

    [ServerToolCommand("clip")]
    async public Task<ApiEventResponse> OnClip(IBridge bridge, ApiToolEventArguments e)
    {
        if (!(e.Sketch is Polygon))
        {
            throw new Exception(bridge.LocalizeString(this, L10nKeys.DrawClipPolygonFirst));
        }

        var affectedFeaturesMethod = (AffectedFeaturesMethod)e.GetInt(EditClipAffectedFeaturesMethodId);

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId),
            CurrentMapSrsId = e.MapCrs.HasValue ? e.MapCrs.Value : 0
        };

        var features = (await e.FeaturesFromSelectionAsync(bridge))
                        .ThrowIfNullOrEmpty(() => "No features selected!");

        var editTheme = e.EditThemeFromSelection(bridge)
                         .ThrowIfNull(() => "Can't find edit theme for selelction");

        var editThemeDef = e.EditFeatureDefinitionFromSelection(bridge, features.First())
                            .ThrowIfNull(() => "Can't find edit theme for selelction");

        var sRef = bridge.CreateSpatialReference(e.CalcCrs.HasValue ? e.CalcCrs.Value : 0);
        var clipPolygon = (Polygon)e.Sketch;
        var method = e[EditClipFeatureMethodId];
        var newFeatures = new List<WebMapping.Core.Feature>();
        var affectedFeatures = new List<WebMapping.Core.Feature>();
        var createFeatures = new List<WebMapping.Core.Feature>();

        List<Shape> newShapesIntersection = new List<Shape>();
        List<Shape> newShapesDifference = new List<Shape>();
        List<Shape> newShapesXor = new List<Shape>();

        foreach (var originalFeature in features)
        {
            var originalShape = originalFeature.Shape;

            if (originalShape is Polyline)
            {
                #region Clip Polyline

                var clipResult = ((Polyline)originalShape).Clip(clipPolygon);

                if (affectedFeaturesMethod == AffectedFeaturesMethod.ApplyClipToIntersected && clipResult.intersect == null)
                {
                    // this shape is not affected
                    continue;
                }

                if (clipResult.intersect != null)
                {
                    newShapesIntersection.Add(clipResult.intersect);
                }

                if (clipResult.difference != null)
                {
                    newShapesDifference.Add(clipResult.difference);
                }

                #endregion
            }
            else if (originalShape is Polygon)
            {
                #region Clip Polygon

                IEnumerable<Polygon> intersection = null;

                if (affectedFeaturesMethod == AffectedFeaturesMethod.ApplyClipToIntersected)
                {
                    intersection = ((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctIntersection);
                    if (intersection == null || intersection.Count() == 0)
                    {
                        // this shape is not affected
                        continue;
                    }
                }

                newShapesIntersection.AddRange(intersection ?? ((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctIntersection));
                newShapesDifference.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctDifference));
                newShapesXor.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctXor));

                #endregion
            }
            else
            {
                throw new Exception($"Cut not supported for this shape/geometry-type: {originalFeature?.ToString()}");
            }
        }

        if (newShapesIntersection.Count() == 0 && newShapesDifference.Count() == 0 && newShapesXor.Count() == 0)
        {
            throw new Exception("Der Verschnitt liefert kein Ergebnis");
        }

        List<UIElement> menuItems = new List<UIElement>();

        newShapesIntersection.ToWGS84(bridge, e.CalcCrs.HasValue ? e.CalcCrs.Value : 0);
        newShapesDifference.ToWGS84(bridge, e.CalcCrs.HasValue ? e.CalcCrs.Value : 0);
        newShapesXor.ToWGS84(bridge, e.CalcCrs.HasValue ? e.CalcCrs.Value : 0);

        if (newShapesIntersection.Count() > 0 && newShapesDifference.Count() > 0)
        {
            menuItems.Add(new UIMenuItem(this, e)
            {
                text = bridge.LocalizeString(this, L10nKeys.ClipIntersectedAndDifference),
                value = ClipMethod.Both.ToString(),
                icon_large = UIImageButton.ToolResourceImage(this, "construct_clip_polygon_both_128"),
                highlight_feature = bridge.ToGeoJson(new WebMapping.Core.Collections.FeatureCollection(new WebMapping.Core.Feature()
                {
                    Shape = new[] { newShapesIntersection.ToMultipartShape(), newShapesDifference.ToMultipartShape() }.ToMultipartShape()
                }))
            });
        }

        if (newShapesIntersection.Count() > 0)
        {
            menuItems.Add(new UIMenuItem(this, e)
            {
                text = bridge.LocalizeString(this, L10nKeys.ClipIntersected),
                value = ClipMethod.Intersect.ToString(),
                icon_large = UIImageButton.ToolResourceImage(this, "construct_clip_polygon_intersect_128"),
                highlight_feature = bridge.ToGeoJson(new WebMapping.Core.Collections.FeatureCollection(new WebMapping.Core.Feature()
                {
                    Shape = newShapesIntersection.ToMultipartShape()
                }))
            });
        }

        if (newShapesDifference.Count() > 0)
        {
            menuItems.Add(new UIMenuItem(this, e)
            {
                text = bridge.LocalizeString(this, L10nKeys.ClipDifference),
                value = ClipMethod.Difference.ToString(),
                icon_large = UIImageButton.ToolResourceImage(this, "construct_clip_polygon_difference_128"),
                highlight_feature = bridge.ToGeoJson(new WebMapping.Core.Collections.FeatureCollection(new WebMapping.Core.Feature()
                {
                    Shape = newShapesDifference.ToMultipartShape()
                }))
            });
        }

        if (newShapesXor.Count() > 0)
        {
            menuItems.Add(new UIMenuItem(this, e)
            {
                text = bridge.LocalizeString(this, L10nKeys.ClipXor),
                value = ClipMethod.Xor.ToString(),
                icon_large = UIImageButton.ToolResourceImage(this, "construct_clip_polygon_xor_128"),
                highlight_feature = bridge.ToGeoJson(new WebMapping.Core.Collections.FeatureCollection(new WebMapping.Core.Feature()
                {
                    Shape = newShapesXor.ToMultipartShape(sRef?.Id ?? 0)
                }))
            });
        }

        var response = new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                //new UIHidden()
                //{
                //    id = CurrentFeatureAttributeId,
                //    value = featureOid,
                //    css = UICss.ToClass(new[]{ UICss.ToolParameter })
                //},
                new UIDiv()
                {
                    target = UIElementTarget.tool_modaldialog.ToString(),
                    elements = new IUIElement[]
                    {
                        new UIMenu()
                        {
                            elements = menuItems.ToArray(),
                            header = bridge.LocalizeString(this, L10nKeys.ChooseResult)
                        },
                        new UIButtonGroup()
                        {
                            elements = new IUIElement[] {
                                new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                                {
                                    text = bridge.LocalizeString(this, L10nKeys.Cancel),
                                    css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionButtonStyle })
                                }
                            }
                        }
                    }
                }

            }.AppendRequiredDeleteOriginalHiddenElements(affectedFeatures.Select(f => f.Oid)),
            ToolSelection = ApiToolEventArguments.SelectionInfoClass.ClearSelection
        };

        return response;
    }

    #endregion

    async public Task<ApiEventResponse> OnPerformClip(IBridge bridge, ApiToolEventArguments e)
    {
        if (!(e.Sketch is Polygon))
        {
            throw new Exception("Bitte zuerst eine Verschnittfläche zeichnen");
        }

        var affectedFeaturesMethod = (AffectedFeaturesMethod)e.GetInt(EditClipAffectedFeaturesMethodId);

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId),
            CurrentMapSrsId = e.MapCrs.HasValue ? e.MapCrs.Value : 0
        };

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

        var clipPolygon = (Polygon)e.Sketch;
        if (!Enum.TryParse<ClipMethod>(e.MenuItemValue, out ClipMethod method))
        {
            throw new Exception("Clip method not defined");
        }

        var newFeatures = new List<WebMapping.Core.Feature>();
        var affectedFeatures = new List<WebMapping.Core.Feature>();

        foreach (var originalFeature in features)
        {
            List<Shape> newShapes = new List<Shape>();
            var originalShape = originalFeature.Shape;

            if (originalShape is Polyline)
            {
                var clipResult = ((Polyline)originalShape).Clip(clipPolygon);

                if (affectedFeaturesMethod == AffectedFeaturesMethod.ApplyClipToIntersected && clipResult.intersect == null)
                {
                    // this shape is not affected
                    continue;
                }

                if (method == ClipMethod.Both)
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
                else if (method == ClipMethod.Intersect)
                {
                    if (clipResult.intersect != null)
                    {
                        newShapes.Add(clipResult.intersect);
                    }
                }
                else if (method == ClipMethod.Difference)
                {
                    if (clipResult.difference != null)
                    {
                        newShapes.Add(clipResult.difference);
                    }
                }
            }
            else if (originalShape is Polygon)
            {
                IEnumerable<Polygon> intersection = null;

                if (affectedFeaturesMethod == AffectedFeaturesMethod.ApplyClipToIntersected)
                {
                    intersection = ((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctIntersection);
                    if (intersection == null || intersection.Count() == 0)
                    {
                        // this shape is not affected
                        continue;
                    }
                }

                if (method == ClipMethod.Both)
                {
                    newShapes.AddRange(intersection ?? ((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctIntersection));
                    newShapes.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctDifference));
                }
                else if (method == ClipMethod.Intersect)
                {
                    newShapes.AddRange(intersection ?? ((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctIntersection));
                }
                else if (method == ClipMethod.Difference)
                {
                    newShapes.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctDifference));
                }
                else if (method == ClipMethod.Xor)
                {
                    newShapes.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctXor));
                }
            }
            else
            {
                throw new Exception($"Cut not supported for this shape/geometry-type: {originalFeature?.ToString()}");
            }

            if (newShapes.Count() > 0)
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
            else if (affectedFeaturesMethod == AffectedFeaturesMethod.ApplyClipToAll)
            {
                affectedFeatures.Add(originalFeature);
            }
        }

        if (newFeatures.Count() == 0)
        {
            throw new Exception(bridge.LocalizeString(this, L10nKeys.ChooseResult));
        }

        if (e.GetEnumValue<MultipartBehavior>(EditClipAutoExplodeMultipartFeaturesId) == MultipartBehavior.DisolveMultipartFeatures)  // explode features
        {
            newFeatures = newFeatures.ExplodeMultipartFeatures().ToList();
        }

        if (!await editEnvironment.InserFeatures(editTheme, newFeatures))
        {
            throw new Exception(bridge.LocalizeString(this, L10nKeys.ErrorOnInsert));
        }

        return new ApiEventResponse()
        {
            ActiveTool = new DeleteSelectedSubsetOriginals()
            {
                ParentTool = new Edit()
            },
            RefreshServices = new string[] { editThemeDef.ServiceId },
            UISetters = await bridge.RequiredDeleteOriginalSetters(
                newFeatures.Select(f => f.Shape),
                affectedFeatures.Select(f => f.Shape),
                affectedFeatures.Select(f => f.Oid))
        };
    }

    #region Old Methods

    async public Task<ApiEventResponse> OnClip_old(IBridge bridge, ApiToolEventArguments e)
    {
        if (!(e.Sketch is Polygon))
        {
            throw new Exception("Bitte zuerst eine Verschnittfläche zeichnen");
        }

        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features == null || features.Count == 0)
        {
            throw new ArgumentException("No features selected!");
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
                        targetwidth = "640px",
                        elements = new IUIElement[]
                        {
                            new UISelect()
                            {
                                id = EditClipAffectedFeaturesMethodId,
                                css = UICss.ToClass(new []{ UICss.ToolParameter}),
                                options = new UISelect.Option[]
                                {
                                    new UISelect.Option() { value=((int)(AffectedFeaturesMethod.ApplyClipToIntersected)).ToString(), label="Nur auf geschnittene Objekte anwenden" },
                                    new UISelect.Option() { value=((int)(AffectedFeaturesMethod.ApplyClipToAll)).ToString(), label="Auf alle Objekte anwenden" }
                                }
                            },
                            new UIBreak(1),
                            new UISelect()
                            {
                                id = EditClipAutoExplodeMultipartFeaturesId,
                                css =UICss.ToClass(new[] {UICss.ToolParameter}),
                                options = new UISelect.Option[]
                                {
                                    new UISelect.Option() { value="0", label="Multiparts auflösen" },
                                    new UISelect.Option() { value="1", label="Geclippte Features bleiben Multiparts Features"}
                                }
                            },
                            new UIBreak(1),
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

    async public Task<ApiEventResponse> OnPerformClip_old(IBridge bridge, ApiToolEventArguments e)
    {
        if (!(e.Sketch is Polygon))
        {
            throw new Exception("Bitte zuerst eine Verschnittfläche zeichnen");
        }

        var affectedFeaturesMethod = (AffectedFeaturesMethod)e.GetInt(EditClipAffectedFeaturesMethodId);

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId),
            CurrentMapSrsId = e.MapCrs.HasValue ? e.MapCrs.Value : 0
        };

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

        var clipPolygon = (Polygon)e.Sketch;
        var method = e[EditClipFeatureMethodId];
        var newFeatures = new List<WebMapping.Core.Feature>();
        var affectedFeatures = new List<WebMapping.Core.Feature>();

        foreach (var originalFeature in features)
        {
            List<Shape> newShapes = new List<Shape>();
            var originalShape = originalFeature.Shape;

            if (originalShape is Polyline)
            {
                var clipResult = ((Polyline)originalShape).Clip(clipPolygon);

                if (affectedFeaturesMethod == AffectedFeaturesMethod.ApplyClipToIntersected && clipResult.intersect == null)
                {
                    // this shape is not affected
                    continue;
                }

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
                IEnumerable<Polygon> intersection = null;

                if (affectedFeaturesMethod == AffectedFeaturesMethod.ApplyClipToIntersected)
                {
                    intersection = ((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctIntersection);
                    if (intersection == null || intersection.Count() == 0)
                    {
                        // this shape is not affected
                        continue;
                    }
                }

                if (method == UIImageButton.ToolResourceImage(this, "construct_clip_polygon_both_128"))
                {
                    newShapes.AddRange(intersection ?? ((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctIntersection));
                    newShapes.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctDifference));
                }
                else if (method == UIImageButton.ToolResourceImage(this, "construct_clip_polygon_intersect_128"))
                {
                    newShapes.AddRange(intersection ?? ((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctIntersection));
                }
                else if (method == UIImageButton.ToolResourceImage(this, "construct_clip_polygon_difference_128"))
                {
                    newShapes.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctDifference));
                }
                else if (method == UIImageButton.ToolResourceImage(this, "construct_clip_xor_128"))
                {
                    newShapes.AddRange(((Polygon)originalShape).Clip(clipPolygon, WebMapping.Core.Geometry.Clipper.ClipType.ctXor));
                }
            }
            else
            {
                throw new Exception($"Cut not supported for this shape/geometry-type: {originalFeature?.ToString()}");
            }

            if (newShapes.Count() > 0)
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
            else if (affectedFeaturesMethod == AffectedFeaturesMethod.ApplyClipToAll)
            {
                affectedFeatures.Add(originalFeature);
            }
        }

        if (newFeatures.Count() == 0)
        {
            throw new Exception("Der Verschnitt liefert kein Ergebnis");
        }

        if (e[EditClipAutoExplodeMultipartFeaturesId] == "0")  // explode features
        {
            newFeatures = newFeatures.ExplodeMultipartFeatures().ToList();
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
            UIElements = new IUIElement[]
            {
                new UIEmpty()  // close dialog
                {
                    target = UIElementTarget.modaldialog.ToString()
                }
            },
            UISetters = await bridge.RequiredDeleteOriginalSetters(
                newFeatures.Select(f => f.Shape),
                affectedFeatures.Select(f => f.Shape),
                affectedFeatures.Select(f => f.Oid))
        };
    }

    #endregion
}
