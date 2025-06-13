using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.ServiceResponses;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

static class ServiceExtensions
{
    async public static Task<ServiceResponse> RenderRestLegendResponse(this IMapService2 service, IRequestContext requestContext, string jsonResponse)
    {
        var serviceLegend = service as IMapServiceLegend;
        if (serviceLegend == null)
        {
            throw new ArgumentException("Service do not implement IServiceLegend");
        }

        string fileTitle = "legend_" + Guid.NewGuid().ToString("N").ToLower() + ".png";

        #region ImageProcessing Variables

        int heightOffset = 25;
        int imageHeight = 0;

        #endregion

        var legendResponse = JSerializer.Deserialize<Legend.LegendResponse>(jsonResponse);

        Dictionary<string, string> elementsToDisplayInLegendDict = new Dictionary<string, string>();
        List<Legend.Layer> legendLayers = new List<Legend.Layer>();

        #region Layer Visibility

        List<int> visibleIds = new List<int>();
        List<string> layerDefs = new List<string>();
        Dictionary<Legend.Layer, List<string>> legendValues = new Dictionary<Legend.Layer, List<string>>();

        var maxWidth = 0;
        using (var bitmap = Current.Engine.CreateBitmap(1, 1))
        using (var canvas = bitmap.CreateCanvas())
        using (var font = Current.Engine.CreateFont(SystemInfo.DefaultFontName, 10))
        {
            foreach (var serviceLayer in service.Layers)
            {
                if (serviceLayer is AnnotationLayer)
                {
                    continue;
                }
                if (serviceLayer != null)
                {
                    bool visible = false;
                    string legendAliasname = String.Empty;

                    visible = serviceLayer.Visible && service.LayerProperties.ShowInLegend(serviceLayer.ID);
                    legendAliasname = service.LayerProperties.LegendAliasname(serviceLayer.ID);

                    if (visible)
                    {
                        visible = WebGIS.CMS.Globals.VisibleInServiceMapScale(service.Map, serviceLayer);
                    }
                    if (visible && (serviceLegend.LegendOptMethod == LegendOptimization.Themes || serviceLegend.LegendOptMethod == LegendOptimization.Symbols))
                    {
                        SpatialFilter filter = new SpatialFilter(serviceLayer.IdFieldName, service.Map.Extent, 1000, 0);

                        if (serviceLayer is ILayer2)
                        {
                            visible = await ((ILayer2)serviceLayer).HasFeaturesAsync(filter, requestContext) > 0;
                        }
                    }
                    if (visible)
                    {
                        foreach (var legendLayer in legendResponse.Layers)
                        {
                            if (legendLayer.LayerId.ToString() == serviceLayer.ID)
                            {
                                foreach (var legendItem in legendLayer.Legend)
                                {
                                    maxWidth = Math.Max(
                                        maxWidth,
                                        String.IsNullOrEmpty(legendItem.Label)
                                            ? 0
                                            : (int)canvas.MeasureText(legendItem.Label, font).Width
                                        );
                                }
                                if (!String.IsNullOrEmpty(legendAliasname))
                                {
                                    legendLayer.LayerName = legendAliasname;
                                }
                                legendLayers.Add(legendLayer);

                                if (serviceLegend.LegendOptMethod == LegendOptimization.Symbols && service.Map.MapScale <= serviceLegend.LegendOptSymbolScale && serviceLayer is ILegendRendererHelper)
                                {
                                    #region Legende Optimieren

                                    var legendRendererHelper = (ILegendRendererHelper)serviceLayer;
                                    if (legendRendererHelper.LengendRendererType == LayerRendererType.UniqueValue)
                                    {
                                        SpatialFilter filter = new SpatialFilter(serviceLayer.IdFieldName, service.Map.Extent, 1000, 0);
                                        filter.SubFields = (
                                            (String.IsNullOrWhiteSpace(legendRendererHelper.UniqueValue_Field1) ? String.Empty : legendRendererHelper.UniqueValue_Field1 + " ") +
                                            (String.IsNullOrWhiteSpace(legendRendererHelper.UniqueValue_Field2) ? String.Empty : legendRendererHelper.UniqueValue_Field2 + " ") +
                                            (String.IsNullOrWhiteSpace(legendRendererHelper.UniqueValue_Field3) ? String.Empty : legendRendererHelper.UniqueValue_Field3)).Trim();

                                        // Domains nicht übersetzen, sonst stimmen Werte beim Optimieren unten nicht mehr zusammen
                                        filter.SuppressResolveAttributeDomains = true;

                                        FeatureCollection features = new FeatureCollection();
                                        await serviceLayer.GetFeaturesAsync(filter, features, requestContext);

                                        var uniqueValues = features.Select(f =>
                                        {
                                            StringBuilder sb = new StringBuilder();
                                            if (!String.IsNullOrWhiteSpace(legendRendererHelper.UniqueValue_Field1))
                                            {
                                                sb.Append(f[legendRendererHelper.UniqueValue_Field1]);
                                            }
                                            if (!String.IsNullOrWhiteSpace(legendRendererHelper.UniqueValue_Field2))
                                            {
                                                sb.Append(legendRendererHelper.UniqueValue_FieldDelimiter);
                                                sb.Append(f[legendRendererHelper.UniqueValue_Field2]);
                                            }
                                            if (!String.IsNullOrWhiteSpace(legendRendererHelper.UniqueValue_Field3))
                                            {
                                                sb.Append(legendRendererHelper.UniqueValue_FieldDelimiter);
                                                sb.Append(f[legendRendererHelper.UniqueValue_Field3]);
                                            }
                                            return sb.ToString();
                                        }).Distinct().ToList();

                                        legendValues[legendLayer] = uniqueValues;
                                    }

                                    #endregion
                                }
                                List<string> layerLegendValues = legendValues.ContainsKey(legendLayer) ? legendValues[legendLayer] : null;

                                maxWidth = Math.Max(maxWidth, (int)canvas.MeasureText(legendLayer.LayerName, font).Width);
                                foreach (var legend in legendLayer.Legend)
                                {
                                    #region Legende Optimieren

                                    if (layerLegendValues != null && legend.Values != null && legend.Values.Count() > 0)
                                    {
                                        bool hasValue = false;
                                        foreach (var val in legend.Values)
                                        {
                                            if (layerLegendValues.Contains(val))
                                            {
                                                hasValue = true;
                                                break;
                                            }
                                        }
                                        if (!hasValue)
                                        {
                                            continue;
                                        }
                                    }

                                    #endregion

                                    byte[] bytearray = Convert.FromBase64String(legend.ImageData);
                                    using (MemoryStream ms = new MemoryStream(bytearray))
                                    using (var legendImage = Current.Engine.CreateBitmap(ms))
                                    {
                                        imageHeight += Math.Max(legendImage.Height + 10, heightOffset);
                                        string label = String.IsNullOrWhiteSpace(legend.Label) ? legendLayer.LayerName : legend.Label;
                                        maxWidth = Math.Max(maxWidth, Math.Max(legendImage.Width + 10, 40) + (int)canvas.MeasureText(label, font).Width);
                                    }
                                }
                                if (legendLayer.Legend.Length > 1)
                                {
                                    imageHeight += 25;
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        if (imageHeight == 0)
        {
            return new ServiceResponse(service.Map.Services.IndexOf(service), service.ID);
        }

        int yOffset = 0;

        var stringFormat = Current.Engine.CreateDrawTextFormat();
        stringFormat.Alignment = StringAlignment.Near;
        stringFormat.LineAlignment = StringAlignment.Near;

        using (var bitmap = Current.Engine.CreateBitmap(maxWidth + 40, imageHeight))
        using (var canvas = bitmap.CreateCanvas())
        using (var iconLabelFontStyle = Current.Engine.CreateFont(SystemInfo.DefaultFontName, 10))
        using (var headlineFontStyle = Current.Engine.CreateFont(SystemInfo.DefaultFontName, 10, FontStyle.Bold))
        using (var blackBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
        {
            canvas.Clear(ArgbColor.White);
            canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

            foreach (var legendLayer in legendLayers)
            {
                var xOffset = 0;
                if ((legendLayer.Legend.Count() > 1))
                {
                    canvas.DrawText(legendLayer.LayerName, headlineFontStyle, blackBrush, new CanvasPointF(5, yOffset), stringFormat); // headline
                    xOffset = 10;
                    yOffset += heightOffset;
                }
                List<string> layerLegendValues = legendValues.ContainsKey(legendLayer) ? legendValues[legendLayer] : null;

                foreach (var legend in legendLayer.Legend)
                {
                    #region Legende Optimieren

                    if (layerLegendValues != null && legend.Values != null && legend.Values.Count() > 0)
                    {
                        bool hasValue = false;
                        foreach (var val in legend.Values)
                        {
                            if (layerLegendValues.Contains(val))
                            {
                                hasValue = true;
                                break;
                            }
                        }
                        if (!hasValue)
                        {
                            continue;
                        }
                    }

                    #endregion
                    //string iconBase64String = legendLayer.Legend[indexl].ImageData; // TODO -> mehrere Elemente
                    //string iconLabelText = legendLayer.Legend[indexl].Label; // TOOD -> mehrere Elemente

                    byte[] bytearray = Convert.FromBase64String(legend.ImageData);
                    using (MemoryStream ms = new MemoryStream(bytearray))
                    using (var legendImage = Current.Engine.CreateBitmap(ms))
                    {
                        // icon and text
                        canvas.DrawBitmap(legendImage, new CanvasPoint(xOffset + 5, yOffset));

                        string label = String.IsNullOrWhiteSpace(legend.Label) ? legendLayer.LayerName : legend.Label;
                        canvas.DrawText(label, iconLabelFontStyle, blackBrush, new CanvasPointF(xOffset + Math.Max(legendImage.Width + 10, 40), yOffset), stringFormat);

                        yOffset += Math.Max(heightOffset, legendImage.Height + 10);
                    }
                }
            }

            await bitmap.SaveOrUpload(service.Map.AsOutputFilename(fileTitle), ImageFormat.Png);
        }

        return new ImageLocation(service.Map.Services.IndexOf(service), service.ID.ToString(),
                           service.Map.AsOutputFilename(fileTitle),
                           service.Map.AsOutputUrl(fileTitle));
    }
}
