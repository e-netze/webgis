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
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(MapCrsDependent = true)]
[ToolHelp("tools/general/measure-line.html")]
public class MeasureLine : IApiServerToolLocalizable<MeasureLine>, 
                           IApiButtonResources
{
    private const string Table3dLengthContainerId = "measureline-3d-length-container";

    #region IApiServerTool Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<MeasureLine> localizer)
    {
        var response = new ApiEventResponse();

        if (e.CalcCrs == Epsg.WebMercator)
        {
            response
                .AddUIElements(
                    new UIDiv()
                    {
                        css = UICss.ToClass(new string[] { "webgis-info" }),
                        elements = new IUIElement[]
                            {
                                new UILiteral() { literal = localizer.Localize("warning-webmercator:body") }
                            }
                    }
                );
        }

        response.AddUIElements(
                new UILabel()
                    .WithLabel(localizer.Localize("length-m")),
                new UIInputText()
                    .WithStyles("webgis-sketch-length"),
                new UIButtonContainer(
                     new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removesketch)
                        .WithStyles(UICss.CancelButtonStyle)
                        .WithText(localizer.Localize("remove-sketch"))),
                new UISketchInfoContainer()
            );

        if (Can3D(bridge))
        {
            response
                .AddUIElements(
                    new UIButtonContainer(new UIButton(UIButton.UIButtonType.servertoolcommand, "calc-3d-length")
                        .WithStyles(UICss.CancelButtonStyle)
                        .WithText(localizer.Localize("3d.determine-3d-length"))),
                    new UITable()
                        .WithId(Table3dLengthContainerId)
                        .WithStyles(UICss.EmptyOnChangeSketch, UICss.TableAlternateRowColor));
        }

        return response;
    }

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<MeasureLine> localizer)
    {
        return null;
    }

    #endregion

    #region IApiTool Member

    public ToolType Type => ToolType.sketch1d;

    public ToolCursor Cursor => ToolCursor.Custom_Pen;

    #endregion

    #region IApiButton Member

    public string Name => "Measure Length";

    public string Container => "Tools";

    public string Image => UIImageButton.ToolResourceImage(this, "measure");

    public string ToolTip => "Draw a line to measure the length.";

    public bool HasUI => true;

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("measure", Properties.Resources.measure);
    }

    #endregion

    #region Server Commands

    #region 3D Messen (dynmisch) => auf Eis gelegt

    //[ServerToolCommand("change_sketch_dimension")]
    //public ApiEventResponse OnChangeSketchDimension(IBridge bridge, ApiToolEventArguments e)
    //{
    //    var dim = e["measureline-sketch-dimension"];

    //    return new ApiEventResponse()
    //    {
    //        SketchHasZ = dim?.ToLower() == "3d",
    //        SketchGetZCommand = "sketch_get_z_values"
    //    };
    //}

    //[ServerToolCommand("sketch_get_z_values")]
    //async public Task<ApiEventResponse> OnGetSketchZValue(IBridge bridge, ApiToolEventArguments e)
    //{
    //    try
    //    {
    //        var polyline = e.Sketch as Polyline;
    //        var polylineWgs84 = e.SketchWgs84 as Polyline;

    //        for (var p = 0; p < polyline.PathCount; p++)
    //        {
    //            for (var i = 0; i < polyline[p].PointCount; i++)
    //            {
    //                var point = polyline[p][i];
    //                var pointWgs84 = polylineWgs84[p][i];

    //                if (point.Z == 0D)
    //                {
    //                    var results = await new RasterQueryHelper().PerformHeightQueryAsync(bridge, pointWgs84, $"{ bridge.AppEtcPath }/measure/3d.xml");

    //                    if(results.Count()==0)
    //                    {
    //                        throw new Exception("Höhenabfrage liefert keine Ergebnisse");
    //                    }
    //                    if(results.FirstOrDefault().ResultString.TryToPlatformDouble(out double zValue))
    //                    {
    //                        point.Z = zValue;
    //                    }
    //                    else
    //                    {
    //                        throw new Exception($"Kein gültige Höhenwert: { results.FirstOrDefault().ResultString }");
    //                    }
    //                }
    //            }
    //        }

    //        polyline.HasZ = true;
    //        return new ApiEventResponse()
    //        {
    //            Sketch = polyline,
    //            SketchHasZ = true,
    //            SketchGetZCommand = "sketch_get_z_values"
    //        };
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new Exception($"Höhe kann nicht für alle Punkte ermittelt werden. 3D Messen nicht möglich: { ex.Message }");
    //    }
    //}

    #endregion

    [ServerToolCommand("calc-3d-length")]
    async public Task<ApiEventResponse> OnCalc3dLength(IBridge bridge, ApiToolEventArguments e, ILocalizer<MeasureLine> localizer)
    {
        try
        {
            var polyline = e.Sketch as Polyline;
            var polylineWgs84 = e.SketchWgs84 as Polyline;

            Point pointN = null;     // Punkt(n)
            Point pointN_1 = null;   // Punkt(n-1)   ... für Segment

            for (var p = 0; p < polyline.PathCount; p++)
            {
                for (var i = 0; i < polyline[p].PointCount; i++)
                {
                    var point = polyline[p][i];
                    var pointWgs84 = polylineWgs84[p][i];

                    var results = await new RasterQueryHelper().PerformHeightQueryAsync(bridge, pointWgs84, $"{bridge.AppEtcPath}/measure/3d.xml");

                    if (results.Count() == 0)
                    {
                        throw new Exception(localizer.Localize("3d.exception-no-results"));
                    }

                    var firstResult = results.FirstOrDefault();
                    if (firstResult.IsValid && firstResult.ResultString.TryToPlatformDouble(out double zValue))
                    {
                        point.Z = zValue;

                        pointN_1 = pointN;
                        pointN = point;
                    }
                    else
                    {
                        throw new Exception($"{localizer.Localize("3d.exception-invalid-elevation")}: {results.FirstOrDefault().ResultString}");
                    }
                }
            }

            var uiTable = new UITable()
            {
                id = Table3dLengthContainerId,
                InsertTypeValue = UITable.TableInsertType.Replace
            };

            uiTable.AddRow(new UITableRow(new IUIElement[]
            {
                new UILiteral { literal = localizer.Localize("3d.length") },
                new UILiteral { literal = $"{ Math.Round( polyline.Length3D, 2)} m" }
            }));

            var allPoints = SpatialAlgorithms.ShapePoints(polyline, false);

            uiTable.AddRow(new UITableRow(new IUIElement[]
            {
                new UILiteral { literal = localizer.Localize("3d.elevation-difference") },
                new UILiteral { literal = $"{ Math.Round(allPoints.Max(p=>p.Z) - allPoints.Min(p=>p.Z), 2)} m" }
            }));

            if (pointN != null && pointN_1 != null && !pointN.Equals(pointN_1))
            {
                double hDist = pointN.Distance2D(pointN_1),
                       sDist = pointN.Distance(pointN_1),
                       dH = pointN.Z - pointN_1.Z,
                       dX = pointN.X - pointN_1.X,
                       dY = pointN.Y - pointN_1.Y;

                double tilt = dH / hDist;
                double azimut = Math.Atan2(dX / hDist, dY / hDist);
                azimut = azimut < 0.0
                    ? azimut + Math.PI * 2.0
                    : azimut;

                uiTable.AddRow(new UITableRow(new IUIElement[]
                {
                    new UILiteral { literal = localizer.Localize("3d.segment-horizontal-distance") },
                    new UILiteral { literal = $"{ Math.Round(hDist, 2)} m" }
                }));
                uiTable.AddRow(new UITableRow(new IUIElement[]
                {
                    new UILiteral { literal = localizer.Localize("3d.segment-3d-distance") },
                    new UILiteral { literal = $"{ Math.Round(sDist, 2)} m" }
                }));
                uiTable.AddRow(new UITableRow(new IUIElement[]
                {
                    new UILiteral { literal = localizer.Localize("3d.segment-elevation-difference") },
                    new UILiteral { literal = $"{ Math.Round(dH, 2)} m" }
                }));
                uiTable.AddRow(new UITableRow(new IUIElement[]
                {
                    new UILiteral { literal = localizer.Localize("3d.segment-inclination-angle") },
                    new UILiteral { literal = $"{ Math.Round(Math.Atan(tilt)*180.0/Math.PI, 2) }° = { Math.Round(tilt*100.0) }%" }
                }));
                uiTable.AddRow(new UITableRow(new IUIElement[]
                {
                    new UILiteral { literal = localizer.Localize("3d.segment-azimuth") },
                    new UILiteral { literal = $"{ Math.Round(azimut*180.0/Math.PI, 3) }° = { Math.Round(azimut*200.0/Math.PI, 3) }gon" }
                }));
            }

            return new ApiEventResponse()
                .UIElementsBehavoir(AppendUIElementsMode.Append)
                .AddUIElements(uiTable);
        }
        catch (Exception ex)
        {
            throw new Exception($"{localizer.Localize("3d.exception-no-valid-results")}: {ex.Message}");
        }
    }

    #endregion

    #region Helper

    private bool Can3D(IBridge bridge)
    {
        FileInfo fi = new FileInfo($"{bridge.AppEtcPath}/measure/3d.xml");
        return fi.Exists;
    }

    #endregion
}
