using E.Standard.Extensions.Compare;
using E.Standard.Localization.Abstractions;
using E.Standard.Platform;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
[ToolHelp("tools/map/downloadmapimage.html")]
[ToolConfigurationSection("print")]
[AdvancedToolProperties(MapCrsDependent = true, MapBBoxDependent = true, MapImageSizeDependent = true)]
public class DownloadMapImage : IApiServerToolLocalizable<DownloadMapImage>,
                                IApiButtonResources
{
    const string BBoxElementId = "donwloadmapimage-bbox";
    const string ImageSizeElementId = "downloadmapimage-size";
    const string OptionsContanerId = "downloadimage-tools";

    #region IApiServerTool

    public ToolType Type => ToolType.current_extent;

    public ToolCursor Cursor => ToolCursor.Crosshair;

    public string Name => "Download Map Image";

    public string Container => "Map";

    public string Image => UIImageButton.ToolResourceImage(this, "downloadmapimage");

    public string ToolTip => "Download (georeferenced) map image of the current frame";

    public bool HasUI => true;

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<DownloadMapImage> localizer)
    {
        var qualities = e.GetConfigDictionay<int, string>(Print.ConfigQualitiesDpi);

        if (qualities == null)
        {
            qualities = new Dictionary<int, string>()
            {
                { 120, localizer.Localize("quality-120") },
                { 150, localizer.Localize("quality-150") }
            };
        }

        if (!qualities.ContainsKey(96))
        {
            qualities.Add(96, localizer.Localize("quality-96"));
        }

        int epsg = e.CalcCrs.OrTake(e.MapCrs).OrTake(0).Value;

        var response = new ApiEventResponse()
            .AddUIElements(
                 new UIOptionContainer()
                    .WithId(OptionsContanerId)
                    .WithStyles(UICss.OptionContainerWithLabels)
                    .AddChildren(
                         new UIImageButton(this.GetType(), "display", UIButton.UIButtonType.servertoolcommand, "display")
                            .WithValue("display")
                            .WithText(localizer.Localize("current-extent")),
                         new UIImageButton(this.GetType(), "rectangle", UIButton.UIButtonType.servertoolcommand, "rectangle")
                            .WithValue("rectangle")
                            .WithText(localizer.Localize("select-bbox")),
                new UIBreak(1),
                new UILabel()
                    .WithLabel($"{localizer.Localize("bounding-box")}: {(e.MapCrsIsDynamic == false && epsg > 0 ? "[EPSG:" + epsg + "]" : "")}"),
                new UIBoundBoxInput()
                    .WithId(BBoxElementId)
                    .WithStyles(UICss.DownloadMapImageBBox)
                    .AsReadonly(),
                new UILabel()
                    .WithLabel($"{localizer.Localize("image-size")}:"),
                new UISizeInput()
                    .WithId(ImageSizeElementId)
                    .WithStyles(UICss.DownloadMapImageSize)
                    .AsReadonly(),
                //new UIBreak(1),
                new UILabel()
                    .WithLabel($"{localizer.Localize("resolution")}:"),
                new UISelect()
                    .WithStyles(UICss.DownloadMapImageDpi)
                    .AddOptions(qualities.Keys
                                         .OrderBy(dpi => dpi)
                                         .Select(dpi => new UISelect.Option()
                                                    .WithValue(dpi.ToString())
                                                    .WithLabel(qualities[dpi]))),
                new UILabel()
                    .WithLabel($"{localizer.Localize("image-format")}:"),
                new UISelect()
                    .WithStyles(UICss.DownloadMapImageFormat)
                    .AddOption(new UISelect.Option()
                                           .WithValue("jpg")
                                           .WithLabel(localizer.Localize("jpg-file")))
                    .AddOption(new UISelect.Option()
                                           .WithValue("png")
                                           .WithLabel(localizer.Localize("png-file"))),
                new UILabel()
                    .WithLabel($"{localizer.Localize("georeference")}:"),
                new UISelect()
                    .WithStyles(UICss.DownloadMapImageWorldfile)
                    .AddOption(new UISelect.Option()
                                           .WithValue("true")
                                           .WithLabel(localizer.Localize("image-and-worldfile")))
                    .AddOption(new UISelect.Option()
                                           .WithValue("false")
                                           .WithLabel(localizer.Localize("image-only"))))
            );

        if (e.CalcCrs.HasValue && !e.CalcCrs.Equals(e.MapCrs))
        {
            response
                .AddUIElement(
                    new UIDiv()
                        .WithStyles("webgis-info")
                        .AddChild(
                            new UILiteral()
                                .WithLiteral(String.Format(localizer.Localize("info1:body"), e.MapCrs.Value)))
                        );

        }

        response
            .AddUIElements(
                new UIBreak(1),
                new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.downloadmapimage)
                    .WithText(localizer.Localize("download"))
            );

        return AppendMapExtendSetters(response, bridge, e);
    }

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<DownloadMapImage> localizer)
    {
        return AppendMapExtendSetters(new ApiEventResponse(), bridge, e);
    }

    #endregion

    #region IApiButtonResources

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("downloadmapimage", Properties.Resources.download);
        toolResourceManager.AddImageResource("rectangle", Properties.Resources.rectangle);
        toolResourceManager.AddImageResource("display", Properties.Resources.display);
    }

    #endregion

    #region Commands

    [ServerToolCommand("display")]
    public ApiEventResponse OnSetBoundsAndSize(IBridge bridge, ApiToolEventArguments e)
    {
        return AppendMapExtendSetters(new ApiEventResponse()
        {
            ActiveToolType = this.Type,
            ToolCursor = this.Cursor
        }, bridge, e);
    }

    [ServerToolCommand("rectangle")]
    public ApiEventResponse OnRectangleToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveToolType = WebMapping.Core.Api.ToolType.box,
            ToolCursor = ToolCursor.Custom_Rectangle,
        };
    }

    [ServerToolCommand("box")]
    public ApiEventResponse OnBox(IBridge bridge, ApiToolEventArguments e)
    {
        return AppendMapExtendSetters(new ApiEventResponse(), bridge, e);
    }

    #endregion

    #region Helper

    private ApiEventResponse AppendMapExtendSetters(ApiEventResponse response, IBridge bridge, ApiToolEventArguments e)
    {
        if (response.UISetters == null)
        {
            response.UISetters = new List<IUISetter>();
        }

        double[] bbox;
        int[] size;

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

        response.UISetters.Add(new UISetter(BBoxElementId, String.Join(",", bbox.Select(v => v.ToPlatformNumberString()))));
        response.UISetters.Add(new UISetter(ImageSizeElementId, String.Join(",", size)));

        return response;
    }

    #endregion
}
