using E.Standard.Drawing;
using E.Standard.Drawing.Pro;
using E.Standard.Plot;
using System;
using System.IO;

namespace WebGIS.Tests;

internal class ImageOperationPlayground
{
    public ImageOperationPlayground()
    {
        GraphicsEngines.Init(GraphicsEngines.Engines.Skia);

        ImageMetadata imageMetadata = new ImageMetadata();
        var bytes = ImageOperations.AutoRotate(File.ReadAllBytes(@"C:\temp\IMG_1491.JPG"), ref imageMetadata);

        foreach (ExifLibrary.Orientation orientation in Enum.GetValues(typeof(ExifLibrary.Orientation)))
        {
            var rotatedBytes = ImageOperations.Rotate(bytes, orientation);

            File.WriteAllBytes(@$"C:\temp\rotate_{orientation}.jpg", rotatedBytes);
        }

        foreach (ExifLibrary.Orientation orientation in Enum.GetValues(typeof(ExifLibrary.Orientation)))
        {
            var rotatedBytes = ImageOperations.Rotate(bytes, orientation);

            File.WriteAllBytes(@$"C:\temp\min_rotate_{orientation}.jpg",
                ImageOperations.Scaledown(rotatedBytes, 256));
        }

        var pdfParser = new PdfParser();
        var imagesBytes = pdfParser.GetImages(new MemoryStream(File.ReadAllBytes(@"c:\temp\pdf1.pdf")));

        foreach (var imageBytes in imagesBytes)
        {
            File.WriteAllBytes(@$"c:\temp\pdf_{Guid.NewGuid()}.jpg", imageBytes);
        }
    }
}
