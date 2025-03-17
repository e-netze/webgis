using gView.GraphicsEngine;
using System;
using System.IO;
using System.Text;

namespace E.Standard.Security.Captcha;

public static class Captcha
{
    private const string Letters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static string GenerateCaptchaCode(string username)
    {
        Random rand = new Random();
        int maxRand = Letters.Length - 1;

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < 4; i++)
        {
            int index = rand.Next(maxRand);
            sb.Append(Letters[index]);
        }

        return sb.ToString();
    }

    public static CaptchaResult GenerateCaptchaImage(string captchaCode, int width = 200, int height = 60)
    {
        using (var baseMap = Current.Engine.CreateBitmap(width, height, PixelFormat.Rgba32))
        using (var canvas = baseMap.CreateCanvas())
        {
            canvas.TextRenderingHint = TextRenderingHint.AntiAlias;
            canvas.SmoothingMode = SmoothingMode.AntiAlias;

            Random rand = new Random();

            var bgColor = GetRandomLightColor();
            canvas.Clear(bgColor);

            DrawCaptchaCode();

            AdjustRippleEffect();
            DrawDisorderLine(bgColor);

            MemoryStream ms = new MemoryStream();

            baseMap.Save(ms, ImageFormat.Png);

            return new CaptchaResult
            {
                CaptchaCode = captchaCode,
                CaptchaByteData = ms.ToArray(),
                Timestamp = DateTime.Now
            };

            int GetFontSize(int imageWidth, int captchCodeCount)
            {
                var averageSize = imageWidth / captchCodeCount;

                return Convert.ToInt32(averageSize);
            }

            ArgbColor GetRandomDeepColor()
            {
                int redlow = 160, greenLow = 100, blueLow = 160;

                return ArgbColor.FromArgb(rand.Next(redlow), rand.Next(greenLow), rand.Next(blueLow));
            }

            ArgbColor GetRandomLightColor()
            {
                int low = 180, high = 255;

                int nRend = rand.Next(high) % (high - low) + low;
                int nGreen = rand.Next(high) % (high - low) + low;
                int nBlue = rand.Next(high) % (high - low) + low;

                return ArgbColor.FromArgb(nRend, nGreen, nBlue);
            }

            void DrawCaptchaCode()
            {
                int fontSize = GetFontSize(width, captchaCode.Length);

                using (var fontBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
                using (var font = Current.Engine.CreateFont("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    for (int i = 0; i < captchaCode.Length; i++)
                    {
                        fontBrush.Color = GetRandomDeepColor();

                        int shiftPx = fontSize / 12;

                        float x = i * fontSize + rand.Next(-shiftPx, shiftPx) + rand.Next(-shiftPx, shiftPx);
                        int maxY = height - fontSize;
                        if (maxY < 0)
                        {
                            maxY = 0;
                        }

                        float y = rand.Next(0, maxY);

                        var format = Current.Engine.CreateDrawTextFormat();
                        format.Alignment = StringAlignment.Near;
                        format.LineAlignment = StringAlignment.Near;

                        canvas.DrawText(captchaCode[i].ToString(), font, fontBrush, Math.Max(0, x), y - 5, format);
                    }
                }
            }

            void DrawDisorderLine(ArgbColor color)
            {
                using (var linePen = Current.Engine.CreatePen(ArgbColor.Black, 2))
                {
                    for (int i = 0; i < rand.Next(3, 5); i++)
                    {
                        linePen.Color = GetRandomDeepColor();

                        var startPoint = new CanvasPointF(rand.Next(0, width), rand.Next(0, height));
                        var endPoint = new CanvasPointF(rand.Next(0, width), rand.Next(0, height));
                        //canvas.DrawLine(linePen, startPoint, endPoint);

                        var bezierPoint1 = new CanvasPointF(rand.Next(0, width), rand.Next(0, height));
                        var bezierPoint2 = new CanvasPointF(rand.Next(0, width), rand.Next(0, height));

                        // ToDo:
                        //canvas.DrawBezier(linePen, startPoint, bezierPoint1, bezierPoint2, endPoint);
                        using (var path = Current.Engine.CreateGraphicsPath())
                        {
                            path.AddLine(startPoint, bezierPoint1);
                            path.AddLine(bezierPoint1, bezierPoint2);
                            path.AddLine(bezierPoint2, endPoint);

                            canvas.DrawPath(linePen, path);
                        }
                    }
                }
            }

            void AdjustRippleEffect()
            {
                short nWave = 6;
                int nWidth = baseMap.Width;
                int nHeight = baseMap.Height;

                CanvasPoint[,] pt = new CanvasPoint[nWidth, nHeight];

                double newX, newY;
                double xo, yo;

                for (int x = 0; x < nWidth; ++x)
                {
                    for (int y = 0; y < nHeight; ++y)
                    {
                        xo = (nWave * Math.Sin(2.0 * 3.1415 * y / 128.0));
                        yo = (nWave * Math.Cos(2.0 * 3.1415 * x / 128.0));

                        newX = (x + xo);
                        newY = (y + yo);

                        if (newX > 0 && newX < nWidth)
                        {
                            pt[x, y].X = (int)newX;
                        }
                        else
                        {
                            pt[x, y].X = 0;
                        }


                        if (newY > 0 && newY < nHeight)
                        {
                            pt[x, y].Y = (int)newY;
                        }
                        else
                        {
                            pt[x, y].Y = 0;
                        }
                    }
                }

                using (var bSrc = Current.Engine.CreateBitmap(baseMap.Width, baseMap.Height, PixelFormat.Rgba32))
                using (var bCanvas = bSrc.CreateCanvas())
                {
                    bCanvas.DrawBitmap(baseMap, new CanvasPoint(0, 0));

                    var bitmapData = baseMap.LockBitmapPixelData(BitmapLockMode.ReadOnly, PixelFormat.Rgba32);
                    var bmSrc = bSrc.LockBitmapPixelData(BitmapLockMode.ReadWrite, PixelFormat.Rgba32);

                    try
                    {
                        int scanline = bitmapData.Stride;

                        IntPtr Scan0 = bitmapData.Scan0;
                        IntPtr SrcScan0 = bmSrc.Scan0;

                        unsafe
                        {
                            byte* p = (byte*)(void*)Scan0;
                            byte* pSrc = (byte*)(void*)SrcScan0;

                            int nOffset = bitmapData.Stride - baseMap.Width * 4;

                            int xOffset, yOffset;

                            for (int y = 0; y < nHeight; ++y)
                            {
                                for (int x = 0; x < nWidth; ++x)
                                {
                                    xOffset = pt[x, y].X;
                                    yOffset = pt[x, y].Y;

                                    if (yOffset >= 0 && yOffset < nHeight && xOffset >= 0 && xOffset < nWidth)
                                    {
                                        p[0] = pSrc[(yOffset * scanline) + (xOffset * 4)];
                                        p[1] = pSrc[(yOffset * scanline) + (xOffset * 4) + 1];
                                        p[2] = pSrc[(yOffset * scanline) + (xOffset * 4) + 2];
                                    }

                                    p += 4;
                                }
                                p += nOffset;
                            }
                        }

                    }
                    finally
                    {
                        if (bitmapData != null)
                        {
                            baseMap.UnlockBitmapPixelData(bitmapData);
                        }

                        if (bmSrc != null)
                        {
                            bSrc.UnlockBitmapPixelData(bmSrc);
                        }
                    }
                }
            }
        }
    }
}
