using E.Standard.WebMapping.Core.Geometry;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core;

public class ServiceRestirctions
{
    private Polygon _bounds = null;
    private Envelope _boundsEnvelope = null;
    private Point[] _boundsPoints = null;

    public Polygon Bounds
    {
        get { return _bounds; }
        set
        {
            _bounds = value;
            if (this.Bounds != null)
            {
                this._boundsEnvelope = this.Bounds.ShapeEnvelope;
                this._boundsPoints = SpatialAlgorithms.ShapePoints(this.Bounds, false).ToArray();
            }
        }
    }

    public bool EnvelopeInBounds(Envelope envelope)
    {
        if (this.Bounds == null || envelope == null)
        {
            return true;
        }

        if (!envelope.Intersects(_boundsEnvelope))
        {
            return false;
        }


        if (_boundsEnvelope.Contains(envelope.MinX, envelope.MinY) && SpatialAlgorithms.Jordan(this.Bounds, envelope.MinX, envelope.MinY))
        {
            return true;
        }

        if (_boundsEnvelope.Contains(envelope.MinX, envelope.MaxY) && SpatialAlgorithms.Jordan(this.Bounds, envelope.MinX, envelope.MaxY))
        {
            return true;
        }

        if (_boundsEnvelope.Contains(envelope.MaxX, envelope.MaxY) && SpatialAlgorithms.Jordan(this.Bounds, envelope.MaxX, envelope.MaxY))
        {
            return true;
        }

        if (_boundsEnvelope.Contains(envelope.MaxX, envelope.MinY) && SpatialAlgorithms.Jordan(this.Bounds, envelope.MaxX, envelope.MinY))
        {
            return true;
        }

        if (_boundsPoints != null)
        {
            foreach (var boundsPoint in _boundsPoints)
            {
                if (envelope.Contains(boundsPoint))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

public class MapRestrictions : Dictionary<string, ServiceRestirctions>
{

}
