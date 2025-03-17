using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Geometry;

public class PointMCollection : List<PointM>
{
    public Envelope ShapeEnvelope
    {
        get
        {
            Envelope ret = null;
            foreach (PointM point in this)
            {
                if (point == null)
                {
                    continue;
                }

                if (ret == null)
                {
                    ret = new Envelope(point.ShapeEnvelope);
                }
                else
                {
                    ret.Union(point.ShapeEnvelope);
                }
            }

            return ret;
        }
    }
}
