using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.IO;
using System.Threading.Tasks;

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
        _image = Current.Engine.CreateBitmap(CloneToMemoryStream(stream));
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
        var image = await path.ImageFromUri(httpService);

        return new ImageWrapper(image, path);
    }

    #endregion
}
