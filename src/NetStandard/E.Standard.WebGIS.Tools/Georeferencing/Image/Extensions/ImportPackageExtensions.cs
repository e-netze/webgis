using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Models;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using System;
using System.IO;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image.Extensions;

static class ImportPackageExtensions
{
    static public GeorefImageMetadata GetGeorefImageMetadata(this ImportPackage package,
                                                             IBridge bridge,
                                                             string id,
                                                             int mapSrsId = 0)
    {
        var metadata = package.Metadata ?? new GeorefImageMetadata();

        metadata.Id = id;
        metadata.Name = package.Name;
        metadata.ImageExtension = package.ImageExtension;

        if (metadata.TopLeft != null &&
            metadata.TopRight != null &&
            metadata.BottomLeft != null &&
            metadata.ImageWidth > 0 &&
            metadata.ImageHeight > 0)
        {
            return metadata;
        }

        var imageSize = package.ImageSize();
        metadata.ImageWidth = imageSize.width;
        metadata.ImageHeight = imageSize.height;

        var srsId = package.TryDterminePackageSrsId(bridge).OrTake(mapSrsId);
        if (package.WorldFile != null && package.WorldFile.IsValid)
        {
            metadata.TopLeft = new GeoPosition()
            {
                Epsg = mapSrsId,
                X = package.WorldFile.OriginX,
                Y = package.WorldFile.OriginY
            };
            metadata.TopRight = new GeoPosition()
            {
                Epsg = mapSrsId,
                X = package.WorldFile.OriginX + package.WorldFile.Vector1X * imageSize.width,
                Y = package.WorldFile.OriginY + package.WorldFile.Vector1Y * imageSize.width
            };
            metadata.BottomLeft = new GeoPosition()
            {
                Epsg = mapSrsId,
                X = package.WorldFile.OriginX + package.WorldFile.Vector2X * imageSize.height,
                Y = package.WorldFile.OriginY + package.WorldFile.Vector2Y * imageSize.height
            };

            using (var transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, 4326, srsId))
            {
                metadata.ProjectGeographic(transformer);
            }
        }

        return metadata;
    }

    static public int TryDterminePackageSrsId(this ImportPackage package, IBridge bridge)
    {
        if (package?.Metadata?.TopLeft != null && package.Metadata.TopLeft.Epsg > 0)
        {
            return package.Metadata.TopLeft.Epsg;
        }

        if (!String.IsNullOrEmpty(package.ProjectionWKT))
        {
            #region Available PrjFiles 

            if (!String.IsNullOrEmpty(bridge?.AppEtcPath))
            {
                //
                //   Hier wird nur das komplette PRJ File als ganzes verglichen.
                //   Besser wäre einezelnen Parameter zu vergleichen
                //
                var di = new System.IO.DirectoryInfo($"{bridge.AppEtcPath}/prj");
                if (di.Exists)
                {
                    foreach (var fi in di.GetFiles("*.prj"))
                    {
                        try
                        {
                            int epsg = int.Parse(fi.Name.Substring(0, fi.Name.Length - 4));
                            var prjText = File.ReadAllText(fi.FullName);

                            if (prjText.Equals(package.ProjectionWKT, StringComparison.OrdinalIgnoreCase))
                            {
                                return epsg;
                            }
                        }
                        catch { }
                    }
                }

            }

            #endregion
        }

        return 0;
    }

    static public bool IsPureImage(this ImportPackage package)
    {
        bool hasAffinePoints = package.Metadata != null &&
            package.Metadata.TopLeft != null &&
            package.Metadata.TopRight != null &&
            package.Metadata.BottomLeft != null &&
            package.Metadata.ImageWidth > 0 &&
            package.Metadata.ImageHeight > 0;

        bool hasValidWorldFile = package.WorldFile != null && package.WorldFile.IsValid;

        return !hasAffinePoints && !hasValidWorldFile;
    }

    static public (int width, int height) ImageSize(this ImportPackage package)
    {
        using (var ms = new MemoryStream(package.ImageData))
        using (var bitmap = Current.Engine.CreateBitmap(ms))
        {
            return (bitmap.Width, bitmap.Height);
        }
    }
}
