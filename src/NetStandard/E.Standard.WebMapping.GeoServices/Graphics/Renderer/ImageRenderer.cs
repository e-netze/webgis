using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.GeoServices.Graphics.Renderer;

public abstract class ImageRenderer
{
    protected IBitmap _bitmap = null;
    protected double minx = 0.0, maxx = 0.0, miny = 0.0, maxy = 0.0, wWidth = 1.0, wHeight = 1.0, dpi = 96;
    protected int iWidth = 1, iHeight = 1;
    private IMap _map = null;

    public ImageRenderer(IMap map)
    {
        if (map != null)
        {
            _map = map;
            iWidth = _map.ImageWidth;
            iHeight = _map.ImageHeight;
            SetImageRect(_map.Extent);
        }
    }

    public IMap Map { get { return _map; } }

    public IBitmap Bitmap
    {
        get { return _bitmap; }
        set
        {
            _bitmap = value;
            iWidth = _bitmap.Width;
            iHeight = _bitmap.Height;
        }
    }
    public abstract void Renderer();

    public void SetImageRect(Envelope env)
    {
        minx = env.MinX;
        miny = env.MinY;
        maxx = env.MaxX;
        maxy = env.MaxY;

        wWidth = Math.Abs(maxx - minx);
        wHeight = Math.Abs(maxy - miny);
    }

    public double Dpi
    {
        get { return dpi; }
        set { dpi = value; }
    }

    public void World2Image(ref double x, ref double y)
    {
        //x = (x - minx) * iWidth / wWidth;
        //y = iHeight - (y - miny) * iHeight / wHeight;
        if (_map != null)
        {
            Core.Geometry.Point wp = new Core.Geometry.Point(x, y);
            wp = _map.WorldToImage(wp);
            x = wp.X;
            y = wp.Y;
        }
    }
}
