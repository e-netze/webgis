using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.AXL.GraphicElements;

public class NetworkBarrierGraphicElement : IGraphicElement
{
    private Point _point;
    private string _serviceUrl, _layerId;
    private int _nodeId;

    public NetworkBarrierGraphicElement(Point point, string serviceUrl, string layerId, int nodeId)
    {
        _point = point;
        _serviceUrl = serviceUrl;
        _layerId = layerId;
        _nodeId = nodeId;
    }

    public int NodeId
    {
        get { return _nodeId; }
    }

    #region IGraphicElement Member

    public void Draw(ICanvas canvas, IMap map)
    {
        Point p = map.WorldToImage(_point);

        using (var pen = Current.Engine.CreatePen(ArgbColor.Black, 3f))
        {
            canvas.DrawLine(pen, (float)p.X - 10f, (float)p.Y - 10f, (float)p.X + 10f, (float)p.Y + 10f);
            canvas.DrawLine(pen, (float)p.X - 10f, (float)p.Y + 10f, (float)p.X + 10f, (float)p.Y - 10f);
        }
    }

    public Envelope Extent =>
        _point is not null
            ? new Envelope(_point.ShapeEnvelope)
            : null;
    #endregion

    public static NetworkBarrierGraphicElement[] FindBarriers(IMap map, string serviceUrl, string layerId)
    {
        List<NetworkBarrierGraphicElement> ret = new List<NetworkBarrierGraphicElement>();
        if (map != null)
        {
            foreach (NetworkBarrierGraphicElement barrier in map.GraphicsContainer.GetElements(typeof(NetworkBarrierGraphicElement)))
            {
                if (barrier._serviceUrl == serviceUrl && barrier._layerId == layerId)
                {
                    ret.Add(barrier);
                }
            }
        }

        return ret.ToArray();
    }

    public static NetworkBarrierGraphicElement FindBarrier(IMap map, string serviceUrl, string layerId, int nodeId)
    {
        if (map != null)
        {
            foreach (NetworkBarrierGraphicElement barrier in map.GraphicsContainer.GetElements(typeof(NetworkBarrierGraphicElement)))
            {
                if (barrier._serviceUrl == serviceUrl && barrier._layerId == layerId && barrier.NodeId == nodeId)
                {
                    return barrier;
                }
            }
        }

        return null;
    }
}
