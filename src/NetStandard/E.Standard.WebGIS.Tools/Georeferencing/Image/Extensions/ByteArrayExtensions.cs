using E.Standard.WebGIS.Tools.Georeferencing.Image.Models;
using E.Standard.WebMapping.Core.Api.Bridge;
using gView.GraphicsEngine;
using System.IO;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image.Extensions;

static class ByteArrayExtensions
{
    static public GeorefImageMetadata GetGeorefImageMetadata(this byte[] imageData,
                                                             IBridge bridge,
                                                             string id,
                                                             string name)
    {
        var metadata = new GeorefImageMetadata();

        metadata.Id = id;
        metadata.Name = name;
        metadata.ImageExtension = name.EndsWith(".png") ? "png" : "jpg";

        using (var ms = new MemoryStream(imageData))
        using (var bm = Current.Engine.CreateBitmap(ms))
        {
            metadata.ImageWidth = bm.Width;
            metadata.ImageHeight = bm.Height;
        }

        return metadata;
    }
}
