using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements;

public class CrossHairElement : IGraphicElement
{
    private readonly Envelope _fullExtent, _bbox;
    private readonly ArgbColor _color;

    public CrossHairElement(Envelope fullExtent, Envelope bbox, ArgbColor color)
    {
        _fullExtent = fullExtent;
        _bbox = bbox;
        _color = color;
    }

    public void Draw(ICanvas canvas, IMap map)
    {
        try
        {
            using (var pen = Current.Engine.CreatePen(_color, 1))
            {
                var fullMin = map.WorldToImage(new Point(_fullExtent.MinX, _fullExtent.MinY));
                var fullMax = map.WorldToImage(new Point(_fullExtent.MaxX, _fullExtent.MaxY));

                var bboxMin = map.WorldToImage(new Point(_bbox.MinX, _bbox.MinY));
                var bboxMax = map.WorldToImage(new Point(_bbox.MaxX, _bbox.MaxY));

                canvas.DrawRectangle(pen, new CanvasRectangleF(
                        (float)Math.Min(bboxMin.X, bboxMax.X),
                        (float)Math.Min(bboxMin.Y, bboxMax.Y),
                        (float)Math.Abs(bboxMax.X - bboxMin.X),
                        (float)Math.Abs(bboxMax.Y - bboxMin.Y))
                    );

                canvas.DrawLine(pen,
                        (float)(bboxMin.X + bboxMax.X) * .5f, (float)fullMin.Y,
                        (float)(bboxMin.X + bboxMax.X) * .5f, (float)fullMax.Y
                    );

                canvas.DrawLine(pen,
                        (float)fullMin.X, (float)(bboxMin.Y + bboxMax.Y) * .5f,
                        (float)fullMax.X, (float)(bboxMin.Y + bboxMax.Y) * .5f
                    );
            }
        }
        catch { }
    }
}
