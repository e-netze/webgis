using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class GraphicsPathPro : IDisposable
{
    private IGraphicsPath _path;
    private CanvasPointF[] _points;

    public GraphicsPathPro(IGraphicsPath path, CanvasPointF[] points)
    {
        _path = path;
        _points = points;
    }

    public IGraphicsPath Path => _path;
    public CanvasPointF[] Points => _points;

    public void Dispose()
    {
        if (_path != null)
        {
            _path.Dispose();
            _path = null;
        }

        _points = null;
    }
}
