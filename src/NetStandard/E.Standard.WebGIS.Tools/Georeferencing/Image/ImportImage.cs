using E.Standard.WebGIS.Tools.Georeferencing.Image.Abstraction;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Models;
using gView.GraphicsEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image;

class ImportImage : IImportImage
{
    public IEnumerable<string> SupportedExtensions => new string[] { "png", "jpg", "jpeg" };

    public IEnumerable<ImportPackage> GetImages(string fileName, byte[] data)
    {
        try
        {
            using (var ms = new MemoryStream(data))
            using (var bitmap = Current.Engine.CreateBitmap(ms))
            {
                return new ImportPackage[]
                {
                    new ImportPackage()
                    {
                        Name = fileName,
                        ImageExtension = fileName.Split('.').Last().ToLower(),
                        ImageData = data
                    }
                };
            }
        }
        catch
        {
            return new ImportPackage[0];
        }
    }
}
