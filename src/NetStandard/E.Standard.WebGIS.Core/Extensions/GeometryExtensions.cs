using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebGIS.Core.Extensions;

static public class GeometryExtensions
{
    static public Point ShapeToPoint(this Shape shape, Point hotSpotPoint = null, SpatialReference sRef = null)
    {
        if (shape != null)
        {
            var shapeEnvelope = shape.ShapeEnvelope;
            hotSpotPoint = hotSpotPoint != null ? hotSpotPoint : shapeEnvelope.CenterPoint;

            Point[] points = null;
            try
            {
                if (shape is Point)
                {
                    points = new Point[] { (Point)shape };
                }
                else
                {
                    points = SpatialAlgorithms.DeterminePointsOnShape(null, shape, 10, sRef != null ? !sRef.IsProjective : true, hotSpotPoint);
                }
            }
            catch { /* TODO: Warnung ausgeben?! */ }

            if (points != null && points.Length > 0)
            {
                return SpatialAlgorithms.ClosestPointToHotspot(points, /*shape.ShapeEnvelope.CenterPoint*/hotSpotPoint);
            }
        }

        return null;
    }
}
