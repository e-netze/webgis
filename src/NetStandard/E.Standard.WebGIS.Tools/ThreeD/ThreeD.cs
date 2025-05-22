using E.Standard.Drawing.Models;
using E.Standard.Extensions.Compare;
using E.Standard.Localization.Abstractions;
using E.Standard.Platform;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Helpers;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.ThreeD;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(ClientDeviceDependent = true, MapBBoxDependent = true, ScaleDependent = true, MapImageSizeDependent = true, MapCrsDependent = true)]
[ToolConfigurationSection("threed")]
[ToolHelp("tools/general/measure-3d.html")]
public class ThreeD : IApiServerToolLocalizableAsync<ThreeD>,
                      IApiButtonResources
{
    const string BBoxElementId = "threed-bbox";
    const string ImageSizeElementId = "threed-size";
    const string ResolutionElementId = "threed-resolution";
    const string TextureElementId = "threed-texture";
    const string OptionsContanerId = "threed-tools";
    const string TerrainModelNameId = "threed-terrainmodel-name";

    #region IApiServerTool Member

    public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<ThreeD> localizer)
    {
        var textureOptions = new List<UISelect.Option>();

        textureOptions.AddRange(new UISelect.Option[]
        {
            new UISelect.Option() { value="0", label = localizer.Localize("texture-monochrome") },
            new UISelect.Option() { value="1", label = localizer.Localize("texture-current-map") },

            new UISelect.Option() { value="2", label= localizer.Localize("texture-orhtophoto") },
            new UISelect.Option() { value="3", label= localizer.Localize("texture-orhtophoto-with-streets") }
        });

        var texturePreviewDict = new Dictionary<string, string>
        {
            { "0", $"{ bridge.AppRootUrl }/content/api/img/3d/monochrome.jpg" },
            { "1", $"{ bridge.AppRootUrl }/content/api/img/3d/map.jpg" },

            { "2", $"{ bridge.AppRootUrl }/content/api/img/3d/ortho.jpg" },
            { "3", $"{ bridge.AppRootUrl }/content/api/img/3d/ortho-streets.jpg" }
        };

        int epsg = e.CalcCrs.OrTake(e.MapCrs).OrTake(0).Value;

        var response = new ApiEventResponse()
            .AddUIElements(
                new UIDiv()
                    .WithStyles("webgis-info")
                    .AddChild(
                         new UILiteral()
                            .WithLiteral(localizer.Localize("description:body"))),
                new UIOptionContainer()
                    .WithId(OptionsContanerId)
                    .WithStyles(UICss.OptionContainerWithLabels)
                    .AddChildren(
                        new UIImageButton(this.GetType(), "display", UIButton.UIButtonType.servertoolcommand, "display")
                            .WithValue("display")
                            .WithText(localizer.Localize("current-extent")),
                        new UIImageButton(this.GetType(), "rectangle", UIButton.UIButtonType.servertoolcommand, "rectangle")
                            .WithValue("rectangle")
                            .WithText(localizer.Localize("select-box"))),
                new UILabel()
                    .WithLabel($"{localizer.Localize("bbox")}: {(e.CalcCrsIsDynamic == false && epsg > 0 ? "[EPSG:" + epsg + "]" : "")}"),
                new UIBoundBoxInput()
                    .WithId(BBoxElementId)
                    .AsReadonly()
                    .AsToolParameter(),
                new UISizeInput() { style = "display:none" }
                    .WithId(ImageSizeElementId)
                    .AsReadonly()
                    .WithStyles(UICss.DownloadMapImageSize),
                new UILabel()
                    .WithLabel($"{localizer.Localize("elevation-model")}:"),
                new UISelect()
                    .WithId(TerrainModelNameId)
                    .AsToolParameter()
                    .AddOptions(new RasterQueryHelper()
                                    .GetHadNodeNames(bridge, $"{bridge.AppEtcPath}/3d/default.xml", new string[] { "gview-image" })
                                    .Select(n => new UISelect.Option()
                                                             .WithValue(n)
                                                             .WithLabel(n))),
                new UILabel()
                    .WithLabel($"{localizer.Localize("resolution-m")}:"),
                new UIInputNumber()
                {
                    MinValue = e.GetConfigDouble("min-resolution", 1D),
                    MaxValue = e.GetConfigDouble("max-resolution", 100D),
                    StepWidth = e.GetConfigDouble("min-resolution", 1D)
                }.WithId(ResolutionElementId).AsToolParameter(),
                new UIDiv()
                    .WithStyles("webgis-info")
                    .AddChild(new UILiteral()
                                  .WithLiteral(localizer.Localize("resolution-info"))),
                new UILabel()
                    .WithLabel($"{localizer.Localize("label-texture")}:"),
                new UISelect()
                    .WithId(TextureElementId)
                    .AsToolParameter()
                    .AddOptions(textureOptions),
                new UILabel()
                    .WithLabel($"{localizer.Localize("preview")}:"),
                new UIDiv()
                    .AddChildren(texturePreviewDict.Keys.Select(k =>
                    {
                        return new UIConditionDiv()
                        {
                            ConditionType = UIConditionDiv.ConditionTypes.ElementValue,
                            ContitionElementId = TextureElementId,
                            ConditionArguments = new string[] { k },
                            ConditionResult = true,
                            elements = new IUIElement[]
                            {
                                new UIImage(texturePreviewDict[k]) { style="max-width:125px;border-radius:7px" }
                            }
                        };
                    })),
                new UIBreak(2),
                new UIButton(UIButton.UIButtonType.servertoolcommand, "build-threed-model")
                    .WithText(localizer.Localize("create-3d-model"))
            );

        return Task.FromResult(AppendMapExtendSetters(response, bridge, e));
    }

    public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<ThreeD> localizer)
        => Task.FromResult(AppendMapExtendSetters(new ApiEventResponse(), bridge, e));


    #endregion

    #region IApiTool Member

    public ToolType Type => ToolType.current_extent;

    public ToolCursor Cursor => ToolCursor.Crosshair;

    #endregion

    #region IApiButton Member

    public string Name => "3D (Measuring)";

    public string Container => "Tools";

    public string Image => UIImageButton.ToolResourceImage(this, "3d");

    public string ToolTip => "display the current map section as a 3D model";

    public bool HasUI => true;

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("3d", Properties.Resources._3d_model_26);
        toolResourceManager.AddImageResource("rectangle", Properties.Resources.rectangle);
        toolResourceManager.AddImageResource("display", Properties.Resources.display);
    }

    #endregion

    #region Commands

    [ServerToolCommand("display")]
    public ApiEventResponse OnSetBoundsAndSize(IBridge bridge, ApiToolEventArguments e)
        => AppendMapExtendSetters(new ApiEventResponse()
                                    .SetActiveToolType(this.Type)
                                    .SetActiveToolCursor(this.Cursor), bridge, e);


    [ServerToolCommand("rectangle")]
    public ApiEventResponse OnRectangleToolClick(IBridge bridge, ApiToolEventArguments e)
        => new ApiEventResponse()
                .SetActiveToolType(WebMapping.Core.Api.ToolType.box)
                .SetActiveToolCursor(ToolCursor.Custom_Rectangle);

    [ServerToolCommand("box")]
    public ApiEventResponse OnBox(IBridge bridge, ApiToolEventArguments e)
        => AppendMapExtendSetters(new ApiEventResponse(), bridge, e);


    [ServerToolCommand("build-threed-model")]
    public async Task<ApiEventResponse> OnBuildModel(IBridge bridge, ApiToolEventArguments e, ILocalizer<ThreeD> localizer)
    {
        var bbox = new Envelope(e.GetArray<double>(BBoxElementId));

        int bbox_epsg = e.CalcCrs.OrTake(e.MapCrs).OrTake(0).Value, map_epsg = e.MapCrs.OrTake(0).Value;
        if (bbox_epsg != map_epsg)
        {
            using (var transformer = new GeometricTransformerPro(bridge.CreateSpatialReference(bbox_epsg), bridge.CreateSpatialReference(map_epsg)))
            {
                transformer.Transform(bbox);
            }
        }

        bbox.SrsId = map_epsg;

        var resolution = e.GetDouble(ResolutionElementId);

        var size = new Dimension();
        var aspect = bbox.Width / bbox.Height;

        if (aspect >= 1)
        {
            size.Width = (int)(bbox.Width / resolution);
            size.Height = (int)(size.Width / aspect);
        }
        else
        {
            size.Height = (int)(bbox.Height / resolution);
            size.Width = (int)(size.Height * aspect);
        }

        var maxSize = e.GetConfigInt("max-model-size");
        if (Math.Max(size.Width, size.Height) > maxSize * 1.01)   // 1% Spatzi
        {
            throw new Exception(localizer.Localize("exception-area-to-large"));
        }

        var result = await new RasterQueryHelper().PerformHeightQueryAsync(
            bridge,
            bbox,
            size,
            resolution,
            bridge.AppEtcPath + @"/3d/default.xml",
            e[TerrainModelNameId]);

        return new ThreeDResponse()
        {
            BoundingBox = result.BoundingBox,
            BoundBoxEpsg = result.BoundingBoxEpsg,
            ArraySize = new int[] { result.ArraySize[1], result.ArraySize[0] },
            Values = result.Data, //NormalizeData(result.Data)

            Texture = (ThreeDTexture)e.GetInt(TextureElementId),
            TextureOrthoService = e.GetConfigValue("texture-ortho-service"),
            TextureStreetsOverlayService = e.GetConfigValue("texture-streets-overlay-service")
        };
    }

    #endregion

    #region Helper

    #region 3D Points

    private (List<Point> queryPoints, double[] bbox, int arrayWidth, int arrayHeight) GenerateQueryPoints(ApiToolEventArguments e, int stepWidth = 20)
    {
        var bbox = e.MapBBox();
        var size = e.MapSize();

        double stepLng = (bbox[2] - bbox[0]) / size[0],
               stepLat = (bbox[3] - bbox[1]) / size[1];

        List<Point> queryPoints = new List<Point>();

        int arrayWidth = 0, arrayHeight = 0, x, y;

        for (y = size[1], arrayHeight = 0; y >= 0; y -= stepWidth, arrayHeight++)
        {
            for (x = 0, arrayWidth = 0; x <= size[0]; x += stepWidth, arrayWidth++)
            {
                queryPoints.Add(new Point(bbox[0] + x * stepLng, bbox[1] + y * stepLat));
            }
        }

        bbox = new double[] {
                queryPoints.Select(p=>p.X).Min(),
                queryPoints.Select(p=>p.Y).Min(),
                queryPoints.Select(p=>p.X).Max(),
                queryPoints.Select(p=>p.Y).Max()
        };

        return (queryPoints, bbox, arrayWidth, arrayHeight);
    }

    private float[] NormalizePointZ(List<Point> points)
    {
        var max = points.Where(p => !double.IsNaN(p.Z)).Select(p => p.Z).Max();
        var min = points.Where(p => !double.IsNaN(p.Z)).Select(p => p.Z).Min();

        var median = (max + min) / 2;

        return points.Select(p =>
        {
            if (double.IsNaN(p.Z))
            {
                return -median / 2.0;
            }

            return p.Z - median;
        })
            .Select(d => (float)d)
            .ToArray();
    }

    private float[] NormalizeData(float[] data)
    {
        var max = data.Where(p => p != 0f).Select(p => p).Max();
        var min = data.Where(p => p != 0f).Select(p => p).Min();

        var median = (max + min) / 2;

        return data.Select(p => p - median).ToArray();
    }

    #endregion

    private ApiEventResponse AppendMapExtendSetters(ApiEventResponse response, IBridge bridge, ApiToolEventArguments e)
    {
        if (response.UISetters == null)
        {
            response.UISetters = new List<IUISetter>();
        }

        double[] bbox;
        int[] size;
        var maxModelSize = e.GetConfigInt("max-model-size", 100);
        var minResolution = e.GetConfigDouble("min-resolution", 1D);
        var maxResolution = e.GetConfigDouble("max-resolution", 10D);

        var calcSRef = bridge.CreateSpatialReference(e.CalcCrs.OrTake(e.MapCrs).OrTake(0).Value);

        if (e.IsBoxEvent)
        {
            var click = e.ToMapProjectedClickEvent();

            var envelope = (Envelope)click.Sketch;

            if (!e.CalcCrs.Equals(e.MapCrs))
            {
                using (var transformer = new GeometricTransformerPro(bridge.CreateSpatialReference(e.MapCrs.Value), calcSRef))
                {
                    transformer.Transform(envelope);
                }
            }

            bbox = new[] { envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY };
            size = click.Size;
        }
        else
        {
            bbox = e.MapBBox();
            size = e.MapSize();

            #region Project

            using (var transformer = new GeometricTransformerPro(bridge.CreateSpatialReference(4326), calcSRef))
            {
                var ll = new Point(bbox[0], bbox[1]);  // Lower Left
                var ur = new Point(bbox[2], bbox[3]);  // Upper Right Cornder

                transformer.Transform(ll);
                transformer.Transform(ur);

                bbox = new double[]
                {
                    ll.X, ll.Y,
                    ur.X, ur.Y
                };
            }

            #endregion
        }

        if (calcSRef.IsProjective)
        {
            bbox = bbox.Select(v => Math.Round(v, 2))
                       .ToArray();
        }
        else
        {
            throw new Exception("This tool can only be used in maps with projected coordinates system with demension meters");
        }

        var maxDimension = Math.Max(Math.Abs(bbox[2] - bbox[0]), Math.Abs(bbox[3] - bbox[1]));
        var bestResolution = Math.Min(maxResolution, Math.Max(minResolution, Math.Round(maxDimension / maxModelSize, 2)));

        response.UISetters.Add(new UISetter(BBoxElementId, String.Join(",", bbox.Select(v => v.ToPlatformNumberString()))));
        response.UISetters.Add(new UISetter(ImageSizeElementId, String.Join(",", size)));
        response.UISetters.Add(new UISetter(ResolutionElementId, bestResolution.ToPlatformNumberString()));

        return response;
    }

    #endregion
}
