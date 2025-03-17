using E.Standard.Extensions.IO;
using E.Standard.Json;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Abstraction;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Extensions;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Models;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image;

class ImportZip : IImportImage
{
    #region IImageImport

    public IEnumerable<string> SupportedExtensions => new string[] { "zip" };

    public IEnumerable<ImportPackage> GetImages(string fileName, byte[] data)
    {
        List<ImportPackage> packages = new List<ImportPackage>();

        var zipArchive = new ZipArchive(new MemoryStream(data), ZipArchiveMode.Read, true);

        foreach (var zipEntry in zipArchive.Entries)
        {
            if (!zipEntry.Name.Contains("."))
            {
                continue;
            }

            string entryName = zipEntry.Name,
                   fileExtension = entryName.Split('.').Last().ToLower(),
                   fileTitle = entryName.Substring(0, entryName.Length - fileExtension.Length - 1);

            if (new string[] { "png", "jpg", "jpeg" }.Contains(fileExtension) == false)
            {
                continue;
            }

            var package = new ImportPackage()
            {
                Name = entryName,
                ImageExtension = fileExtension
            };
            packages.Add(package);

            using (var entryStream = zipEntry.Open())
            {
                var buffer = entryStream.ReadFully();
                package.ImageData = buffer;
            }

            var worldFileEntry = zipArchive.GetEntry($"{fileTitle}.{fileExtension.WorldFileExtension()}");
            if (worldFileEntry != null)
            {
                using (var entryStream = worldFileEntry.Open())
                {
                    var buffer = entryStream.ReadFully();
                    try
                    {
                        package.WorldFile = new ImageWorldfile(System.Text.Encoding.UTF8.GetString(buffer));
                    }
                    catch { }
                }
            }

            var prjFileEntry = zipArchive.GetEntry($"{fileTitle}.prj");
            if (prjFileEntry != null)
            {
                using (var entryStream = prjFileEntry.Open())
                {
                    var buffer = entryStream.ReadFully();
                    try
                    {
                        package.ProjectionWKT = System.Text.Encoding.Default.GetString(buffer);
                    }
                    catch { }
                }
            }

            var metaFileEntry = zipArchive.GetEntry($"{fileTitle}.meta");
            if (metaFileEntry != null)
            {
                using (var entryStream = metaFileEntry.Open())
                {
                    var buffer = entryStream.ReadFully();
                    try
                    {
                        package.Metadata = JSerializer.Deserialize<GeorefImageMetadata>(System.Text.Encoding.UTF8.GetString(buffer));
                    }
                    catch { }
                }
            }
        }

        return packages;
    }

    #endregion
}
