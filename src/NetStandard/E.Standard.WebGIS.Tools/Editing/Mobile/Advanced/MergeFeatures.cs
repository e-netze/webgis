using E.Standard.Localization.Abstractions;
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
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing.Mobile.Advanced;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true, MapCrsDependent = true)]
[LocalizationNamespace("tools.editing.merge")]
public class MergeFeatures : IApiServerToolLocalizableAsync<MergeFeatures>, 
                             IApiChildTool
{
    const string CurrentFeatureAttributeId = "edit-mergefeature-feature-oid";
    const int _cancelationTokenTimeoutMilliseconds = 10000;

    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<MergeFeatures> localizer)
    {
        return await OnChangeFeature(bridge, e, localizer);
    }

    public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<MergeFeatures> localizer)
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

    public string Name => "Zusammenführen";

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
    async public Task<ApiEventResponse> OnMerge(IBridge bridge, ApiToolEventArguments e)
    {
        string featureOid = e[CurrentFeatureAttributeId];

        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId),
            CurrentMapSrsId = e.GetInt(Edit.EditMapCrsId)
        };

        var features = await e.FeaturesFromSelectionAsync(bridge);
        if (features.Count < 1)
        {
            throw new ArgumentException("Less than two features selected!");
        }

        var masterFeature = features.Where(f => f.Oid.ToString() == featureOid).FirstOrDefault();
        if (masterFeature == null)
        {
            throw new ArgumentException("Unknown feature with id=" + featureOid);
        }

        var copyFeatures = features.Where(f => f.Oid.ToString() != featureOid).ToArray();

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

        //
        // DoTo: Transaction? Was ist, wenn Update oder Insert nicht hinhauen!?
        // Vorher überprüfen ob Editthema update und delete Rechte hat...
        // Eventuell den Update erst als letztes machen, damit Orignal Feature erst am schluss überschrieben wird?
        //

        #region Geometrie übertragen

        var feature = masterFeature.Clone(false);
        feature.Oid = 0;

        #region old method

        //foreach (var copyFeature in copyFeatures)
        //{
        //    feature.Shape.AppendMuiltiparts(copyFeature.Shape);
        //}

        #endregion

        //var sRef = bridge.CreateSpatialReference(e.CalcCrs.HasValue ? e.CalcCrs.Value : 0);
        //var tolerance = sRef != null && sRef.IsProjective ? 1e-3 : 1e-7;

        if (features.HasAllGeometry<Polygon>())
        {
            var polygons = new List<Polygon>(new[] { (Polygon)masterFeature.Shape });
            polygons.AddRange(copyFeatures.Select(f => (Polygon)f.Shape));

            using (var cts = new CancellationTokenSource(_cancelationTokenTimeoutMilliseconds))
            {
                feature.Shape = SpatialAlgorithms.FastMergePolygon(polygons, cts);
            }
        }
        else
        {
            foreach (var copyFeature in copyFeatures)
            {
                feature.Shape.AppendMuiltiparts(copyFeature.Shape);
            }
        }

        #endregion

        #region Insert Feature

        if (!await editEnvironment.InsertFeature(editTheme, feature))
        {
            throw new Exception("Can't update feature");
        }

        #endregion

        return new ApiEventResponse()
        {
            ActiveTool = new DeleteOriginal()
            {
                ParentTool = new Edit()
            },
            RefreshServices = new string[] { editThemeDef.ServiceId },
            RefreshSelection = true,

            UISetters = await bridge.RequiredDeleteOriginalSetters(feature.Shape, features.Select(f => f.Shape))
        };
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
            feature = features.Where(f => f.Oid.ToString() == featureOid).FirstOrDefault();
            if (feature == null)
            {
                throw new ArgumentException("Unknown feature with id=" + featureOid);
            }
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
                                             EditOperation.Merge,
                                             feature);

        List<IUIElement> uiElements = new List<IUIElement>(
            new IUIElement[]
            {
                new UITitle()
                {
                    label=$"{localizer.Localize("label-attributes-from")}:"
                },
                new UISelect(UIButton.UIButtonType.servertoolcommand,"changefeature")
                {
                    id=CurrentFeatureAttributeId,
                    css=UICss.ToClass(new string[] { UICss.ToolParameter }),
                    style="background:red;color:white",
                    options=features.Select(f=>new UISelect.Option()
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
        List<IUISetter> setters = new List<IUISetter>(mask.UISetters);
        setters.Add(new UISetter(CurrentFeatureAttributeId, feature.Oid.ToString()));


        return new ApiEventResponse()
        {
            UIElements = mask.UIElements,
            UISetters = setters.ToArray(),
            ToolSelection = new ApiToolEventArguments.SelectionInfoClass(e.SelectionInfo, new int[] { feature.Oid })
        };
    }

    #endregion
}
