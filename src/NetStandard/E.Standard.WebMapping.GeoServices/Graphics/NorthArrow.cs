using E.Standard.Platform;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.GeoServices.Graphics;

public class NorthArrow
{
    private double _angle;
    private float _dpi;
    private IDrawTextFormat _textFormat;

    public NorthArrow(double angle)
        : this(angle, 96f)
    {
    }
    public NorthArrow(double angle, float dpi)
    {
        _angle = angle;
        _dpi = dpi;

        _textFormat = Current.Engine.CreateDrawTextFormat();
        _textFormat.Alignment = StringAlignment.Near;
        _textFormat.LineAlignment = StringAlignment.Near;
    }

    public void Create(IBitmap bitmap)
    {
        using (var gr = bitmap.CreateCanvas())
        {
            Create(gr, bitmap.Width, bitmap.Height);
        }
    }

    public void Create(ICanvas canvas, int width, int height)
    {
        canvas.SmoothingMode = SmoothingMode.AntiAlias;
        canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

        float size = (float)Math.Min(width, height) - 1;
        canvas.TranslateTransform(new CanvasPointF(size / 2f, size / 2f));
        canvas.RotateTransform((float)-_angle);

        using (var grayPen = Current.Engine.CreatePen(ArgbColor.Gray, 1))
        using (var whitePen = Current.Engine.CreatePen(ArgbColor.White, 1))
        using (var whiteBrush = Current.Engine.CreateSolidBrush(ArgbColor.White))
        using (var blackBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
        using (var path = Current.Engine.CreateGraphicsPath())
        {
            // Kreis
            canvas.DrawEllipse(grayPen, -size / 2f, -size / 2f, size, size);

            // Pfeil
            path.StartFigure();
            path.AddLine(0f, -size / 2f, size / 2f * 0.574f, size / 2f * 0.819f);
            path.AddLine(0f, size / 3.0f, -size / 2f * 0.574f, size / 2f * 0.819f);
            path.CloseFigure();

            canvas.DrawPath(whitePen, path);
            canvas.FillPath(blackBrush, path);

            using (var font = Current.Engine.CreateFont(SystemInfo.DefaultFontName, 7.0f * _dpi / 96f, FontStyle.Bold))
            {
                var nSize = canvas.MeasureText("N", font);
                canvas.DrawText("N", font, whiteBrush, -nSize.Width / 2f + 0.5f, -nSize.Height / 2f + 3f, _textFormat);
            }
        }
    }
}
