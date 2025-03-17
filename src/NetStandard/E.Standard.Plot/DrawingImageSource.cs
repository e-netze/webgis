using E.Standard.Web.Abstractions;
using MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes;
using System;
using System.IO;

namespace E.Standard.Drawing;

public class DrawingImageSource : ImageSource
{
    private const int MinQuality = 100;
    private readonly IHttpService _http;

    public DrawingImageSource(IHttpService http)
    {
        _http = http;
    }

    protected override IImageSource FromBinaryImpl(string name, Func<byte[]> imageSource, int? quality = 98)
    {
        return new ImageSharpImageSourceImpl(name, () =>
        {
            return new ImageWrapper(imageSource.Invoke());
        }, (int)quality);
    }

    protected override IImageSource FromFileImpl(string path, int? quality = 98)
    {
        return new ImageSharpImageSourceImpl(path, () =>
        {
            return ImageWrapper.CreateImageWrapper(_http, path).Result;
        }, (int)quality);
    }

    protected override IImageSource FromStreamImpl(string name, Func<Stream> imageStream, int? quality = 98)
    {
        return new ImageSharpImageSourceImpl(name, () =>
        {
            using (var stream = imageStream.Invoke())
            {
                return new ImageWrapper(stream);
            }
        }, (int)quality);
    }

    private class ImageSharpImageSourceImpl : IImageSource
    {

        private ImageWrapper _image;
        private ImageWrapper Image
        {
            get
            {
                if (_image == null)
                {
                    _image = _getImage.Invoke();
                }
                return _image;
            }
        }
        private Func<ImageWrapper> _getImage;
        private readonly int _quality;

        public int Width => Image.Width;
        public int Height => Image.Height;
        public string Name { get; }

        public bool IsJpeg => Image.IsJpeg;

        public bool Transparent => false;

        public ImageSharpImageSourceImpl(string name, Func<ImageWrapper> getImage, int quality)
        {
            Name = name;
            _getImage = getImage;
            _quality = Math.Max(MinQuality, quality);
        }

        public void SaveAsJpeg(MemoryStream ms)
        {
            Image.SaveAsJpeg(ms, _quality);
        }

        public void SaveAsBmp(MemoryStream ms)
        {
            Image.SaveAsBmp(ms);
        }

        public void Dispose()
        {
            Image.Dispose();
        }

        public void SaveAsPdfBitmap(MemoryStream ms)
        {
            SaveAsJpeg(ms);
        }
    }
}