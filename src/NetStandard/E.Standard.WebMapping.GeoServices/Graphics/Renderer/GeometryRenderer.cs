using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.Graphics.Renderer;

internal class DisplayOperations
{
    public static IGraphicsPath Geometry2GraphicsPath(ImageRenderer display, Shape geometry)
    {
        try
        {
            if (geometry is Polygon)
            {
                return ConvertPolygon(display, (Polygon)geometry);
            }
            else if (geometry is Polyline)
            {
                return ConvertPolyline(display, (Polyline)geometry);
            }
            else if (geometry is Envelope)
            {
                return ConvertEnvelope(display, (Envelope)geometry);
            }
        }
        catch
        {
        }
        return null;
    }

    private static IGraphicsPath ConvertPolygon(ImageRenderer display, Polygon polygon)
    {
        var gp = Current.Engine.CreateGraphicsPath();
        double o_x = -1e10, o_y = -1e10;

        for (int r = 0; r < polygon.RingCount; r++)
        {
            bool first = true;
            int count = 0;
            Ring ring = polygon[r];
            int pCount = ring.PointCount;
            gp.StartFigure();
            for (int p = 0; p < pCount; p++)
            {
                Point point = ring[p];
                double x = point.X, y = point.Y;
                display.World2Image(ref x, ref y);

                //
                // Auf 0.1 Pixel runden, sonst kann es bei fast
                // horizontalen (vertikalen) Linien zu Fehlern kommen
                // -> Eine hälfte (beim Bruch) wird nicht mehr gezeichnet
                //
                x = Math.Round(x, 1);
                y = Math.Round(y, 1);

                if (!((float)o_x == (float)x &&
                    (float)o_y == (float)y))
                {
                    if (!first)
                    {
                        gp.AddLine(
                            (float)o_x,
                            (float)o_y,
                            (float)x,
                            (float)y);
                        count++;
                    }
                    else
                    {
                        first = false;
                    }
                }
                o_x = x;
                o_y = y;
            }
            if (count > 0)
            {
                gp.CloseFigure();
            }
        }

        return gp;
    }
    private static IGraphicsPath ConvertEnvelope(ImageRenderer display, Envelope envelope)
    {
        var gp = Current.Engine.CreateGraphicsPath();

        double minx = envelope.MinX, miny = envelope.MinY;
        double maxx = envelope.MaxX, maxy = envelope.MaxY;
        display.World2Image(ref minx, ref miny);
        display.World2Image(ref maxx, ref maxy);

        gp.StartFigure();
        gp.AddLine((float)minx, (float)miny, (float)maxx, (float)miny);
        gp.AddLine((float)maxx, (float)miny, (float)maxx, (float)maxy);
        gp.AddLine((float)maxx, (float)maxy, (float)minx, (float)maxy);
        gp.CloseFigure();

        return gp;
    }
    private static IGraphicsPath ConvertPolyline(ImageRenderer display, Polyline polyline)
    {
        var gp = Current.Engine.CreateGraphicsPath();
        double o_x = -1e10, o_y = -1e10;

        for (int r = 0; r < polyline.PathCount; r++)
        {
            bool first = true;
            int count = 0;
            Path path = polyline[r];
            int pCount = path.PointCount;
            gp.StartFigure();
            for (int p = 0; p < pCount; p++)
            {
                Point point = path[p];
                double x = point.X, y = point.Y;
                display.World2Image(ref x, ref y);

                //
                // Auf 0.1 Pixel runden, sonst kann es bei fast
                // horizontalen (vertikalen) Linien zu Fehlern kommen
                // -> Eine hälfte (beim Bruch) wird nicht mehr gezeichnet
                //
                x = Math.Round(x, 1);
                y = Math.Round(y, 1);

                if (!((float)o_x == (float)x &&
                    (float)o_y == (float)y))
                {
                    if (!first)
                    {
                        gp.AddLine(
                            (float)o_x,
                            (float)o_y,
                            (float)x,
                            (float)y);
                        count++;
                    }
                    else
                    {
                        first = false;
                    }
                }
                o_x = x;
                o_y = y;
            }
            /*
            if(count>0) 
            { 
                gp.CloseFigure();
            }
            */
        }

        return gp;
    }
}

class GeometryRenderer : ImageRenderer
{
    private List<Shape> _geometry = new List<Shape>();
    private IBrushCollection _brush = null;
    private IPen _pen = null;

    public GeometryRenderer(IMap map)
        : base(map)
    {
    }
    public List<Shape> Geometry
    {
        get { return _geometry; }
    }
    public IBrushCollection Brush
    {
        get { return _brush; }
        set { _brush = value; }
    }
    public IPen Pen
    {
        get { return _pen; }
        set { _pen = value; }
    }

    public override void Renderer()
    {
        foreach (Shape geometry in _geometry)
        {
            if (geometry is Polygon)
            {
                RendererPolygon((Polygon)geometry);
            }
        }
    }

    private void RendererPolygon(Polygon polygon)
    {
        using (var path = DisplayOperations.Geometry2GraphicsPath(this, polygon))
        using (var gr = this.Bitmap.CreateCanvas())
        {
            if (_brush != null)
            {
                gr.FillPath(_brush, path);
            }

            if (_pen != null)
            {
                gr.DrawPath(_pen, path);
            }
        }
    }
}
