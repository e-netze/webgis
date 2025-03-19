using E.Standard.Extensions.Compare;
using E.Standard.Localization.Abstractions;
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
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Geometry.Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Desktop.Advanced;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true, MapCrsDependent = true)]
public class MergeFeatures : IApiServerToolLocalizableAsync<MergeFeatures>, IApiChildTool
{
    const string CurrentFeatureAttributeId = "edit-mergefeature-feature-oid";
    const string MergeMethodId = "edit-merge-methode";
    const int _cancelationTokenTimeoutMilliseconds = 10000;

    enum PolylineMergeMethod
    {
        CreateMultipart = 0,
        CreateSinglepart = 1
    }

    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<MergeFeatures> localizer)
    {
        return await OnChangeFeature(bridge, e, localizer);
    }

    async public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<MergeFeatures> localizer)
    {
        string featureOid = e[CurrentFeatureAttributeId];
        var shape = e.MenuItemValue?.ShapeFromWKT()
                     .ThrowIfNull(() => "Can't tetermine geometry");


        var features = (await e.FeaturesFromSelectionAsync(bridge))
                               .ThrowIfNullOrCountLessThan(2, () => "Less than two features selected!");

        var masterFeature = features.Where(f => f.Oid.ToString() == featureOid).FirstOrDefault()
                                    .ThrowIfNull(() => $"Unknown feature with id={featureOid}");

        var sRef = bridge.CreateSpatialReference(e.CalcCrs.HasValue ? e.CalcCrs.Value : 0);

        var newFeature = masterFeature.Clone(false);
        newFeature.Oid = 0;
        newFeature.Shape = shape;
        shape.SrsId = sRef?.Id ?? 0;

        return await MergeAndStore(bridge, e, features, newFeature);
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

    public string Name => L10nKeys.MergeObjects;

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

    [ServerToolCommand("merge")]
    async public Task<ApiEventResponse> OnMerge(IBridge bridge, ApiToolEventArguments e, ILocalizer<MergeFeatures> localizer)
    {
        string featureOid = e[CurrentFeatureAttributeId];

        var features = (await e.FeaturesFromSelectionAsync(bridge))
                               .ThrowIfNullOrCountLessThan(2, () => "Less than two features selected!");

        var masterFeature = features.Where(f => f.Oid.ToString() == featureOid).FirstOrDefault()
                                    .ThrowIfNull(() => $"Unknown feature with id={featureOid}");

        var copyFeatures = features.Where(f => f.Oid.ToString() != featureOid).ToArray();

        #region Geometrie übertragen

        var sRef = bridge.CreateSpatialReference(e.CalcCrs.HasValue ? e.CalcCrs.Value : 0);
        var tolerance = sRef != null && sRef.IsProjective ? 1e-3 : 1e-7;
        var createFeatures = new List<WebMapping.Core.Feature>();

        bool createPreview = true; // false

        if (features.HasAllGeometry<Polygon>())
        {
            var polygons = new List<Polygon>(new[] { (Polygon)masterFeature.Shape });
            polygons.AddRange(copyFeatures.Select(f => (Polygon)f.Shape));

            var feature = masterFeature.Clone(false);
            feature.Oid = 0;

            using (var cts = new CancellationTokenSource(_cancelationTokenTimeoutMilliseconds))
            {
                feature.Shape = SpatialAlgorithms.FastMergePolygon(polygons, cts);
                feature.Shape.SrsId = sRef?.Id ?? 0;
            }

            createFeatures.Add(feature);
        }
        else if (features.HasAllGeometry<Polyline>() && e.GetEnumValue<PolylineMergeMethod>(MergeMethodId) == PolylineMergeMethod.CreateSinglepart)
        {
            var polylines = new List<Polyline>(new[] { (Polyline)masterFeature.Shape });
            polylines.AddRange(copyFeatures.Select(f => (Polyline)f.Shape));

            using (var cts = new CancellationTokenSource(_cancelationTokenTimeoutMilliseconds))
            {
                foreach (var shape in polylines.MergeToSinglePart(cts, tolerance).OrderByDescending(f => f.Length))
                {
                    var feature = masterFeature.Clone(false);
                    feature.Oid = 0;
                    feature.Shape = shape;
                    feature.Shape.SrsId = sRef?.Id ?? 0;

                    createFeatures.Add(feature);
                }
            }

            createPreview = true;
        }
        else
        {
            var feature = masterFeature.Clone(false);
            feature.Oid = 0;
            feature.Shape.SrsId = sRef?.Id ?? 0;

            foreach (var copyFeature in copyFeatures)
            {

                feature.Shape.AppendMuiltiparts(copyFeature.Shape);
            }

            createFeatures.Add(feature);
        }

        #endregion

        if (createFeatures.Count == 0)
        {
            throw new Exception(localizer.Localize(L10nKeys.MergeHasNoResult));
        }

        if (createFeatures.Count == 1 && createPreview == false)
        {
            return await MergeAndStore(bridge, e, features, createFeatures[0]);
        }

        List<UIElement> menuItems = new List<UIElement>();

        foreach (var createFeature in createFeatures)
        {
            string subText = string.Empty;

            if (createFeature.Shape is Polyline)
            {
                subText = $"{localizer.Localize(L10nKeys.Length)}: {Math.Round(((Polyline)createFeature.Shape).Length, 2)}";
            }
            else if (createFeature.Shape is Polygon)
            {
                subText = $"{localizer.Localize(L10nKeys.Area)}: {Math.Round(((Polygon)createFeature.Shape).Area, 2)}";
            }

            string value = createFeature.Shape.WKTFromShape();

            using (var transformer = new GeometricTransformerPro(sRef, bridge.CreateSpatialReference(4326)))
            {
                var shape = createFeature.Shape;
                if (shape != null)
                {
                    transformer.Transform(shape);
                }
            }

            menuItems.Add(new UIMenuItem(this, e)
            {
                text = $"{localizer.Localize(L10nKeys.MergedObject)}: {createFeature.Shape.GetType().ToString().Split('.').Last()}{(createFeature.Shape.IsMultipart ? $" [{createFeature.Shape.Multiparts.Count()} {localizer.Localize(L10nKeys.Parts)}]" : "")}",
                subtext = subText,
                value = value,
                highlight_feature = bridge.ToGeoJson(new WebMapping.Core.Collections.FeatureCollection(createFeature))
            });
        }

        var response = new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                new UIHidden()
                {
                    id = CurrentFeatureAttributeId,
                    value = featureOid,
                    css = UICss.ToClass(new[]{ UICss.ToolParameter })
                },
                new UIDiv()
                {
                    target = UIElementTarget.tool_modaldialog.ToString(),
                    elements = new IUIElement[]
                    {
                        new UIMenu()
                        {
                            elements = menuItems.ToArray(),
                            header = localizer.Localize(L10nKeys.ChooseResult)
                        },
                        new UIButtonGroup()
                        {
                            elements = new IUIElement[] {
                                new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                                {
                                    text = localizer.Localize(L10nKeys.Cancel),
                                    css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionButtonStyle })
                                }
                            }
                        }
                    }
                }

            }.AppendRequiredDeleteOriginalHiddenElements(),
            ToolSelection = ApiToolEventArguments.SelectionInfoClass.ClearSelection
        };

        return response;

    }

    [ServerToolCommand("changefeature")]
    async public Task<ApiEventResponse> OnChangeFeature(IBridge bridge, ApiToolEventArguments e, ILocalizer<MergeFeatures> localizer)
    {
        string featureOid = e[CurrentFeatureAttributeId];

        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features.Count < 2)
        {
            throw new ArgumentException("Less than two features selected!");
        }

        WebMapping.Core.Feature feature = features[0];
        if (!string.IsNullOrWhiteSpace(featureOid))
        {
            feature = features.Where(f => f.Oid.ToString() == featureOid).FirstOrDefault()
                              .ThrowIfNull(() => $"Unknown feature with id={featureOid}");
        }

        var editTheme = e.EditThemeFromSelection(bridge)
                         .ThrowIfNull(() => "Can't find edit theme for selelction");

        var editThemeDef = e.EditFeatureDefinitionFromSelection(bridge, features[0])
                            .ThrowIfNull(() => "Can't find edit theme for selelction");

        var mask = await editTheme.ParseMask(bridge,
                                             editThemeDef.ToEditThemeDefinition(),
                                             EditOperation.Merge,
                                             feature);

        var uiElements = new List<IUIElement>();

        if (features.HasAllGeometry<Polyline>())
        {
            uiElements.AddRange(new IUIElement[]
            {
                new UITitle()
                {
                    label = localizer.Localize(L10nKeys.PolylineMergeMethod)
                },
                new UISelect()
                {
                    id = MergeMethodId,
                    css = UICss.ToClass(new[] { UICss.ToolParameter }),
                    options = EnumExtensions.ToUISelectOptions<PolylineMergeMethod>(localizer)
                },
                new UIInfoBox(MergeMethodId, EnumExtensions.ToDescriptionDictionary<PolylineMergeMethod>(localizer))
            });
        }

        uiElements.AddRange(
            new IUIElement[]
            {
                new UITitle()
                {
                    label = localizer.Localize(L10nKeys.MergeOriginFeature)
                },
                new UIInfoBox(localizer.Localize(L10nKeys.MergeOriginFeatureDesription)),
                new UISelect(UIButton.UIButtonType.servertoolcommand,"changefeature")
                {
                    id = CurrentFeatureAttributeId,
                    css = UICss.ToClass(new string[] { UICss.ToolParameter }),
                    style = "background:red;color:white",
                    options = features.Select(f=>new UISelect.Option()
                                              {
                                                  label=f.Oid.ToString(),
                                                  value=f.Oid.ToString()
                                              }).ToArray()
                }
            });

        // Copy Elements to Mask
        var maskContainer = mask.UIElements.Where(uiElement => uiElement.id == EditEnvironment.EditTheme.EditMaskContainerId).FirstOrDefault();
        var maskElements = maskContainer.elements.ToList();
        maskElements.InsertRange(0, uiElements);
        maskContainer.elements = maskElements.ToArray();

        // Append Setters
        List<IUISetter> setters = new List<IUISetter>(mask.UISetters)
        {
            new UISetter(CurrentFeatureAttributeId, feature.Oid.ToString())
        };
        if (features.HasAllGeometry<Polyline>())
        {
            setters.Add(new UISetter(MergeMethodId, e[MergeMethodId].OrTake(((int)PolylineMergeMethod.CreateSinglepart).ToString())));
        }

        return new ApiEventResponse()
        {
            UIElements = mask.UIElements,
            UISetters = setters.ToArray(),
            ToolSelection = new ApiToolEventArguments.SelectionInfoClass(e.SelectionInfo, new int[] { feature.Oid })
        };
    }

    #endregion

    async private Task<ApiEventResponse> MergeAndStore(IBridge bridge, ApiToolEventArguments e,
                                                       IEnumerable<WebMapping.Core.Feature> selectedFeatures,
                                                       WebMapping.Core.Feature newFeature)
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId),
            CurrentMapSrsId = e.GetInt(Edit.EditMapCrsId)
        };

        var editTheme = e.EditThemeFromSelection(bridge)
                         .ThrowIfNull(() => "Can't find edit theme for selelction");

        var editThemeDef = e.EditFeatureDefinitionFromSelection(bridge, selectedFeatures.First())
                            .ThrowIfNull(() => "Can't find edit theme for selelction");

        #region Insert Feature

        if (!await editEnvironment.InsertFeature(editTheme, newFeature))
        {
            throw new Exception("Can't update feature");
        }

        #endregion

        return new ApiEventResponse()
        {
            ActiveTool = new DeleteSelectedOriginals()
            {
                ParentTool = new Edit()
            },
            RefreshServices = new string[] { editThemeDef.ServiceId },
            RefreshSelection = true,

            UISetters = await bridge.RequiredDeleteOriginalSetters(newFeature.Shape, selectedFeatures.Select(f => f.Shape))
        };
    }
}
