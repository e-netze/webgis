using E.Standard.Plot;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Abstraction;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Models;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image;

class ImportPdf : IImportImage
{
    public IEnumerable<string> SupportedExtensions => new string[] { "pdf" };

    public IEnumerable<ImportPackage> GetImages(string fileName, byte[] data)
    {
        List<ImportPackage> imagePackages = new List<ImportPackage>();

        var pdfParser = new PdfParser();
        var imageDatas = pdfParser.GetImages(new MemoryStream(data));

        string fileTitle = fileName.Contains(".") ?
            fileName.Substring(0, fileName.LastIndexOf(".")) :
            fileName;
        int counter = 0;

        foreach (var imageData in imageDatas)
        {
            try
            {
                using (var stream = new MemoryStream(imageData))
                using (var bitmap = Current.Engine.CreateBitmap(stream))
                {
                    if (bitmap.Width > 256 && bitmap.Height > 256)
                    {
                        imagePackages.Add(new ImportPackage()
                        {
                            Name = $"{fileTitle}-{++counter}",
                            ImageExtension = "jpg",
                            ImageData = imageData
                        });
                    }
                }
            }
            catch(System.Exception)
            {
                //File.WriteAllBytes(@$"c:\temp\error_image_{Guid.NewGuid().ToString()}.jpg", imageData);
            }
        }

        return imagePackages;
    }
}
