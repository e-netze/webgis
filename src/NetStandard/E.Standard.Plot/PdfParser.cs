using E.Standard.Drawing.Pro;
using gView.GraphicsEngine;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Advanced;
using PdfSharpCore.Pdf.Filters;
using PdfSharpCore.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace E.Standard.Plot;

public class PdfParser
{
    public IEnumerable<byte[]> GetImages(Stream stream)
    {
        var doc = PdfReader.Open(stream);

        List<byte[]> images = new List<byte[]>();

        foreach (var page in doc.Pages)
        {
            // Get the resources dictionary.
            CollectImagesFromResources(page, images);

            //CollectImagesFromContents(page, images);
        }

        return images.Where(image => image != null);
    }

    private void CollectImagesFromResources(PdfPage page, List<byte[]> images)
    {
        var resources = page.Elements.GetDictionary("/Resources");
        if (resources == null)
        {
            return;
        }

        // Get the external objects dictionary.
        var xObjects = resources.Elements.GetDictionary("/XObject");
        if (xObjects == null)
        {
            return;
        }

        var items = xObjects.Elements.Values;
        // Iterate the references to external objects.
        foreach (var item in items)
        {
            var reference = item as PdfReference;
            if (reference == null)
            {
                continue;
            }

            var xObject = reference.Value as PdfDictionary;
            // Is external object an image?
            if (xObject != null && xObject.Elements.GetString("/Subtype") == "/Image")
            {
                images.Add(ExportImage(xObject));
            }
        }
    }


    private void CollectImagesFromContents(PdfPage page, List<byte[]> images)
    {
        // dont work...
        // experimental

        var contents = page.Elements.GetObject("/Contents") as PdfArray;
        if (contents is null)
        {
            return;
        }

        for (int e = 0; e < contents.Elements.Count; e++)
        {
            var image = contents.Elements.GetObject(e) as PdfDictionary;
            if (image?.Stream is null) continue;

            //FlateDecode flate = new FlateDecode();
            //var decodedBytes = flate.Decode(image.Stream.Value, new FilterParms(new PdfDictionary()));

            //images.Add(ExportImage(image));
        }
    }

    private byte[] ExportImage(PdfDictionary image)
    {
        try
        {
            var filter = image.Elements.GetValue("/Filter");
            // Do we have a filter array?
            var array = filter as PdfArray;
            if (array != null)
            {
                // PDF files sometimes contain "zipped" JPEG images.
                if (array.Elements.Count >= 2 &&
                    array.Elements.GetName(0) == "/FlateDecode" &&
                    array.Elements.GetName(1) == "/DCTDecode")
                {
                    return ExportJpegImage(image, true);
                }

                if (array.Elements.GetName(0) == "/DCTDecode")
                {
                    return ExportJpegImage(image, false);
                }

                if (array.Elements.GetName(0) == "/FlateDecode")
                {
                    return ExportAsPngImage(image);
                }
                // TODO Deal with other encodings like "/FlateDecode" + "/CCITTFaxDecode"
            }

            // Do we have a single filter?
            var name = filter as PdfName;
            if (name != null)
            {
                var decoder = name.Value;
                switch (decoder)
                {
                    case "/DCTDecode":
                        return ExportJpegImage(image, false);

                    case "/FlateDecode":
                        return ExportAsPngImage(image);

                        // TODO Deal with other encodings like "/CCITTFaxDecode"
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return null;
    }

    private static byte[] ExportJpegImage(PdfDictionary image, bool flateDecode)
    {
        // Fortunately JPEG has native support in PDF and exporting an image is just writing the stream to a file.
        var stream = flateDecode ? Filtering.Decode(image.Stream.Value, "/FlateDecode") : image.Stream.Value;
        return stream;
    }

    /// <summary>
    /// Exports image in PNG format.
    /// </summary>
    private static byte[] ExportAsPngImage__(PdfDictionary image)
    {
        var width = image.Elements.GetInteger(PdfImage.Keys.Width);
        var height = image.Elements.GetInteger(PdfImage.Keys.Height);
        var bitsPerComponent = image.Elements.GetInteger(PdfImage.Keys.BitsPerComponent);

        //var stream = image.Stream.Value;

        //return stream;

        // TODO: You can put the code here that converts from PDF internal image format to a Windows bitmap.
        // and use GDI+ to save it in PNG format.
        // It is the work of a day or two for the most important formats. Take a look at the file
        // PdfSharp.Pdf.Advanced/PdfImage.cs to see how we create the PDF image formats.
        // We don't need that feature at the moment and therefore will not implement it.
        // If you write the code for exporting images I would be pleased to publish it in a future release
        // of PDFsharp.

        return null;
    }

    private static byte[] ExportAsPngImage(PdfDictionary image)
    {
        int width = image.Elements.GetInteger(PdfImage.Keys.Width);
        int height = image.Elements.GetInteger(PdfImage.Keys.Height);

        if(width == 0 || height == 0)
        {
            return null;
        }

        var canUnfilter = image.Stream.TryUnfilter();
        byte[] decodedBytes;

        if (canUnfilter)
        {
            decodedBytes = image.Stream.Value;
        }
        else
        {
            FlateDecode flate = new FlateDecode();
            decodedBytes = flate.Decode(image.Stream.Value, new FilterParms(new PdfDictionary()));

            //decodedBytes = Filtering.Decode(image.Stream.Value, "/FlateDecode");
        }

        int bitsPerComponent = 0;
        while (decodedBytes.Length - ((width * height) * bitsPerComponent / 8) > 0)
        {
            bitsPerComponent++;
        }

        if(decodedBytes.Length - ((width * height) * bitsPerComponent / 8) != 0)  // must be 0!!!
        {
            return null;
        }

        PixelFormat pixelFormat;
        switch (bitsPerComponent)
        {
            case 1:
                pixelFormat = PixelFormat.Gray8;
                break;
            case 8:
                pixelFormat = PixelFormat.Gray8;
                break;
            //case 16:
            //    pixelFormat = PixelFormat.Rgb24;
            //    break;
            case 24:
                pixelFormat = PixelFormat.Rgb24;
                break;
            case 32:
                pixelFormat = PixelFormat.Rgba32;
                break;
            //case 64:
            //    pixelFormat = PixelFormat.Rgba32;
            //    break;
            default:
                throw new Exception("Unknown pixel format " + bitsPerComponent);
        }

        decodedBytes = decodedBytes.Reverse().ToArray();
        var orientation = ExifLibrary.Orientation.Normal;

        using (var bmp = Current.Engine.CreateBitmap(width, height, pixelFormat))
        using (MemoryStream ms = new MemoryStream())
        {
            int length = (int)Math.Ceiling(width * bitsPerComponent / 8.0);

            if (bitsPerComponent == 1)
            {
                BitmapPixelData bmpData = null;
                try
                {
                    bmpData = bmp.LockBitmapPixelData(BitmapLockMode.WriteOnly, bmp.PixelFormat);
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x += 8)
                        {
                            int scanOffset = y * bmpData.Stride;
                            var data = decodedBytes[y * length + x / 8];

                            for (int i = 0; i < 8; i++)
                            {
                                int bit = (data >> i) & 1;

                                Marshal.Copy(bit == 1
                                        ? new byte[] { 255 }
                                        : new byte[] { 0 },
                                        0,
                                        new IntPtr(bmpData.Scan0.ToInt64() + scanOffset + x + i),
                                        1
                                    );
                            }
                        }
                    }
                }
                finally
                {
                    bmp.UnlockBitmapPixelData(bmpData);
                }

                orientation = ExifLibrary.Orientation.Rotated180;
            }
            else
            {
                BitmapPixelData bmpData = null;
                try
                {
                    bmpData = bmp.LockBitmapPixelData(BitmapLockMode.WriteOnly, bmp.PixelFormat);
                    for (int i = 0; i < height; i++)
                    {
                        int offset = i * length;
                        int scanOffset = i * bmpData.Stride;
                        Marshal.Copy(
                                decodedBytes,
                                offset,
                                new IntPtr(bmpData.Scan0.ToInt64() + scanOffset),
                                length
                            );
                    }
                }
                finally
                {
                    bmp.UnlockBitmapPixelData(bmpData);
                }

                orientation = ExifLibrary.Orientation.Rotated180;
            }

            // ToDo:
            //bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
            bmp.Save(ms, ImageFormat.Png);
            var buffer = ms.GetBuffer();

            return ImageOperations.Rotate(buffer, orientation);
            //return buffer;
        }
    }
}
