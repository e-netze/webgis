using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Geometry.Topology;

public class PointM2Comparerer<T> : IComparer<PointM2>
   where T : IComparable
{
    public int Compare(PointM2 x, PointM2 y)
    {
        return ((T)x.M2).CompareTo((T)y.M2);
    }
}
