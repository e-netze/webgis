using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class PolygonElement : IGraphicElement
{
    private Polygon _polygon;
    private ArgbColor _color1, _color2;
    private float _width = 1, _fontSize;
    private LineDashStyle _dashStyle;

    public PolygonElement(Polygon polygon, ArgbColor color, float width, LineDashStyle dashStyle = LineDashStyle.Solid)
        : this(polygon, color, color, width, dashStyle)
    {
    }
    public PolygonElement(Polygon polygon,
                          ArgbColor penColor,
                          ArgbColor brushColor,
                          float width,
                          LineDashStyle dashStyle = LineDashStyle.Solid,
                          float fontSize = 9f)
    {
        _polygon = polygon;
        _color1 = penColor;
        _color2 = brushColor;
        _width = width;
        _dashStyle = dashStyle;
        _fontSize = fontSize > 0 ? fontSize : 9f;
    }

    protected Polygon Polygon => _polygon;

    protected void Draw(ICanvas canvas, IMap map, bool labelPointNumber, bool labelSegmentLength, Polygon calcPolygon = null)
    {
        float dpiFactor = map.DpiFactor();
        try
        {
            using (var pen = Current.Engine.CreatePen(_color1, _width))
            using (var brush = Current.Engine.CreateSolidBrush(_color2))
            using (var gp = Current.Engine.CreateGraphicsPath())
            {
                pen.DashStyle = _dashStyle;
                pen.StartCap = pen.EndCap = LineCap.Round;

                for (int i = 0; i < _polygon.RingCount; i++)
                {
                    Ring ring = _polygon[i];
                    ring.Close();

                    gp.StartFigure();
                    float o_x = 0f, o_y = 0f;
                    bool first = true;
                    for (int p = 0; p < ring.PointCount; p++)
                    {
                        Point point = map.WorldToImage(ring[p]);

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
                }

                canvas.FillPath(brush, gp);
                canvas.DrawPath(pen, gp);
            }

            if (labelPointNumber || labelSegmentLength)
            {
                int pointNumber = 0;

                calcPolygon = calcPolygon ?? _polygon;

                for (int i = 0; i < _polygon.RingCount; i++)
                {
                    Path ring = _polygon[i];
                    Path calcRing = calcPolygon[i];

                    ring.ClosePath();
                    calcRing.ClosePath();

                    float o_x = 0f, o_y = 0f;

                    for (int p = 0; p < ring.PointCount; p++)
                    {
                        Point point = map.WorldToImage(ring[p]);

                        if (labelPointNumber && p < ring.PointCount - 1)
                        {
                            canvas.LabelPointNumber((float)point.X, (float)point.Y, ++pointNumber);
                        }

                        if (p > 0 && labelSegmentLength)
                        {
                            canvas.LabelSegmentLength(map,
                                                  o_x, o_y, (float)point.X, (float)point.Y,
                                                  Math.Sqrt(Math.Pow(calcRing[p].X - calcRing[p - 1].X, 2) + Math.Pow(calcRing[p].Y - calcRing[p - 1].Y, 2)),
                                                  fontSize: _fontSize,
                                                  dpiFactor: dpiFactor);
                        }

                        o_x = (float)point.X;
                        o_y = (float)point.Y;
                    }
                }
            }
        }
        catch { }
    }

    #region IGraphicElement Member

    virtual public void Draw(ICanvas canvas, IMap map)
    {
        Draw(canvas, map, false, false);
    }

    #endregion
}
