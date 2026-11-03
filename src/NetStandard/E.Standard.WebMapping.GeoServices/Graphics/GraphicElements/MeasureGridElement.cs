using System;
using System.Collections.Generic;
using System.Linq;

using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;

using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

// MapMarkup measure tool 'measure_grid' (Messgitter), grid geometry: MeasureGridCalculator (same as L.MeasureGrid.calc)
// labels (cells, border, coords, none) are hidden/thinned out like in the client (L.MeasureGrid._labelState), text color = line color
// more than MeasureGridCalculator.MaxCells cells => only the frame
public class MeasureGridElement : IGraphicElement
{
    private readonly Polyline _polyline, _wgsPolyline;
    private readonly int _calcCrsId;
    private readonly ArgbColor _color;
    private readonly float _width;
    private readonly LineDashStyle _dashStyle;
    private readonly double _cellWidth, _cellHeight;
    private readonly string _labels;
    private readonly bool _align;
    private readonly float _fontSize;

    public MeasureGridElement(Polyline polyline,
                              Polyline wgsPolyline,
                              int calcCrsId,
                              ArgbColor color,
                              float width,
                              LineDashStyle dashStyle,
                              double cellWidth,
                              double cellHeight,
                              string labels = "cells",
                              bool align = false,
                              float fontSize = 14f)
    {
        _polyline = polyline;
        _wgsPolyline = wgsPolyline;
        _calcCrsId = calcCrsId;
        _color = color;
        _width = width;
        _dashStyle = dashStyle;
        _cellWidth = cellWidth;
        _cellHeight = cellHeight > 0 ? cellHeight : cellWidth;
        _labels = !String.IsNullOrEmpty(labels) ? labels.ToLowerInvariant() : "cells";
        _align = align;
        _fontSize = fontSize > 0 ? fontSize : 14f;
    }

    public void Draw(ICanvas canvas, IMap map)
    {
        var lngLats = SpatialAlgorithms.ShapePoints(_wgsPolyline, false);

        using (var grid = MeasureGridCalculator.Calc(lngLats, _calcCrsId, _cellWidth, _cellHeight, _align))
        using (var toMap = new GeometricTransformerPro(CoreApiGlobals.SRefStore, 4326, map.SpatialReference?.Id ?? 4326))
        {
            if (grid is null)
            {
                return;
            }

            float dpiFactor = map.DpiFactor();

            Point ToImage(double c, double r)
            {
                var point = grid.LngLat(c, r);
                toMap.Transform(point);
                return map.WorldToImage(point);
            }

            using (var pen = Current.Engine.CreatePen(_color, _width * dpiFactor))
            {
                pen.DashStyle = _dashStyle;

                void DrawLine(params Point[] points)
                {
                    using (IGraphicsPath gp = Current.Engine.CreateGraphicsPath())
                    {
                        gp.StartFigure();
                        for (int i = 1; i < points.Length; i++)
                        {
                            gp.AddLine((float)points[i - 1].X, (float)points[i - 1].Y, (float)points[i].X, (float)points[i].Y);
                        }
                        canvas.DrawPath(pen, gp);
                    }
                }

                if (grid.TooMany)
                {
                    DrawLine(ToImage(0, 0), ToImage(grid.Cols, 0), ToImage(grid.Cols, grid.Rows), ToImage(0, grid.Rows), ToImage(0, 0));
                    return;
                }

                for (int c = 0; c <= grid.Cols; c++)
                {
                    DrawLine(ToImage(c, 0), ToImage(c, grid.Rows));
                }
                for (int r = 0; r <= grid.Rows; r++)
                {
                    DrawLine(ToImage(0, r), ToImage(grid.Cols, r));
                }
            }

            DrawLabels(canvas, map, grid, ToImage, dpiFactor);
        }
    }

    private void DrawLabels(ICanvas canvas, IMap map, MeasureGridCalculator.Grid grid, Func<double, double, Point> toImage, float dpiFactor)
    {
        string mode = _labels;
        if (mode == "none")
        {
            return;
        }
        if (mode == "coords" && !grid.Aligned)
        {
            mode = "border";
        }

        Point p0 = toImage(0, 0), pu = toImage(1, 0), pv = toImage(0, 1);
        double dux = pu.X - p0.X, duy = pu.Y - p0.Y, dvx = pv.X - p0.X, dvy = pv.Y - p0.Y;
        double lu = Math.Sqrt(dux * dux + duy * duy), lv = Math.Sqrt(dvx * dvx + dvy * dvy);
        if (lu == 0 || lv == 0)
        {
            return;
        }

        // same heuristic as L.MeasureGrid._labelState (image pixels)
        double fontSize = _fontSize * dpiFactor, textHeight = fontSize * 1.2, charWidth = fontSize * 0.6, gap = 4 * dpiFactor;
        double ExtentU(int chars) => Math.Abs(dux) / lu * chars * charWidth + Math.Abs(duy) / lu * textHeight;
        double ExtentV(int chars) => Math.Abs(dvx) / lv * chars * charWidth + Math.Abs(dvy) / lv * textHeight;

        var labels = new List<(Point point, string text)>();

        if (mode == "cells")
        {
            int chars = MeasureGridCalculator.ColumnName(grid.Cols - 1).Length + grid.Rows.ToString().Length;
            if (ExtentU(chars) + gap > lu || ExtentV(chars) + gap > lv)
            {
                return;
            }

            for (int c = 0; c < grid.Cols; c++)
            {
                for (int r = 0; r < grid.Rows; r++)
                {
                    labels.Add((toImage(c + 0.5, r + 0.5), MeasureGridCalculator.CellName(c, r)));
                }
            }
        }
        else
        {
            int colChars, rowChars;
            if (mode == "coords")
            {
                colChars = Math.Max(MeasureGridCalculator.FormatCoordinate(grid.LineX(0)).Length, MeasureGridCalculator.FormatCoordinate(grid.LineX(grid.Cols)).Length);
                rowChars = Math.Max(MeasureGridCalculator.FormatCoordinate(grid.LineY(0)).Length, MeasureGridCalculator.FormatCoordinate(grid.LineY(grid.Rows)).Length);
            }
            else
            {
                colChars = MeasureGridCalculator.ColumnName(grid.Cols - 1).Length;
                rowChars = grid.Rows.ToString().Length;
            }

            int colStep = Math.Max(1, (int)Math.Ceiling((ExtentU(colChars) + gap) / lu)),
                rowStep = Math.Max(1, (int)Math.Ceiling((ExtentV(rowChars) + gap) / lv));
            // outside the frame: column labels before row 0, row labels before column 0 (in cell units)
            double colOffset = (ExtentV(colChars) / 2.0 + gap) / lv,
                   rowOffset = (ExtentU(rowChars) / 2.0 + gap) / lu;

            if (mode == "coords")
            {
                for (int c = 0; c <= grid.Cols; c += colStep)
                {
                    labels.Add((toImage(c, -colOffset), MeasureGridCalculator.FormatCoordinate(grid.LineX(c))));
                }
                for (int r = 0; r <= grid.Rows; r += rowStep)
                {
                    labels.Add((toImage(-rowOffset, r), MeasureGridCalculator.FormatCoordinate(grid.LineY(r))));
                }
            }
            else
            {
                for (int c = 0; c < grid.Cols; c += colStep)
                {
                    labels.Add((toImage(c + 0.5, -colOffset), MeasureGridCalculator.ColumnName(c)));
                }
                for (int r = 0; r < grid.Rows; r += rowStep)
                {
                    labels.Add((toImage(-rowOffset, r + 0.5), (r + 1).ToString()));
                }
            }
        }

        using (var font = Current.Engine.CreateFont(E.Standard.Platform.SystemInfo.DefaultFontName, (float)fontSize))
        using (var brush = Current.Engine.CreateSolidBrush(_color))
        using (var haloBrush = Current.Engine.CreateSolidBrush(ArgbColor.White))
        {
            var format = Current.Engine.CreateDrawTextFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;

            foreach (var label in labels.Where(l => l.point is not null))
            {
                canvas.DrawOutlineLabel(map, label.text, label.point, font, brush, haloBrush, format);
            }
        }
    }

    public Envelope Extent =>
        _polyline is not null
            ? new Envelope(_polyline.ShapeEnvelope)
            : null;
}
