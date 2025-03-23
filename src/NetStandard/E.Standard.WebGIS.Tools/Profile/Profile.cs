using E.Standard.Localization.Reflection;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Profile.QueryEngines;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.Drawing;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Profile;

[Export(typeof(IApiButton))]
[ToolCmsConfigParameter(ProfileEnvironment.CmsConfigParameter)]
[ToolHelp("tools/general/profile.html")]
[LocalizationNamespace("tools.profile")]
public class CreateProfile : IApiServerTool, IApiButtonResources
{
    #region IApiServerTool Members

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var profiles = new ProfileEnvironment(bridge).Profiles;

        return new ApiEventResponse()
            .AddUIElements(
                new UISelect()
                    .WithId("profile_name")
                    .AsToolParameter()
                    .AddOptions(profiles?.Select(profile =>
                        new UISelect.Option()
                            .WithValue(profile.Name)
                            .WithLabel(profile.Name))),
                new UIButtonContainer()
                    .AddChildren(
                        new UIButton(UIButton.UIButtonType.servertoolcommand, "printdialog")
                            .WithText("Drucken"),
                        new UIButton(UIButton.UIButtonType.servertoolcommand, "create")
                            .WithText("Profil erstellen"),
                        new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removesketch)
                            .WithText("Sketch entfernen")
                            .WithStyles(UICss.CancelButtonStyle)));
    }

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        return null;
    }

    #endregion

    #region IApiTool Members

    public ToolType Type => ToolType.sketch1d;

    public ToolCursor Cursor => ToolCursor.Crosshair;

    #endregion

    #region IApiButton Member

    public string Name => "Elevation Profile";

    public string Container => "Tools";

    public string Image => UIImageButton.ToolResourceImage(this, "profile");

    public string ToolTip => "Create an elevation profile along a digitized route.";

    public bool HasUI => true;

    #endregion

    #region Commands

    [ServerToolCommand("create")]
    [ServerToolCommand("refreshchart")]
    async public Task<ApiEventResponse> OnCreateProfileAsync(IBridge bridge, ApiToolEventArguments e)
    {
        var profileEnvironment = new ProfileEnvironment(bridge);
        var profile = profileEnvironment[e["profile_name"]];
        if (profile == null)
        {
            throw new ArgumentException("Unbekanntes Profil: " + e["profile_name"]);
        }

        var polyline = e.SketchWgs84 as Polyline;
        if (polyline == null || polyline.PathCount != 1)
        {
            throw new ArgumentException("Invalid Polyline. Only Singlepath...");
        }

        using (var transformer = bridge.GeometryTransformer(4326, profile.SrsId))
        {
            transformer.Transform(polyline);
        }

        #region Vertices

        #region Path Points

        List<PointM> vertices = new List<PointM>();
        List<PointM3> xGridVertices = new List<PointM3>();
        double stat = 0D;
        for (int p = 0; p < polyline[0].PointCount; p++)
        {
            if (p > 0)
            {
                stat += polyline[0][p - 1].Distance2D(polyline[0][p]);
            }

            vertices.Add(new PointM(polyline[0][p], stat));
            xGridVertices.Add(new PointM3(polyline[0][p], stat, "Vertex " + (p + 1), vertices[vertices.Count - 1]));
        }

        #endregion

        #region Stat Points

        double vertexIncrement = profile.VertexIncrement != 0 ? profile.VertexIncrement : ClosestStep(stat / 1000); ;
        //for (double len = profile.VertexIncrement; len < polyline[0].Length; len += profile.VertexIncrement)
        for (double len = vertexIncrement; len < polyline[0].Length; len += vertexIncrement)
        {
            vertices.Add(new PointM(SpatialAlgorithms.PolylinePoint(polyline, len), len));
        }

        vertices.Sort(new PointM.MComparer<double>());

        #endregion

        #endregion

        Dictionary<string, List<PointM>> serviceVertices = new Dictionary<string, List<PointM>>();

        if (!String.IsNullOrWhiteSpace(profile.ArcInfoGrid))
        {
            // ToDo
        }
        else if (profile.Server.Length > 0 &&
                 profile.Service.Length > 0 &&
                !String.IsNullOrWhiteSpace(profile.RasterTheme))
        {
            string serviceType = profile.ServiceType;
            int resultIndex = profile.ResultIndex;

            #region Service Request

            for (int s = 0; s < profile.Service.Length; s++)
            {
                if (serviceType == "ags" || serviceType == "ags-mosaic")
                {
                    await AgsEngine.QueryPoints(bridge, profile, s, vertices, stat, serviceVertices);
                }
                else // ims
                {
                    await ImsEngine.QueryPoints(bridge, profile, s, vertices, stat, serviceVertices);
                }
            }

            #endregion
        }

        #region Create Chart

        List<double> xAxis = new List<double>();
        Dictionary<string, List<double[]>> serviceData = new Dictionary<string, List<double[]>>();

        foreach (var point in vertices)
        {
            // Falls Wert Kommastelle hat => auf 2 Kommastelle genau anzeigen
            double roundValue = Math.Abs((double)point.M % 1) <= (Double.Epsilon * 100) ? (double)point.M : Math.Truncate((double)point.M * 100) / 100;
            xAxis.Add(roundValue);
        }

        foreach (var item in serviceVertices)
        {
            var helpList = new List<double>();
            foreach (var point in item.Value)
            {
                helpList.Add(point.Z);
            }
            serviceData[item.Key] = new List<double[]>() { helpList.ToArray() };
        }


        ChartBridge chart = new ChartBridge()
        {
            SketchConnected = true,
        };
        chart.Data.XAxis = new List<double[]>() { xAxis.ToArray() };
        chart.Data.DataDict = serviceData;
        //chart.Data.Data = new List<double[]>() { serviceData.ElementAt(0).Value[0] };
        chart.Data.Types = new List<string>() { "area" };
        chart.Data.Colors = profile.EarthColorHex;
        chart.Point.Show = profile.ShowPoints;

        #region Grid Lines

        foreach (var point in xGridVertices)
        {
            chart.AddGridXLine(new ChartBridge.ChartGridData.Line()
            {
                Value = (double)point.M,
                Text = (string)point.M2 // + ": " + Math.Round(((Point)point.M3).Z, 2)
            });
        }


        double horizontalLineStep = profile.VerticalLineStep != 0 ? profile.VerticalLineStep : ClosestStep(stat / 20);
        //for (double x = profile.HorizontalLineStep; x < polyline[0].Length; x += profile.HorizontalLineStep)
        for (double x = horizontalLineStep; x < polyline[0].Length; x += horizontalLineStep)
        {
            chart.AddGridXLine(new ChartBridge.ChartGridData.Line()
            {
                Value = x
            });
        }


        double minZ = serviceData.Min(s => s.Value[0].Min());
        double maxZ = serviceData.Max(s => s.Value[0].Max());
        double diff = maxZ - minZ;
        minZ -= diff * 0.05;   // 5%
        maxZ += diff * 0.05;
        double diffTolerance = maxZ - minZ;


        double verticalLineStep = profile.VerticalLineStep != 0 ? profile.VerticalLineStep : ClosestStep(diffTolerance / 20);
        //for (double y = 0D; y < maxZ; y += profile.VerticalLineStep)
        for (double y = 0D; y < maxZ; y += verticalLineStep)
        {
            if (y < minZ)
            {
                continue;
            }

            chart.AddGridYLine(new ChartBridge.ChartGridData.Line()
            {
                Value = y
            });
        }

        chart.SetAxisY(minZ, maxZ);

        #endregion

        #endregion

        return new ApiEventResponse()
        {
            Chart = chart
        };
    }

    [ServerToolCommand("printdialog")]
    public ApiEventResponse OnPrintDialog(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
            .AddUIElement(
                new UIDiv()
                    .AsDialog()
                    .WithDialogTitle("Profil drucken")
                    .WithStyles(UICss.NarrowFormMarginAuto)
                    .AddChildren(
                        new UILabel()
                            .WithLabel("Maßstab auswählen 1 : x"),
                        new UISelect(new string[] { "100", "500", "1000", "2500", "5000" })
                            .WithId("profile_print_scale")
                            .AsToolParameter(),

                        new UILabel()
                            .WithLabel("Überhöhung auswählen 1 : x"),
                        new UISelect(new string[] { "1", "2", "3", "0,5" })
                            .WithId("profile_print_superelevation")
                            .AsToolParameter(),

                        new UIButtonContainer(new UIButton(UIButton.UIButtonType.servertoolcommand, "print")
                            .WithText("Druckauftrag starten"))));
    }

    [ServerToolCommand("print")]
    async public Task<ApiEventResponse> OnPrintAsync(IBridge bridge, ApiToolEventArguments e)
    {
        ApiEventResponse chart = await OnCreateProfileAsync(bridge, e);

        int dpi = 150;
        int scale = Convert.ToInt32(e["profile_print_scale"]);
        float superelevation = Convert.ToSingle(e["profile_print_superelevation"]);
        string fontName = "Verdana";
        float paddingLeft_mm = 30F; float paddingRight_mm = 30F; float paddingTop_mm = 30F; float paddingBottom_mm = 30F;
        paddingLeft_mm = paddingRight_mm = paddingTop_mm = paddingBottom_mm = 30F;
        float fontTitleSize = 20F;
        float fontGridSize = 8F;
        float legendFontSize = 12F;

        float dpm = (float)(dpi / 0.0254);    // Dots per Meter   Bsp. 150 dpi = 5905 dpm
        float dpmm = (float)(dpi / 25.4);       // Dots per Millimeter  
        float pix = dpm / scale;       // Pixel je Meter   Bsp: 1m (bei 150dpi) = 2.36 pixel

        float distanceH = (float)chart.Chart.Data.XAxis[0].Last() * pix;
        float distanceV = (float)(chart.Chart.Axis.Y.Max - chart.Chart.Axis.Y.Min) * superelevation * pix;
        //float baseHeight = chart.Chart.Grid.Y == null ? 0F : (float)chart.Chart.Grid.Y.Lines?[0].Value;
        float baseHeight = (float)chart.Chart.Axis.Y.Min;
        Int32 imageWidth = (int)Math.Ceiling(distanceH + (paddingLeft_mm + paddingRight_mm) * dpmm);
        Int32 imageHeight = (int)Math.Ceiling(distanceV + (paddingBottom_mm + paddingTop_mm) * dpmm);

        float tickLength = 6F;

        float imageWidth_mm = imageWidth / (float)dpi * (float)25.4;
        float imageHeight_mm = imageHeight / (float)dpi * (float)25.4;

        if (imageWidth_mm > 1189f || imageHeight_mm > 841f)  // A0 841 x 1189
        {
            throw new Exception("Die Größe des Plans (" + (int)imageWidth_mm + " x " + (int)imageHeight_mm + ")mm übersteigt DIN A0 (1189 x 841)mm.\nWähle einen anderen Maßstab bzw. eine andere Überhöhung.");
        }

        using (var bitmap = Current.Engine.CreateBitmap(imageWidth, imageHeight))
        using (var canvas = bitmap.CreateCanvas())
        using (var canvasText = bitmap.CreateCanvas())
        using (var pen = Current.Engine.CreatePen(ArgbColor.Black, 2))
        {
            // Hintergrundfarbe
            canvas.Clear(ArgbColor.White);
            canvasText.Clear(ArgbColor.White);

            // Ursprung unten links
            bitmap.SetResolution(dpi, dpi);

            canvas.TranslateTransform(new CanvasPointF(0, imageHeight));
            canvas.ScaleTransform(1, -1);
            canvas.TranslateTransform(new CanvasPointF(paddingLeft_mm * dpmm, paddingBottom_mm * dpmm));
            canvasText.TranslateTransform(new CanvasPointF(paddingLeft_mm * dpmm, paddingBottom_mm * dpmm));
            //gr.PageUnit = GraphicsUnit.Millimeter


            #region Grid

            using (var penGrid = Current.Engine.CreatePen(ArgbColor.LightGray, 1f))
            using (var fontGrid = Current.Engine.CreateFont(fontName, fontGridSize))
            using (var brushGrid = Current.Engine.CreateSolidBrush(ArgbColor.FromArgb(50, 50, 50)))
            {
                //chart.Chart.Grid.X.Lines.Last().Value = Math.Truncate(chart.Chart.Grid.X.Lines.Last().Value * 10) / 10;     // Letzten Wert (immer auf Index 1) maximal auf 1 Kommastelle genau anzeigen
                foreach (var gridX in chart.Chart.Grid.X.Lines)
                {
                    canvas.DrawLine(penGrid, (float)gridX.Value * pix, -tickLength, (float)gridX.Value * pix, distanceV);
                    var textRectangle = new CanvasRectangleF((float)gridX.Value * pix - 30, distanceV + tickLength + 4, 60, fontGridSize + 2);
                    //grText.DrawRectangles(pen, new RectangleF[] { textRectangle });
                    //grText.DrawString(gridX.Value.ToString(), fontGrid, brushGrid, (float)gridX.Value * pix, distanceV + tickLength);

                    var stringFormatGridX = Current.Engine.CreateDrawTextFormat();

                    stringFormatGridX.Alignment = StringAlignment.Center;
                    stringFormatGridX.LineAlignment = StringAlignment.Center;

                    // Falls Wert Kommastelle hat => auf 1 Kommastelle genau anzeigen
                    gridX.Value = Math.Abs(gridX.Value % 1) <= (Double.Epsilon * 100) ? gridX.Value : Math.Truncate(gridX.Value * 10) / 10;
                    canvasText.DrawText(gridX.Value.ToString(), fontGrid, brushGrid, textRectangle.Center, stringFormatGridX);
                }

                if (chart.Chart.Grid.Y != null)
                {
                    foreach (var gridY in chart.Chart.Grid.Y.Lines)
                    {
                        canvas.DrawLine(penGrid, -tickLength, (float)(gridY.Value - baseHeight) * superelevation * pix, distanceH, (float)(gridY.Value - baseHeight) * pix * superelevation);
                        var textRectangle = new CanvasRectangleF(-60 - tickLength - 2, distanceV - ((float)(gridY.Value - baseHeight) * superelevation * pix + (fontGridSize + 2) / 2), 60, fontGridSize + 2);
                        //grText.DrawRectangles(pen, new RectangleF[] { textRectangle });

                        var stringFormatGridY = Current.Engine.CreateDrawTextFormat();

                        stringFormatGridY.Alignment = StringAlignment.Far;
                        stringFormatGridY.LineAlignment = StringAlignment.Center;

                        canvasText.DrawText(gridY.Value.ToString(), fontGrid, brushGrid, textRectangle.Center, stringFormatGridY);

                    }
                }
            }
            #endregion

            #region DEMs
            foreach (var item in chart.Chart.Data.DataDict.Select((Entry, Index) => new { Entry, Index }))
            {
                using (var path = Current.Engine.CreateGraphicsPath())
                {
                    string color = chart.Chart.Data.Colors[item.Index];

                    var points = new List<CanvasPointF>();
                    foreach (var point in item.Entry.Value[0].Select((elem, idx) => new { elem, idx }))
                    {
                        points.Add(new CanvasPointF((float)(chart.Chart.Data.XAxis[0][point.idx] * pix), (float)((point.elem - baseHeight) * superelevation * pix)));
                    }

                    for (int i = 1; i < points.Count; i++)
                    {
                        path.AddLine(points[i - 1], points[i]);
                    }

                    using (var penDEM = Current.Engine.CreatePen(ArgbColor.FromHexString(color), 2F))
                    {
                        canvas.DrawPath(penDEM, path);
                    }
                }
            }
            #endregion

            #region Legend
            int legendLength = chart.Chart.Data.DataDict.Count;
            float legendTextWidth = 100F;
            float legendOffsetX = 50F;
            float legendSymbolWidth = 25F;
            float legendSumWidth = legendLength * (legendSymbolWidth + legendTextWidth + legendOffsetX) - legendOffsetX; // letzten Offset weg
            var listSymbolRectangles = new List<CanvasRectangleF>();
            var listTextRectangles = new List<CanvasRectangleF>();
            foreach (var item in chart.Chart.Data.DataDict.Select((Entry, Index) => new { Entry, Index }))
            {
                float legendX = item.Index * (legendSymbolWidth + legendTextWidth + legendOffsetX);
                float legendXFromCenter = legendX - (legendSumWidth / 2) + distanceH / 2;
                listSymbolRectangles.Add(new CanvasRectangleF(legendXFromCenter, distanceV + 40, legendSymbolWidth, legendFontSize + 2));
                listTextRectangles.Add(new CanvasRectangleF(legendXFromCenter + legendSymbolWidth, distanceV + 40, legendTextWidth, legendFontSize + 2));
            }

            //listSymbolRectangles = listSymbolRectangles.Select(r => { r.Offset((-legendSumWidth / 2) + distanceH / 2, 0); return r; }).ToList();  // Offset: Void-Methode auf Value Wert
            //listTextRectangles = listTextRectangles.Select(r => { r.Offset((-legendSumWidth / 2) + distanceH / 2, 0); return r; }).ToList();      

            using (var fontLegend = Current.Engine.CreateFont(fontName, fontGridSize))
            using (var brushLegend = Current.Engine.CreateSolidBrush(ArgbColor.FromArgb(50, 50, 50)))
            {

                var stringFormatLegend = Current.Engine.CreateDrawTextFormat();
                stringFormatLegend.Alignment = StringAlignment.Near;
                stringFormatLegend.LineAlignment = StringAlignment.Center;

                for (int j = 0; j < legendLength; j++)
                {
                    //grText.DrawRectangles(pen, new RectangleF[] { listTextRectangles.ElementAt(j) });
                    canvasText.DrawText(chart.Chart.Data.DataDict.ElementAt(j).Key, fontLegend, brushLegend, listTextRectangles.ElementAt(j).Center, stringFormatLegend);
                    using (var brushLegendRectangle = Current.Engine.CreateSolidBrush(ArgbColor.FromHexString(chart.Chart.Data.Colors.ElementAt(j))))
                    {
                        canvasText.FillRectangle(brushLegendRectangle, listSymbolRectangles.ElementAt(j));
                    }
                }
            }
            #endregion

            #region Titel
            using (var fontTitle = Current.Engine.CreateFont(fontName, fontTitleSize, FontStyle.Bold))
            using (var fontSubTitle = Current.Engine.CreateFont(fontName, (float)(fontTitleSize * 0.7)))
            using (var brushTitle = Current.Engine.CreateSolidBrush(ArgbColor.FromArgb(50, 50, 50)))
            {
                var titleRectangle = new CanvasRectangleF((distanceH / 2) - 200, -80, 400, fontTitleSize + 6);
                var subTitleRectangle = new CanvasRectangleF((distanceH / 2) - 200, -80 + (float)(fontTitleSize * 0.7) + 6 + 10, 400, (float)(fontTitleSize * 0.75) + 2);
                //grText.DrawRectangles(pen, new RectangleF[] { titleRectangle, subTitleRectangle });
                var stringFormatTitle = Current.Engine.CreateDrawTextFormat();
                stringFormatTitle.Alignment = StringAlignment.Center;
                stringFormatTitle.LineAlignment = StringAlignment.Center;

                canvasText.DrawText(e["profile_name"], fontTitle, brushTitle, titleRectangle.Center, stringFormatTitle);
                canvasText.DrawText("Massstab 1:" + scale + (superelevation > 1 ? "  Überhöhung Faktor: " + superelevation : ""), fontSubTitle, brushTitle, subTitleRectangle.Center, stringFormatTitle);
            }
            #endregion

            canvas.DrawLine(pen, 0, 0, distanceH, 0);
            canvas.DrawLine(pen, 0, 0, 0, distanceV);

            //gr.DrawLine(pen, distanceH/2, -70, distanceH/2, distanceV + 20);    // Mittelstrich

            string filename = "profile_" + Guid.NewGuid().ToString("N").ToLower();

            await bitmap.SaveOrUpload(bridge.OutputPath + @"/" + filename + ".png", ImageFormat.Png);
            byte[] previewData = Drawing.Pro.ImageOperations.Scaledown(System.IO.File.ReadAllBytes(bridge.OutputPath + @"\" + filename + ".png"), 300);
            //File.WriteAllBytes(bridge.OutputPath + @"/preview_" + filename + ".png", previewData);
            await previewData.SaveOrUpload(bridge.OutputPath + @"/preview_" + filename + ".png");

            #region Create Pdf

            var pic2pdf = new Plot.Picture2Pdf();
            pic2pdf.PageWidth = (double)bitmap.Width / dpi * 25.4 + 10D;  // 2x5mm Rand
            pic2pdf.PageHeight = (double)bitmap.Height / dpi * 25.4 + 10D;
            pic2pdf.MarginLeft = pic2pdf.MarginRight = pic2pdf.MarginTop = pic2pdf.MarginBottom = 5;

            using (var imageStream = await (bridge.OutputPath + @"/" + filename + ".png").BytesFromUri(bridge.HttpService))
            {
                var output = pic2pdf.Convert(bridge.HttpService, imageStream).ToArray();

                //System.IO.File.WriteAllBytes(bridge.OutputPath + @"/" + filename + ".pdf", output);
                await output.SaveOrUpload(bridge.OutputPath + @"/" + filename + ".pdf");

                return new ApiPrintEventResponse()
                {
                    Url = bridge.OutputUrl + "/" + filename + ".pdf",
                    Path = bridge.OutputPath + "/" + filename + ".pdf",
                    PreviewUrl = bridge.OutputUrl + "/preview_" + filename + ".png",
                    Length = output.Length
                };
            }

            #endregion
        }

        //return null;

    }

    #endregion

    #region Helper
    private double ClosestStep(double searchValue)
    {
        List<double> stepList = new List<double> { 0.1, 0.5, 1D, 2D, 5D, 10D, 20D, 50D, 100D, 1000D, 5000D };
        return stepList.Aggregate((x, y) => Math.Abs(x - searchValue) < Math.Abs(y - searchValue) ? x : y);
    }

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("profile", Properties.Resources.profile);
    }

    #endregion
}
