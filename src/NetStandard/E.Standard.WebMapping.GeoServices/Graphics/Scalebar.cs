using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.GeoServices.Graphics;

public class Scalebar
{
    double _scale, _dpi;
    bool _showText = true;
    private string _fontName = "Verdana";
    private IDrawTextFormat _textFormat;

    public Scalebar(double scale, double dpi, string fontName)
    {
        _scale = scale;
        _dpi = dpi;
        _fontName = fontName;
        _textFormat = Current.Engine.CreateDrawTextFormat();
        _textFormat.Alignment = StringAlignment.Near;
        _textFormat.LineAlignment = StringAlignment.Near;
    }

    public bool ShowText
    {
        get { return _showText; }
        set { _showText = value; }
    }
    public int ScaleBarWidth
    {
        get
        {
            double dpm = _dpi / 0.0254;
            double pix = _scale / dpm;
            double bl = pix * (200 * (_dpi / 96)) / 5.0;

            if (bl > 1000000)
            {
                bl = Math.Round((int)(bl / 100000) * 100000.0, 0);
            }
            else if (bl > 100000)
            {
                bl = Math.Round((int)(bl / 10000) * 10000.0, 0);
            }
            else if (bl > 10000)
            {
                bl = Math.Round((int)(bl / 5000) * 5000.0, 0);
            }
            else if (bl > 1000)
            {
                bl = Math.Round((int)(bl / 500) * 500.0, 0);
            }
            else if (bl > 100)
            {
                bl = Math.Round((int)(bl / 100) * 100.0, 0);
            }
            else if (bl > 10)
            {
                bl = Math.Round((int)(bl / 10) * 10.0, 0);
            }

            int bm_bl = (int)(bl / pix);
            int dist = (int)Math.Round(bl * 5, 0);
            return bm_bl * 5;
        }
    }

    public bool Create(IBitmap bm, int a, int y)
    {
        using (var canvas = bm.CreateCanvas())
        {
            return Create(canvas, a, y);
        }
    }
    public bool Create(ICanvas canvas, int a, int y)
    {
        double dpm = _dpi / 0.0254;
        double pix = _scale / dpm;
        double bl = pix * (200.0 * (_dpi / 96.0)) / 5.0;
        float fac = (float)(_dpi / 96.0);

        if (bl > 1000000)
        {
            bl = Math.Round((int)(bl / 100000) * 100000.0, 0);
        }
        else if (bl > 100000)
        {
            bl = Math.Round((int)(bl / 10000) * 10000.0, 0);
        }
        else if (bl > 10000)
        {
            bl = Math.Round((int)(bl / 5000) * 5000.0, 0);
        }
        else if (bl > 1000)
        {
            bl = Math.Round((int)(bl / 500) * 500.0, 0);
        }
        else if (bl > 100)
        {
            bl = Math.Round((int)(bl / 100) * 100.0, 0);
        }
        else if (bl > 10)
        {
            bl = Math.Round((int)(bl / 10) * 10.0, 0);
        }
        else if (bl > 5)
        {
            bl = Math.Round((int)(bl / 5) * 5.0, 0);
        }
        else if (bl > 1)
        {
            bl = Math.Round(bl, 0);
        }

        if (pix == 0.0 || double.IsNaN(pix))
        {
            return false;
        }

        float bm_bl = (float)(bl / pix);
        using (var font = Current.Engine.CreateFont(_fontName, 7 * fac, FontStyle.Bold))
        using (var brush = Current.Engine.CreateSolidBrush(ArgbColor.FromArgb(155, 149, 149)))
        using (var pen = Current.Engine.CreatePen(ArgbColor.Black, 1))
        {
            int dist = (int)Math.Round(bl * 5, 0);

            // Hintergrund und Rahmen zeichnen
            canvas.FillRectangle(brush, new CanvasRectangleF(a + 0, 15 * fac + y, bm_bl, 5 * fac));
            canvas.FillRectangle(brush, new CanvasRectangleF(a + 2 * bm_bl, 15 * fac + y, bm_bl, 5 * fac));
            canvas.FillRectangle(brush, new CanvasRectangleF(a + 4 * bm_bl, 15 * fac + y, bm_bl, 5 * fac));
            brush.Color = ArgbColor.FromArgb(255, 255, 255);
            canvas.FillRectangle(brush, new CanvasRectangleF(a + 1 * bm_bl, 15 * fac + y, bm_bl, 5 * fac));
            canvas.FillRectangle(brush, new CanvasRectangleF(a + 3 * bm_bl, 15 * fac + y, bm_bl, 5 * fac));

            canvas.DrawRectangle(pen, new CanvasRectangleF(a + 0, 14 * fac + y, bm_bl * 5 - 1, 5 * fac));
            canvas.DrawLine(pen, a, 12 * fac + y, a, 19 * fac + y);
            canvas.DrawLine(pen, a + bm_bl * 5 - 1, 12 * fac + y, a + bm_bl * 5 - 1, 19 * fac + y);
            canvas.DrawLine(pen, a + bm_bl, 14 * fac + y, a + bm_bl, 19 * fac + y);
            canvas.DrawLine(pen, a + bm_bl * 2, 14 * fac + y, a + bm_bl * 2, 19 * fac + y);
            canvas.DrawLine(pen, a + bm_bl * 3, 14 * fac + y, a + bm_bl * 3, 19 * fac + y);
            canvas.DrawLine(pen, a + bm_bl * 4, 14 * fac + y, a + bm_bl * 4, 19 * fac + y);

            string text = Math.Round(_scale, 0).ToString(), t = "";
            int counter = 1;
            // Tausenderpunkte
            for (int i = text.Length - 1; i > 0; i--)
            {
                t = text[i] + t;
                if ((counter++ % 3) == 0)
                {
                    t = "." + t;
                }
            }

            brush.Color = ArgbColor.FromArgb(0, 0, 0);
            canvas.SmoothingMode = SmoothingMode.AntiAlias;
            canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

            if (_showText)
            {
                t = text[0] + t;
                text = "M 1:" + t;
                canvas.DrawText(text, font, brush, (float)(a + (bm_bl * 5 - canvas.MeasureText(text, font).Width) / 2), y, _textFormat);
            }
            canvas.DrawText("0", font, brush, a - 4, (float)y, _textFormat);

            if (dist > 1000)
            {
                float x = dist / (float)1000;
                text = x.ToString() + " km";
            }
            else
            {
                text = dist.ToString() + " m";
            }
            canvas.DrawText(text, font, brush, (float)(a + bm_bl * 5 - 5 * fac), y, _textFormat);
            //gr.Dispose();
            //gr=null;

            font.Dispose();
        }
        return true;
    }

    public IBitmap Create()
    {
        double dpm = _dpi / 0.0254;
        double pix = _scale / dpm;
        double bl = pix * 200 / 5.0;

        if (bl > 1000000)
        {
            bl = Math.Round((int)(bl / 100000) * 100000.0, 0);
        }
        else if (bl > 100000)
        {
            bl = Math.Round((int)(bl / 10000) * 10000.0, 0);
        }
        else if (bl > 10000)
        {
            bl = Math.Round((int)(bl / 5000) * 5000.0, 0);
        }
        else if (bl > 1000)
        {
            bl = Math.Round((int)(bl / 500) * 500.0, 0);
        }
        else if (bl > 100)
        {
            bl = Math.Round((int)(bl / 100) * 100.0, 0);
        }
        else if (bl > 10)
        {
            bl = Math.Round((int)(bl / 10) * 10.0, 0);
        }

        float bm_bl = (float)(bl / pix);

        int a = 4, dist = (int)Math.Round(bl * 5, 0);
        using (var bm = Current.Engine.CreateBitmap((int)(bm_bl * 5 + a + 50), 34))
        using (var gr = bm.CreateCanvas())
        using (var brush = Current.Engine.CreateSolidBrush(ArgbColor.FromArgb(155, 149, 149)))
        using (var pen = Current.Engine.CreatePen(ArgbColor.FromArgb(0, 0, 0), 1))
        using (var font = Current.Engine.CreateFont(_fontName, 7, FontStyle.Bold))
        {

            gr.FillRectangle(brush, new CanvasRectangleF(a + 0, 15, bm_bl, 5));
            gr.FillRectangle(brush, new CanvasRectangleF(a + 2 * bm_bl, 15, bm_bl, 5));
            gr.FillRectangle(brush, new CanvasRectangleF(a + 4 * bm_bl, 15, bm_bl, 5));

            gr.DrawRectangle(pen, new CanvasRectangleF(a + 0, 14, bm_bl * 5 - 1, 5));
            gr.DrawLine(pen, a, 12, a, 19);
            gr.DrawLine(pen, a + bm_bl * 5 - 1, 12, a + bm_bl * 5 - 1, 19);
            gr.DrawLine(pen, a + bm_bl, 14, a + bm_bl, 19);
            gr.DrawLine(pen, a + bm_bl * 2, 14, a + bm_bl * 2, 19);
            gr.DrawLine(pen, a + bm_bl * 3, 14, a + bm_bl * 3, 19);
            gr.DrawLine(pen, a + bm_bl * 4, 14, a + bm_bl * 4, 19);

            string text = Math.Round(_scale, 0).ToString(), t = "";
            int counter = 1;
            // Tausenderpunkte
            for (int i = text.Length - 1; i > 0; i--)
            {
                t = text[i] + t;
                if ((counter++ % 3) == 0)
                {
                    t = "." + t;
                }
            }

            brush.Color = ArgbColor.FromArgb(0, 0, 0);
            gr.SmoothingMode = SmoothingMode.AntiAlias;
            gr.TextRenderingHint = TextRenderingHint.AntiAlias;

            if (_showText)
            {
                t = text[0] + t;
                text = "M 1:" + t;

                gr.DrawText(text, font, brush, (float)(bm_bl * 5 + a - gr.MeasureText(text, font).Width) / 2, 0, _textFormat);
            }
            gr.DrawText("0", font, brush, 0, (float)0, _textFormat);

            if (dist > 1000)
            {
                float x = dist / 1000;
                text = x.ToString() + " km";
            }
            else
            {
                text = dist.ToString() + " m";
            }
            gr.DrawText(text, font, brush, (float)bm_bl * 5 - 1, 0, _textFormat);

            return bm;
        }
    }
}
