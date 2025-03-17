using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Geometry.Topology;


public class PointM3Comparerer<T> : IComparer<PointM3>
   where T : IComparable
{
    public int Compare(PointM3 x, PointM3 y)
    {
        return ((T)x.M3).CompareTo((T)y.M3);
    }
}
