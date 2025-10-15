using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class HectoPolylineElement : IGraphicElement
{
    private readonly Polyline _polyline, _calcPolyline;
    private readonly ArgbColor _color;
    private readonly float _width = 1;
    private readonly LineDashStyle _dashStyle;
    private readonly double _interval;
    private readonly string _unit;
    private readonly float _fontSize;

    public HectoPolylineElement(Polyline polyline,
                                ArgbColor color,
                                float width,
                                LineDashStyle dashStyle = LineDashStyle.Solid,
                                double interval = 100.0,
                                string unit = "m",
                                float fontSize = 11f,
                                Polyline calcPolyline = null)
    {
        _polyline = polyline;
        _color = color;
        _width = width;
        _dashStyle = dashStyle;
        _interval = interval;
        _unit = unit.ToLower();
        _fontSize = fontSize > 0 ? fontSize : 11f;
        _calcPolyline = calcPolyline;
    }

    public void Draw(ICanvas canvas, IMap map)
    {
        float dpiFactor = map.DpiFactor();
        for (int i = 0; i < _polyline.PathCount; i++)
        {
            Path path = _polyline[i];

            try
            {
                using (var pen = Current.Engine.CreatePen(_color, _width * dpiFactor))
                using (IGraphicsPath gp = Current.Engine.CreateGraphicsPath())
                {
                    pen.DashStyle = _dashStyle;
                    pen.StartCap = pen.EndCap = LineCap.Round;

                    gp.StartFigure();
                    float o_x = 0f, o_y = 0f;
                    bool first = true;
                    for (int p = 0; p < path.PointCount; p++)
                    {
                        Point point = map.WorldToImage(path[p]);

                        if (!first)
                        {
                            gp.AddLine(o_x, o_y, (float)point.X, (float)point.Y);
                        }
                        else
                        {
                            first = false;
                        }
                        o_x = (float)point.X;
                        o_y = (float)point.Y;
                    }
                    canvas.DrawPath(pen, gp);
                }
            }
            catch { }
        }

        string intervalUnit = _unit;
        double interval = _interval;

        switch (intervalUnit)
        {
            case "km":
                interval *= 1000.0;
                break;
        }

        var polyline = _calcPolyline ?? _polyline;
        if (interval >= 1.0 && interval > polyline.Length / 1000D)
        {
            using (var calcTransformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, _polyline.SrsId, _calcPolyline != null ? _calcPolyline.SrsId : 0))
            {
                using (var symbolPen = Current.Engine.CreatePen(_color, _width * dpiFactor))
                using (var drawFont = Current.Engine.CreateFont(E.Standard.Platform.SystemInfo.DefaultFontName, (float)(_fontSize * dpiFactor)))
                using (var drawBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
                using (var haloBrush = Current.Engine.CreateSolidBrush(ArgbColor.White))
                {
                    bool isEndpoint = false;
                    for (double stat = 0, to = polyline.Length + interval * 2; stat < to; stat += interval)
                    {
                        if (stat > polyline.Length)
                        {
                            stat = polyline.Length;
                            isEndpoint = true;
                        }

                        double direction = 0;
                        var point = SpatialAlgorithms.PolylinePoint(polyline, stat, out direction);
                        direction = Math.Atan2(-Math.Sin(direction), Math.Cos(direction)); // Image Coordinate System
                                                                                           //ouble lineDegrees = (360 / (2 * Math.PI)) * radiantDirection;

                        //lineRadiantDirectionArray.Add(lineDegrees);

                        calcTransformer.InvTransform(point);

                        point = map.WorldToImage(point);
                        double x = point.X, y = point.Y;

                        // symbol
                        float ellipsisdiameter = 6 * dpiFactor;
                        float ellipsisradius = ellipsisdiameter / 2;
                        canvas.DrawEllipse(symbolPen, (float)x - ellipsisradius, (float)y - ellipsisradius, ellipsisdiameter, ellipsisdiameter);

                        // adding text
                        string drawString = "";
                        if (intervalUnit == "km")
                        {
                            drawString = Math.Round((stat / 1000), 3).ToString() + intervalUnit;
                        }
                        else if (intervalUnit == "m")
                        {
                            drawString = Math.Round(stat, 2).ToString() + intervalUnit;
                        }
                        else { }

                        var drawFormat = Current.Engine.CreateDrawTextFormat();
                        drawFormat.Alignment = StringAlignment.Near;
                        drawFormat.LineAlignment = StringAlignment.Far;

                        canvas.DrawOutlineLabel(map, drawString, new Point(x, y), drawFont, drawBrush, haloBrush, drawFormat);

                        if (isEndpoint)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }

    public Envelope Extent =>
        _polyline is not null
            ? new Envelope(_polyline.ShapeEnvelope)
            : null;
}
