using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System.IO;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class ImageElement : IGraphicElement
{
    private readonly byte[] _imageBytes;
    private readonly Point _point;
    private readonly Offset _offset;

    public ImageElement(byte[] imageBytes, Point point, Offset offset = null)
    {
        _imageBytes = imageBytes;
        _point = point;
        _offset = offset ?? new Offset(0f, 0f);
    }

    #region IGraphicElement

    public void Draw(ICanvas canvas, IMap map)
    {
        try
        {
            using (var ms = new MemoryStream(_imageBytes))
            using (var bitmap = Current.Engine.CreateBitmap(ms))
            {
                var point = map.WorldToImage(_point);

                canvas.DrawBitmap(bitmap, new CanvasPointF((float)point.X - _offset.Left, (float)point.Y - _offset.Top));
            }
        }
        catch { }
    }

    #endregion
}
