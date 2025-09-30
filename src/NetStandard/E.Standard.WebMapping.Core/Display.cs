using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.Core;

public class Display : IDisplay
{
    protected Envelope _extent = new Envelope();
    protected int _iWidth = 0, _iHeight = 0;
    protected double _displayRotation = 0.0;
    protected double _dpi, _dpm;
    private double _rCos = 1.0, _rSin = 0.0;

    public Display()
    {
        this.Dpi = 96;
    }

    #region IDisplay Member

    public int ImageWidth
    {
        get { return _iWidth; }
        set
        {
            if (value > 0)
            {
                _iWidth = value;
            }
        }
    }

    public int ImageHeight
    {
        get { return _iHeight; }
        set
        {
            if (value > 0)
            {
                _iHeight = value;
            }
        }
    }

    public double Dpi
    {
        get { return _dpi; }
        set
        {
            _dpi = value;
            _dpm = _dpi / 0.0254;
        }
    }

    virtual public Envelope Extent
    {
        get { return _extent; }
        set
        {
            if (value != null)
            {
                _extent = value;
            }
        }
    }

    public Point WorldToImage(Point worldPoint)
    {
        if (Extent.Width == 0.0 || Extent.Height == 0.0)
        {
            return new Point(0, 0);
        }

        double x = (worldPoint.X - Extent.MinX) * ImageWidth / Extent.Width;
        double y = ImageHeight - (worldPoint.Y - Extent.MinY) * ImageHeight / Extent.Height;

        if (DisplayRotation != 0.0)
        {
            Transform(ref x, ref y);
        }

        return new Point(x, y);
    }

    public Point ImageToWorld(Point imagePoint)
    {
        if (ImageWidth == 0.0 || ImageHeight == 0.0)
        {
            return new Point(0, 0);
        }

        if (DisplayRotation != 0.0)
        {
            double px = imagePoint.X, py = imagePoint.Y;
            InvTransform(ref px, ref py);
            imagePoint = new Point(px, py);
        }

        double x = Extent.Width / ImageWidth * imagePoint.X + Extent.MinX;
        double y = Extent.Height / ImageHeight * (ImageHeight - imagePoint.Y) + Extent.MinY;

        return new Point(x, y);
    }

    public double DisplayRotation
    {
        get
        {
            if (Math.Abs(_displayRotation) < 1e-2)
            {
                return 0.0;
            }

            return _displayRotation;
        }
        set
        {
            _displayRotation = value;
            _rCos = Math.Cos(_displayRotation * Math.PI / 180.0);
            _rSin = Math.Sin(_displayRotation * Math.PI / 180.0);
        }
    }

    public ArgbColor BackgroundColor { get; set; } = ArgbColor.White;

    public long[] TimeEpoch { get; set; } = null;

    #endregion

    static public Envelope ProjectBounds(Envelope bounds, int targetSref)
    {
        using (var transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, bounds.SrsId, targetSref))
        {
            PointCollection pColl = bounds.ToPointCollection(0);
            transformer.Transform(pColl);

            return pColl.ShapeEnvelope;
        }
    }

    static public Envelope TransformedBounds(Display display)
    {

        if (display == null || display.Extent == null)
        {
            return new Envelope();
        }

        if (display.DisplayRotation == 0.0)
        {
            return display.Extent;
        }

        Envelope oBounds = display.Extent;

        Envelope bounds = new Envelope(display.Extent);
        bounds.TranslateTo(0.0, 0.0);

        PointCollection pColl = bounds.ToPointCollection(0);

        for (int i = 0; i < pColl.PointCount; i++)
        {
            Point point = pColl[i];

            double x = point.X * display._rCos + point.Y * display._rSin;
            double y = -point.X * display._rSin + point.Y * display._rCos;
            point.X = x;
            point.Y = y;

        }

        bounds = new Envelope(pColl.ShapeEnvelope);
        bounds.TranslateTo(oBounds.CenterPoint.X, oBounds.CenterPoint.Y);

        return bounds;
    }
    static public Display TransformedDisplay(IMap map)
    {
        if (map == null)
        {
            return null;
        }

        Display display = new Display();
        Envelope transEnv = Display.TransformedBounds(map as Display);
        display = new Display();
        display.Extent.Set(transEnv.MinX, transEnv.MinY, transEnv.MaxX, transEnv.MaxY);
        display.Dpi = map.Dpi;
        display.ImageWidth = (int)(display.Extent.Width * (map.Dpi / 0.0254) / map.MapScale);
        display.ImageHeight = (int)(display.Extent.Height * (map.Dpi / 0.0254) / map.MapScale);

        return display;
    }
    static public IBitmap TransformImage(IBitmap source, IDisplay sourceDisplay, IDisplay targetDisplay)
    {
        if (source == null || sourceDisplay == null || targetDisplay == null)
        {
            return null;
        }

        Envelope targetExtent = targetDisplay.Extent;
        double a = targetDisplay.DisplayRotation * Math.PI / 180.0;
        Point p0 = targetDisplay.WorldToImage(new Point(sourceDisplay.Extent.MinX, sourceDisplay.Extent.MaxY));
        Point p1 = targetDisplay.WorldToImage(new Point(sourceDisplay.Extent.MaxX, sourceDisplay.Extent.MaxY));
        Point p2 = targetDisplay.WorldToImage(new Point(sourceDisplay.Extent.MinX, sourceDisplay.Extent.MinY));

        CanvasPointF[] destPoints = {
            new CanvasPointF((float)p0.X,(float)p0.Y),
            new CanvasPointF((float)p1.X,(float)p1.Y),
            new CanvasPointF((float)p2.X,(float)p2.Y)
        };
        var sourceRect = new CanvasRectangleF(0f, 0f, sourceDisplay.ImageWidth, sourceDisplay.ImageHeight);

        IBitmap target = null;
        try
        {
            target = Current.Engine.CreateBitmap(targetDisplay.ImageWidth, targetDisplay.ImageHeight, PixelFormat.Rgba32);
            using (var canvas = target.CreateCanvas())
            {
                canvas.DrawBitmap(source, destPoints, sourceRect);
            }
            return target;
        }
        catch
        {
            if (target != null)
            {
                target.Dispose();
            }

            return null;
        }
    }

    private void Transform(ref double x, ref double y)
    {
        Transform(ref x, ref y, _iWidth / 2.0, _iHeight / 2.0);
    }
    private void Transform(ref double x, ref double y, double cx, double cy)
    {
        if (DisplayRotation == 0.0)
        {
            return;
        }

        x -= cx;
        y -= cy;

        double x_ = x, y_ = y;
        x = x_ * _rCos + y_ * _rSin;
        y = -x_ * _rSin + y_ * _rCos;

        x += cx;
        y += cy;
    }

    private void InvTransform(ref double x, ref double y)
    {
        InvTransform(ref x, ref y, _iWidth / 2.0, _iHeight / 2.0);
    }
    private void InvTransform(ref double x, ref double y, double cx, double cy)
    {
        if (DisplayRotation == 0.0)
        {
            return;
        }

        x -= cx;
        y -= cy;

        double x_ = x, y_ = y;
        x = x_ * _rCos - y_ * _rSin;
        y = x_ * _rSin + y_ * _rCos;

        x += cx;
        y += cy;
    }

    #region Static Members
    static public void TransformImagePoint(IDisplay display, Point p)
    {
        if (display is Display)
        {
            double x = p.X, y = p.Y;
            ((Display)display).Transform(ref x, ref y);
            p.X = x;
            p.Y = y;
        }
    }

    static public void InvTransformImagePoint(IDisplay display, Point p)
    {
        if (display is Display)
        {
            double x = p.X, y = p.Y;
            ((Display)display).InvTransform(ref x, ref y);
            p.X = x;
            p.Y = y;
        }
    }

    static public void TransformPoint(IDisplay display, Point p, Point center)
    {
        if (display is Display)
        {
            double x = p.X, y = p.Y;
            ((Display)display).Transform(ref x, ref y, center.X, center.Y);
            p.X = x;
            p.Y = y;
        }
    }

    static public void InvTransfromPoint(IDisplay display, Point p, Point center)
    {
        if (display is Display)
        {
            double x = p.X, y = p.Y;
            ((Display)display).InvTransform(ref x, ref y, center.X, center.Y);
            p.X = x;
            p.Y = y;
        }
    }
    #endregion
}
