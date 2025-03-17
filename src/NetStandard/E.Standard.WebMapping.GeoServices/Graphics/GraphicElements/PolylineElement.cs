using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class PolylineElement : IGraphicElement
{
    private Polyline _polyline;
    private ArgbColor _color;
    private float _width = 1, _fontSize;
    private LineDashStyle _dashStyle;

    public PolylineElement(Polyline polyline,
                           ArgbColor color,
                           float width,
                           LineDashStyle dashStyle = LineDashStyle.Solid,
                           float fontSize = 9f)
    {
        _polyline = polyline;
        _color = color;
        _width = width;
        _dashStyle = dashStyle;
        _fontSize = fontSize > 0 ? fontSize : 9f;
    }

    protected Polyline Polyline => _polyline;

    protected void Draw(ICanvas canvas, IMap map, bool labelPointNumber, bool labelSegmentLength, Polyline calcPolyline = null)
    {
        float dpiFactor = map.DpiFactor();
        for (int i = 0; i < _polyline.PathCount; i++)
        {
            Path path = _polyline[i];

            try
            {
                using (var pen = Current.Engine.CreatePen(_color, _width * dpiFactor))
                using (var gp = Current.Engine.CreateGraphicsPath())
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

        if (labelPointNumber || labelSegmentLength)
        {
            int pointNumber = 0;

            calcPolyline = calcPolyline ?? _polyline;

            using (var circlePen = Current.Engine.CreatePen(_color, 2f * dpiFactor))
            {
                for (int i = 0; i < _polyline.PathCount; i++)
                {
                    Path path = _polyline[i];
                    Path calcPath = calcPolyline[i];
                    float o_x = 0f, o_y = 0f;

                    for (int p = 0; p < path.PointCount; p++)
                    {
                        Point point = map.WorldToImage(path[p]);

                        if (labelPointNumber)
                        {
                            canvas.LabelPointNumber((float)point.X, (float)point.Y, ++pointNumber, dpiFactor: dpiFactor);
                        }
                        else
                        {
                            canvas.DrawEllipse(circlePen,
                                           ((float)point.X) - 3f * dpiFactor, ((float)point.Y) - 3f * dpiFactor,
                                           6f * dpiFactor, 6f * dpiFactor);
                        }

                        if (p > 0 && labelSegmentLength)
                        {
                            canvas.LabelSegmentLength(map,
                                                  o_x, o_y, (float)point.X, (float)point.Y,
                                                  Math.Sqrt(Math.Pow(calcPath[p].X - calcPath[p - 1].X, 2) + Math.Pow(calcPath[p].Y - calcPath[p - 1].Y, 2)),
                                                  fontSize: _fontSize,
                                                  dpiFactor: dpiFactor,
                                                  lineAlignment: StringAlignment.Far);
                        }

                        o_x = (float)point.X;
                        o_y = (float)point.Y;
                    }
                }
            }
        }
    }

    #region IGraphicElement Member

    virtual public void Draw(ICanvas canvas, IMap map)
    {
        Draw(canvas, map, false, false);
    }

    #endregion
}
