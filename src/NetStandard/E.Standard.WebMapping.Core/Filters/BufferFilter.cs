using E.Standard.WebMapping.Core.Abstraction;
using System.Globalization;
using System.Text;

namespace E.Standard.WebMapping.Core.Filters;

public class BufferFilter
{
    private double _bufferDist = 0.0;
    private ILayer _targetLayer = null;
    private SpatialFilter _spatialQuery = null;
    private string _bufferUnits = "meters";

    public double BufferDistance
    {
        get
        {
            return _bufferDist;
        }
        set
        {
            _bufferDist = value;
        }
    }

    public ILayer TargetLayer
    {
        get { return _targetLayer; }
        set { _targetLayer = value; }
    }
    public SpatialFilter SpatialQuery
    {
        get { return _spatialQuery; }
        set { _spatialQuery = value; }
    }
    public string BufferUnits
    {
        get { return _bufferUnits; }
        set { _bufferUnits = value; }
    }

    public string ArcXML(NumberFormatInfo nfi)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<BUFFER distance=\"" + (nfi != null ? _bufferDist.ToString(nfi) : _bufferDist.ToString()) + "\" bufferunits=\"" + _bufferUnits + "\">");
        if (_targetLayer != null)
        {
            sb.Append("<TARGETLAYER id=\"" + _targetLayer.ID + "\" />");
        }

        if (_spatialQuery != null)
        {
            sb.Append(_spatialQuery.ArcXML(nfi));
        }

        sb.Append("</BUFFER>");

        return sb.ToString();
    }

    public BufferFilter Clone()
    {
        BufferFilter clone = new BufferFilter();
        clone._bufferDist = _bufferDist;
        clone._targetLayer = _targetLayer;
        clone._spatialQuery = (_spatialQuery == null) ? null : _spatialQuery.Clone() as SpatialFilter;
        clone._bufferUnits = _bufferUnits;

        return clone;
    }
}
