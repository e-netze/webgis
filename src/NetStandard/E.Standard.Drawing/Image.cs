using System;
using System.IO;
using System.Threading.Tasks;

using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;

using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.Drawing;

public class ImageWrapper : IDisposable
{
    private IBitmap _image;
    private int _imageWidth, _imageHeight;
    //private byte[] _rawJpgData = null;

    private ImageWrapper(IBitmap image, string path)
    {
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        _image = image;
        _imageWidth = _image.Width;
        _imageHeight = _image.Height;

        //if (ImageFormat.Jpeg.Equals(_image.RawFormat))
        //{
        //    _image.Dispose();
        //    _image = null;

        //    _rawJpgData = File.ReadAllBytes(path);
        //}
    }

    public ImageWrapper(byte[] bytes)
        : this(new MemoryStream(bytes))
    {
    }

    public ImageWrapper(Stream stream)
    {
        // Clone Stream,
        // otherwise there can be GDI+ generic errors on saving the image
        var ms = CloneToMemoryStream(stream);
        EnsureSupportedImageFormat(ms);
        _image = Current.Engine.CreateBitmap(ms);
        _imageWidth = _image.Width;
        _imageHeight = _image.Height;
    }

    public int Width { get { return _imageWidth; } }
    public int Height { get { return _imageHeight; } }

    public bool IsJpeg
    {
        get
        {
            // ToDo
            return true;
        }
    }

    public void SaveAsJpeg(Stream stream, int quality = 90)
    {
        _image.Save(stream, ImageFormat.Jpeg, quality);
    }

    public void SaveAsBmp(Stream stream)
    {
        if (_image != null)
        {
            _image.Save(stream, ImageFormat.Bmp);

            this.Dispose();  // wird sonst nicht aufgerufen
        }
    }

    #region Helper

    // TIFF decoding is not needed for printing and has known ImageSharp vulnerabilities
    private static void EnsureSupportedImageFormat(MemoryStream ms)
    {
        var buffer = new byte[4];
        ms.Position = 0;
        if (ms.Read(buffer, 0, 4) < 4)
        {
            throw new ArgumentException("Invalid image data");
        }

        bool isTiff = (buffer[0] == 0x49 && buffer[1] == 0x49 && (buffer[2] == 0x2A || buffer[2] == 0x2B) && buffer[3] == 0x00)
                   || (buffer[0] == 0x4D && buffer[1] == 0x4D && buffer[2] == 0x00 && (buffer[3] == 0x2A || buffer[3] == 0x2B));

        if (isTiff)
        {
            throw new NotSupportedException("TIFF images are not supported");
        }

        ms.Position = 0;
    }

    private MemoryStream CloneToMemoryStream(Stream stream)
    {
        if (stream is MemoryStream inputMemStream)
        {
            return new MemoryStream(inputMemStream.ToArray());
        }
        else
        {
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms;
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_image != null)
        {
            _image.Dispose();
            _image = null;
        }
    }

    #endregion

    #region Static Members

    async static public Task<ImageWrapper> CreateImageWrapper(IHttpService httpService, string path)
    {
        using var ms = await path.BytesFromUri(httpService);

        return new ImageWrapper(ms);
    }

    #endregion
}
