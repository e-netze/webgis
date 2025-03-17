using E.Standard.WebGIS.Tools.Georeferencing.Image.Models;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image.Abstraction;

interface IImportImage
{
    IEnumerable<string> SupportedExtensions { get; }
    IEnumerable<ImportPackage> GetImages(string fileName, byte[] data);
}
