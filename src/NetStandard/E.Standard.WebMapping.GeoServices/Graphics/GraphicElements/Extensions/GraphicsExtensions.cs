using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;

static public class GraphicsExtensions
{
    static public void LabelPointNumber(this ICanvas canvas, float x, float y, int pointNumber, float dpiFactor = 1f)
    {

        try
        {
            using (var font = Current.Engine.CreateFont(Platform.SystemInfo.DefaultFontName, 9 * dpiFactor))
            using (var whiteBrush = Current.Engine.CreateSolidBrush(ArgbColor.White))
            using (var blackBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
            {
                var text = $"{pointNumber}";
                var size = canvas.MeasureText(text, font);

                canvas.TranslateTransform(new CanvasPointF(x, y));

                var drawTextFormat = Current.Engine.CreateDrawTextFormat();
                drawTextFormat.Alignment = StringAlignment.Center;
                drawTextFormat.LineAlignment = StringAlignment.Center;

                canvas.FillRectangle(whiteBrush, new CanvasRectangleF(-size.Width / 2f, -size.Height / 2f, size.Width, size.Height));
                canvas.DrawText(text, font, blackBrush, new CanvasPointF(0f, 0f), drawTextFormat);
            }
        }
        catch { }
        finally
        {
            canvas.ResetTransform();
        }
    }

    static public void LabelSegmentLength(this ICanvas canvas,
                                          IMap map,
                                          float x1, float y1, float x2, float y2, double length,
                                          float fontSize,
                                          float dpiFactor = 1f,
                                          StringAlignment alignment = StringAlignment.Center,
                                          StringAlignment lineAlignment = StringAlignment.Center,
                                          Unit unit = Unit.Meter)
    {
        try
        {
            using (var font = Current.Engine.CreateFont(Platform.SystemInfo.DefaultFontName, fontSize * dpiFactor))
            using (var whiteBrush = Current.Engine.CreateSolidBrush(ArgbColor.White))
            using (var blackBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
            {
                canvas.TranslateTransform(new CanvasPointF((x1 + x2) * 0.5f, (y1 + y2) * 0.5f));

                var angle = (float)(Math.Atan2(y2 - y1, x2 - x1) * 180.0 / Math.PI);
                if (angle < 0)
                {
                    angle += 360f;
                }

                if (angle > 90f && angle < 270f)
                {
                    angle += 180f;
                }

                canvas.RotateTransform(angle);
                //gr.TranslateTransform(0, -font.Height / 2.0f);

                var text = $"{Math.Round(length.MetersToUnit(unit), 2)}{unit.ToAbbreviation()}";
                var drawTextFormat = Current.Engine.CreateDrawTextFormat();
                drawTextFormat.Alignment = alignment;
                drawTextFormat.LineAlignment = lineAlignment;

                canvas.DrawOutlineLabel(map, text, new Point(0, 0), font, blackBrush, whiteBrush, drawTextFormat);
            }
        }
        catch { }
        finally
        {
            canvas.ResetTransform();
        }
    }

    static public void DrawOutlineLabel(this ICanvas canvas, IMap map, string text, Point point, IFont font, IBrush brush, IBrush bkbrush, IDrawTextFormat drawFormat = null)
    {
        if (String.IsNullOrEmpty(text))
        {
            return;
        }

        var oldHint = canvas.TextRenderingHint;
        canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

        //var bkSize = Math.Max(2, (int)Math.Min(2, size / 10.0));
        var bkSize = (int)Math.Round(1f * (float)map.Dpi / 96f);

        var p = new CanvasPointF((float)point.X, (float)point.Y);

        if (drawFormat == null)
        {
            drawFormat = Current.Engine.CreateDrawTextFormat();
            drawFormat.Alignment = StringAlignment.Near;
            drawFormat.LineAlignment = StringAlignment.Center;
        }

        if (bkbrush is IBrush && bkbrush.Color.A > 0)
        {
            for (var i = -bkSize; i <= bkSize; i++)
            {
                for (int j = -bkSize; j <= bkSize; j++)
                {
                    canvas.DrawText(text, font, bkbrush, new CanvasPointF(p.X - i, p.Y - j), drawFormat);
                }
            }
        }

        canvas.DrawText(text, font, brush, p, drawFormat);

        canvas.TextRenderingHint = oldHint;
    }
}
