using E.Standard.Esri.Shapefile.IO;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Extensions;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Models;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.IO;
using System.Text;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image;

class ZipExport
{
    private readonly MemoryStream _memoryStream;
    private readonly IStreamProvider _streamProvider;
    private readonly IBridge _bridge;
    private readonly GeorefImageMetadata _georefImageMetadata;

    public ZipExport(IBridge bridge, GeorefImageMetadata georefImageMetadata)
    {
        if (bridge == null || georefImageMetadata == null)
        {
            throw new ArgumentNullException();
        }

        _memoryStream = new MemoryStream();
        _streamProvider = new ZipArchiveStreamProvider(_memoryStream);

        _bridge = bridge;
        _georefImageMetadata = georefImageMetadata;
    }

    public byte[] GetBytes(string title,
                           bool addWorldfile,
                           bool addMetadata,
                           int targetEpsgCode)
    {
        #region Append Image File

        var imageBytes = _bridge.Storage.Load($"{_georefImageMetadata.ImageFileTitle}.{_georefImageMetadata.ImageExtension}", WebMapping.Core.Api.IO.StorageBlobType.Data);
        _streamProvider[$"{title}.{_georefImageMetadata.ImageExtension}"].Write(imageBytes, 0, imageBytes.Length);

        #endregion

        if (addWorldfile == true || addMetadata == true)
        {
            using (var transformer = _bridge.GeometryTransformer(4326, targetEpsgCode))
            {
                _georefImageMetadata.ProjectWorld((IGeometricTransformer2)transformer);
            }

            if (_georefImageMetadata.TopLeft != null &&
                _georefImageMetadata.TopRight != null &&
                _georefImageMetadata.BottomLeft != null)
            {
                if (addWorldfile == true)
                {
                    var origin = _georefImageMetadata.TopLeft;
                    double worldWidth = _georefImageMetadata.WorldWidth(),
                           worldHeight = _georefImageMetadata.WorldHeight();

                    var worldFileString = new StringBuilder();
                    using (var sr = new StringWriter(worldFileString))
                    {
                        sr.WriteLine(((_georefImageMetadata.TopRight.X - _georefImageMetadata.TopLeft.X) / worldWidth * worldWidth / _georefImageMetadata.ImageWidth).ToPlatformNumberString());
                        sr.WriteLine(((_georefImageMetadata.TopRight.Y - _georefImageMetadata.TopLeft.Y) / worldWidth * worldWidth / _georefImageMetadata.ImageWidth).ToPlatformNumberString());
                        sr.WriteLine(((_georefImageMetadata.BottomLeft.X - _georefImageMetadata.TopLeft.X) / worldHeight * worldHeight / _georefImageMetadata.ImageHeight).ToPlatformNumberString());
                        sr.WriteLine(((_georefImageMetadata.BottomLeft.Y - _georefImageMetadata.TopLeft.Y) / worldHeight * worldHeight / _georefImageMetadata.ImageHeight).ToPlatformNumberString());

                        sr.WriteLine(origin.X.ToPlatformNumberString());
                        sr.WriteLine(origin.Y.ToPlatformNumberString());
                    }

                    var worldFileBytes = Encoding.UTF8.GetBytes(worldFileString.ToString());
                    _streamProvider[$"{title}.{_georefImageMetadata.WorldFileExtension()}"].Write(worldFileBytes, 0, worldFileBytes.Length);
                }

                if (addMetadata)
                {
                    var metadataBytes = Encoding.UTF8.GetBytes(JSerializer.Serialize(_georefImageMetadata));
                    _streamProvider[$"{title}.meta"].Write(metadataBytes, 0, metadataBytes.Length);
                }

                #region Prj File

                byte[] prjFileBytes = null;
                try
                {
                    prjFileBytes = Encoding.Default.GetBytes(File.ReadAllText($"{_bridge.AppEtcPath}/prj/{targetEpsgCode}.prj"));
                }
                catch { }

                if (prjFileBytes != null)
                {
                    _streamProvider[$"{title}.prj"].Write(prjFileBytes, 0, prjFileBytes.Length);
                }

                #endregion
            }

        }

        _streamProvider.FlushAndReleaseStreams();

        return _memoryStream.ToArray();
    }
}
