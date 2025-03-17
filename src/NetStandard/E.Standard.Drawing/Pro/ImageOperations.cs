using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.IO;

namespace E.Standard.Drawing.Pro;

public class ImageOperations
{
    #region AutoRotate

    public static byte[] AutoRotate(byte[] imageBytes, ref ImageMetadata metadata)
    {
        try
        {
            metadata = new ImageMetadata();
            metadata.ReadExif(new MemoryStream(imageBytes));

            return Rotate(imageBytes, metadata.Orientation);
        }

        catch { }
        return null;
    }

    public static byte[] Rotate(byte[] imageBytes, ExifLibrary.Orientation orientation)
    {
        if (orientation == ExifLibrary.Orientation.Normal)
        {
            return imageBytes;
        }

        using (var from = Current.Engine.CreateBitmap(new MemoryStream(imageBytes)))
        {
            var (targetWidth, targetHeight) = (from.Width, from.Height);
            Action<ICanvas> transform = canvas => { };

            // with skia: https://github.com/mono/SkiaSharp/issues/836

            switch (orientation)
            {
                case ExifLibrary.Orientation.Rotated180:
                    transform = canvas =>
                    {
                        canvas.RotateTransform(180);
                        canvas.TranslateTransform(new CanvasPointF(-targetWidth, -targetHeight));
                    };
                    break;
                case ExifLibrary.Orientation.RotatedRight:
                    (targetWidth, targetHeight) = (from.Height, from.Width);
                    transform = canvas =>
                    {
                        canvas.ScaleTransform(
                            targetWidth / (float)from.Width,
                            targetHeight / (float)from.Height);
                        canvas.RotateTransform(90);
                        canvas.TranslateTransform(new CanvasPointF(0, -targetHeight));
                    };
                    break;
                case ExifLibrary.Orientation.RotatedLeft:
                    (targetWidth, targetHeight) = (from.Height, from.Width);
                    transform = canvas =>
                    {
                        canvas.ScaleTransform(
                            targetWidth / (float)from.Width,
                            targetHeight / (float)from.Height);
                        canvas.RotateTransform(-90);
                        canvas.TranslateTransform(new CanvasPointF(-targetWidth, 0));
                    };
                    break;
                case ExifLibrary.Orientation.Flipped:
                    transform = canvas =>
                    {
                        canvas.TranslateTransform(new CanvasPointF(targetWidth, 0));
                        canvas.ScaleTransform(-1, 1);

                    };
                    break;
                case ExifLibrary.Orientation.FlippedAndRotated180:
                    transform = canvas =>
                    {
                        // flip
                        canvas.TranslateTransform(new CanvasPointF(targetWidth, 0));
                        canvas.ScaleTransform(-1, 1);
                        // rotate
                        canvas.RotateTransform(180);
                        canvas.TranslateTransform(new CanvasPointF(-targetWidth, -targetHeight));
                    };
                    break;
                case ExifLibrary.Orientation.FlippedAndRotatedRight:
                    (targetWidth, targetHeight) = (from.Height, from.Width);
                    transform = canvas =>
                    {
                        // flip
                        canvas.TranslateTransform(new CanvasPointF(targetWidth, 0));
                        canvas.ScaleTransform(-1, 1);
                        // rotate
                        canvas.ScaleTransform(
                            targetWidth / (float)from.Width,
                            targetHeight / (float)from.Height);
                        canvas.RotateTransform(90);
                        canvas.TranslateTransform(new CanvasPointF(0, -targetHeight));
                    };
                    break;
                case ExifLibrary.Orientation.FlippedAndRotatedLeft:
                    (targetWidth, targetHeight) = (from.Height, from.Width);
                    transform = canvas =>
                    {
                        // flip
                        canvas.TranslateTransform(new CanvasPointF(targetWidth, 0));
                        canvas.ScaleTransform(-1, 1);
                        // rotate
                        canvas.ScaleTransform(
                            targetWidth / (float)from.Width,
                            targetHeight / (float)from.Height);
                        canvas.RotateTransform(-90);
                        canvas.TranslateTransform(new CanvasPointF(-targetWidth, 0));
                    };
                    break;
                default:
                    return imageBytes;
            }

            using (var target = Current.Engine.CreateBitmap(targetWidth, targetHeight))
            using (var targetCanvas = target.CreateCanvas())
            {
                transform.Invoke(targetCanvas);

                targetCanvas.DrawBitmap(from,
                    new CanvasRectangleF(0, 0, targetWidth, targetHeight),
                    new CanvasRectangleF(0, 0, from.Width, from.Height));

                var resultStream = new MemoryStream();
                target.Save(resultStream, ImageFormat.Jpeg);
                return Stream2Bytes(resultStream);
            }
        }
    }

    #endregion

    #region Scaledown 

    public static byte[] Scaledown(MemoryStream ms, int maxDimension)
    {
        return Scaledown(Stream2Bytes(ms), maxDimension);
    }

    public static byte[] Scaledown(byte[] imageBytes, int maxDimension)
    {
        try
        {
            using (var fromBitmap = Current.Engine.CreateBitmap(new MemoryStream(imageBytes)))
            {
                if (fromBitmap.Width <= maxDimension && fromBitmap.Height <= maxDimension)
                {
                    return imageBytes;
                }
                else
                {
                    int w, h;
                    if (fromBitmap.Width > fromBitmap.Height)
                    {
                        w = maxDimension;
                        h = (int)(fromBitmap.Height / (float)fromBitmap.Width * w);
                    }
                    else
                    {
                        h = maxDimension;
                        w = (int)(fromBitmap.Width / (float)fromBitmap.Height * h);
                    }

                    using (var targetBitmap = Current.Engine.CreateBitmap(w, h))
                    using (var targetCanvas = targetBitmap.CreateCanvas())
                    {
                        targetCanvas.InterpolationMode = InterpolationMode.HighQualityBilinear;

                        targetCanvas.DrawBitmap(fromBitmap,
                            new CanvasRectangleF(0f, 0f, w, h),
                            new CanvasRectangleF(0f, 0f, fromBitmap.Width, fromBitmap.Height));

                        MemoryStream ms = new MemoryStream();
                        targetBitmap.Save(ms, ImageFormat.Jpeg);

                        return Stream2Bytes(ms);
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }

    #endregion

    #region Private Members

    internal static byte[] Stream2Bytes(MemoryStream ms)
    {
        byte[] buffer = new byte[ms.Length];
        ms.Position = 0;
        ms.Read(buffer, 0, buffer.Length);

        return buffer;
    }

    #endregion
}
