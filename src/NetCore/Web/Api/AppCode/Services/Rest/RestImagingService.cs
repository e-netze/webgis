#pragma warning disable CA1416

using Api.Core.AppCode.Extensions;
using E.Standard.Api.App.Extensions;
using E.Standard.Caching.Abstraction;
using E.Standard.Configuration.Services;
using E.Standard.Extensions.Credentials;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;
using gView.GraphicsEngine;
using System;
using System.IO;

namespace Api.Core.AppCode.Services.Rest;

public class RestImagingService
{
    private readonly ConfigurationService _config;
    private readonly ITempDataByteCache _cache;

    public RestImagingService(ConfigurationService config,
                              ITempDataByteCache cache)
    {
        _config = config;
        _cache = cache;
    }

    public byte[] GetUserMarkerImageBytes(string username, int width, int height)
    {
        if (height <= width)
        {
            return new byte[0];
        }

        string cacheKey = $"usermarker-{username.PureUsername()}-{width}-{height}.png";
        var cachedData = _cache.Get(cacheKey);
        if (cachedData != null)
        {
            return cachedData;
        }

        var penWidth = 1.5f;

        var brushColor = username.ColorFromUsername();
        var penColor = ArgbColor.White;

        using (var bitmap = Current.Engine.CreateBitmap(width, height, PixelFormat.Rgba32))
        using (var canvas = bitmap.CreateCanvas())
        using (var brush = Current.Engine.CreateSolidBrush(brushColor))
        using (var pen = Current.Engine.CreatePen(penColor, penWidth * 2f))
        using (var font = Current.Engine.CreateFont(E.Standard.Platform.SystemInfo.DefaultFontName, width / 3f))
        using (var fontBrush = Current.Engine.CreateSolidBrush(penColor))
        {
            bitmap.MakeTransparent();

            canvas.SmoothingMode = SmoothingMode.AntiAlias;
            canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

            #region Bubble

            double b = width / 2d - penWidth - 1;
            double c = (double)height - penWidth - b;
            double a = Math.Sqrt(c * c - b * b);

            double h_ = a * b / c;
            double a_ = h_ * b / a;

            pen.StartCap = pen.EndCap = LineCap.Round;

            using (var path = Current.Engine.CreateGraphicsPath())
            {

                double step = 0.02d;
                var alpha = Math.Asin(a_ / b);

                CanvasPointF? p0 = null, p_ = null;
                for (double w = -alpha; w <= Math.PI + alpha; w += step)
                {
                    double x = width / 2d - b * Math.Cos(w);
                    double y = width / 2d - b * Math.Sin(w);

                    var p = new CanvasPointF((float)x, (float)y);
                    if (p0 == null)
                    {
                        p0 = p;
                    }

                    if (p_.HasValue)
                    {
                        path.AddLine(p_.Value, p);
                    }
                    p_ = p;
                }

                var pSpot = new CanvasPointF(width / 2f, height - penWidth);

                path.AddLine(pSpot, p0.Value);

                canvas.FillPath(brush, path);
                canvas.DrawPath(pen, path);

                #endregion

                #region Text

                var text = username.ShortPureUsername(2);

                var pText = new CanvasPointF(width / 2f, width / 2f + font.Size / 6f);
                var stringFormat = Current.Engine.CreateDrawTextFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                canvas.DrawText(text, font, fontBrush, pText, stringFormat);

                #endregion
            }

            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);

            var data = ms.ToArray();
            _cache.Set(cacheKey, data);

            return data;
        }
    }

    public byte[] GetQueryMarkerImageBytes(int? id, int? width = null, int? height = null, string[] colorsHex = null, float penWidth = 1.5f, float? fontSize = null)
    {
        string markerText = null;
        if (id.HasValue)
        {
            markerText = id >= 1000 ? $"{id / 1000}K" : id.ToString();
        }

        return GetQueryMarkerImageBytes(markerText, width, height, colorsHex, penWidth, fontSize);
    }

    public byte[] GetQueryMarkerImageBytes(string markerText, int? width = null, int? height = null, string[] colorsHex = null, float penWidth = 1.5f, float? fontSize = null, bool suppressCache = false)
    {
        width = width.HasValue ? width : 33;
        height = height.HasValue ? height : 41;

        if (height <= width)
        {
            return new byte[0];
        }

        colorsHex = colorsHex ?? _config.DefaultMarkerColors();

        string cacheKey = $"textmarker-{markerText}-{width}-{height}-{String.Join("-", colorsHex ?? new string[0])}-{penWidth}-{fontSize}.png";
        var cachedData = _cache.Get(cacheKey);
        if (cachedData != null)
        {
            return cachedData;
        }

        var brushColor = colorsHex != null && colorsHex.Length > 0 ? colorsHex[0].HexToColor() : ArgbColor.FromArgb(255, 33, 126, 208);
        var penColor = colorsHex != null && colorsHex.Length > 1 ? colorsHex[1].HexToColor() : ArgbColor.FromArgb(255, 150, 194, 218);
        var fontColor = colorsHex != null && colorsHex.Length > 2 ? colorsHex[2].HexToColor() : ArgbColor.White;

        using (var bm = Current.Engine.CreateBitmap(width.Value, height.Value, PixelFormat.Rgba32))
        using (var gr = bm.CreateCanvas())
        using (var brush = Current.Engine.CreateSolidBrush(brushColor))
        using (var pen = Current.Engine.CreatePen(penColor, penWidth * 2f))
        using (var font = Current.Engine.CreateFont(E.Standard.Platform.SystemInfo.DefaultFontName, fontSize.HasValue ? fontSize.Value : (float)width / 3f))
        using (var fontBrush = Current.Engine.CreateSolidBrush(fontColor))
        {
            bm.MakeTransparent();

            gr.SmoothingMode = SmoothingMode.AntiAlias;
            gr.TextRenderingHint = TextRenderingHint.AntiAlias;

            #region Bubble

            double b = (double)width / 2d - penWidth - 1;
            double c = (double)height - penWidth - b;
            double a = Math.Sqrt(c * c - b * b);

            double h_ = a * b / c;
            double a_ = h_ * b / a;

            pen.StartCap = pen.EndCap = LineCap.Round;

            using (var path = Current.Engine.CreateGraphicsPath())
            {

                double step = 0.02d;
                var alpha = Math.Asin(a_ / b);

                CanvasPointF? p0 = null, p_ = null;
                for (double w = -alpha; w <= Math.PI + alpha; w += step)
                {
                    double x = (double)width / 2d - b * Math.Cos(w);
                    double y = (double)width / 2d - b * Math.Sin(w);

                    var p = new CanvasPointF((float)x, (float)y);
                    if (p0 == null)
                    {
                        p0 = p;
                    }

                    if (p_.HasValue)
                    {
                        path.AddLine(p_.Value, p);
                    }
                    p_ = p;
                }

                var pSpot = new CanvasPointF((float)width / 2f, (float)height - penWidth);

                path.AddLine(pSpot, p0.Value);

                if (String.IsNullOrEmpty(markerText))
                {
                    // ToDO: !!!!
                    path.AddEllipse(new CanvasRectangleF(
                        width.Value / 2f - width.Value / 5.5f,
                        width.Value / 10f + width.Value / 5.5f,
                        width.Value / 5.5f * 2f,
                        width.Value / 5.5f * 2f)
                     );
                }

                gr.FillPath(brush, path);
                gr.DrawPath(pen, path);

                #endregion

                #region Text

                if (!String.IsNullOrEmpty(markerText))
                {
                    if (markerText.Length > 4)
                    {
                        markerText = $"{markerText.Substring(0, 3)}...";
                    }

                    var pText = new CanvasPointF((float)width / 2f, (float)width / 2f + font.Size / 6f);
                    var stringFormat = Current.Engine.CreateDrawTextFormat();
                    stringFormat.Alignment = StringAlignment.Center;
                    stringFormat.LineAlignment = StringAlignment.Center;

                    gr.DrawText(markerText, font, fontBrush, pText, stringFormat);
                }

                #endregion

            }
            var ms = new MemoryStream();
            bm.Save(ms, ImageFormat.Png);

            var data = ms.ToArray();
            if (!suppressCache)
            {
                _cache.Set(cacheKey, data);
            }
            return data;
        }
    }

    public Offset GetQueryMarkerImageOffset(int? w = null, int? h = null)
    {
        int width = w.HasValue ? w.Value : 33;
        int height = h.HasValue ? h.Value : 41;

        return new Offset(width / 2f, height);
    }

    public byte[] GetCoordsMarkerImageBytes(int? number, int? w = null, int? h = null, string[] colorsHex = null)
    {
        int width = w.HasValue ? w.Value : 26;
        int height = h.HasValue ? h.Value : 37;

        if (height < width)
        {
            return null;
        }

        string cacheKey = $"coordsmarker-{number}-{width}-{height}-{String.Join("-", colorsHex ?? new string[0])}.png";
        var cachedData = _cache.Get(cacheKey);
        if (cachedData != null)
        {
            return cachedData;
        }

        var pieColor = colorsHex != null && colorsHex.Length > 1 ? colorsHex[1].HexToColor() : ArgbColor.Red;
        var brushColor = colorsHex != null && colorsHex.Length > 0 ? colorsHex[0].HexToColor() : ArgbColor.White;
        var fontColor = colorsHex != null && colorsHex.Length > 2 ? colorsHex[2].HexToColor() : ArgbColor.Black;
        var penColor = colorsHex != null && colorsHex.Length > 3 ? colorsHex[3].HexToColor() : pieColor;

        using (var bitmap = Current.Engine.CreateBitmap(width, height, PixelFormat.Rgba32))
        using (var canvas = bitmap.CreateCanvas())
        using (var pen = Current.Engine.CreatePen(penColor, width / 16f))
        using (var font = Current.Engine.CreateFont(E.Standard.Platform.SystemInfo.DefaultFontName, width / 3f))
        using (var brush = Current.Engine.CreateSolidBrush(brushColor))
        using (var fontBrush = Current.Engine.CreateSolidBrush(fontColor))
        using (var pieBrush = Current.Engine.CreateSolidBrush(pieColor))
        {
            bitmap.MakeTransparent();

            canvas.SmoothingMode = SmoothingMode.AntiAlias;
            canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

            float circleDiameter = 2f * width / 3f;

            var circleRect = new CanvasRectangleF(width / 6f, width / 6f, circleDiameter, circleDiameter);

            canvas.TranslateTransform(new CanvasPointF(0, height - width));

            // ToDO: !!!!
            canvas.FillEllipse(brush, circleRect.Left, circleRect.Top, circleRect.Width, circleRect.Height);
            canvas.FillPie(pieBrush, new CanvasRectangle((int)circleRect.Left, (int)circleRect.Top, (int)circleRect.Width, (int)circleRect.Height), 270, 90);
            canvas.FillPie(pieBrush, new CanvasRectangle((int)circleRect.Left, (int)circleRect.Top, (int)circleRect.Width, (int)circleRect.Height), 90, 90);
            ////////////////////////

            canvas.DrawEllipse(pen, width / 6f, width / 6f, circleDiameter, circleDiameter);

            canvas.DrawLine(pen, new CanvasPointF(width / 6f + circleDiameter / 2f, 0f), new CanvasPointF(width / 6f + circleDiameter / 2f, width));
            canvas.DrawLine(pen, new CanvasPointF(0f, width / 2f), new CanvasPointF(2 * width / 6f + circleDiameter, width / 2f));

            canvas.ResetTransform();

            if (number.HasValue)
            {

                var stringFormat = Current.Engine.CreateDrawTextFormat();
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                var text = number.ToString();
                int bkSize = 1;

                var point = new CanvasPointF(width / 2f, font.Size / 2f * 1.8f);

                for (var i = -bkSize; i <= bkSize; i++)
                {
                    for (int j = -bkSize; j <= bkSize; j++)
                    {
                        canvas.DrawText(text, font, brush, new CanvasPointF(point.X - i, point.Y - j), stringFormat);
                    }
                }

                canvas.DrawText(number.ToString(), font, fontBrush, point, stringFormat);
            }

            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);

            var data = ms.ToArray();
            _cache.Set(cacheKey, data);

            return data;
        }
    }

    public Offset GetCoordsMarkerImageOffset(int? w = null, int? h = null)
    {
        int width = w.HasValue ? w.Value : 26;
        int height = h.HasValue ? h.Value : 37;

        return new Offset(width / 2f, (height - width) + width / 2f);
    }

    public byte[] GetChainageMarkerImageBytes(int? number, int? w = null, int? h = null, string[] colorsHex = null)
    {
        if (colorsHex == null)
        {
            colorsHex = new[]
            {
                "#ffffff",
                "#8888ff",
                "#000000",
                "#000000"
            };
        }

        return GetCoordsMarkerImageBytes(number, w, h, colorsHex);
    }

    public Offset GetChainageMarkerImageOffset(int? w = null, int? h = null)
    {
        int width = w.HasValue ? w.Value : 26;
        int height = h.HasValue ? h.Value : 37;

        return new Offset(width / 2f, (height - width) + width / 2f);
    }

    public byte[] GetQueryMarkerSpriteBytes(int? width = null, int? height = null, string[] colorsHex = null, float penWidth = 1.5f, float? fontSize = null)
    {
        width = width.HasValue ? width : 33;
        height = height.HasValue ? height : 41;

        colorsHex = colorsHex ?? _config.DefaultMarkerColors();

        string cacheKey = $"textmarker-{width}-{height}-{String.Join("-", colorsHex ?? new string[0])}-{penWidth}-{fontSize}.png";
        var cachedData = _cache.Get(cacheKey);
        if (cachedData != null)
        {
            return cachedData;
        }

        int counter = 0;
        using (var bitmap = Current.Engine.CreateBitmap(width.Value * 50, height.Value * 20))
        using (var canvas = bitmap.CreateCanvas())
        {
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 50; x++)
                {
                    using (var markerStream = new MemoryStream(GetQueryMarkerImageBytes((++counter).ToString(), width, height, colorsHex, penWidth, fontSize, suppressCache: true)))
                    using (var markerBitmap = Current.Engine.CreateBitmap(markerStream))
                    {
                        canvas.DrawBitmap(markerBitmap, new CanvasPoint(x * width.Value, y * height.Value));
                    }
                }
            }

            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);

            var data = ms.ToArray();
            _cache.Set(cacheKey, data);

            return data;
        }
    }

    #region Helper

    #endregion
}
