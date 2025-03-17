using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Geometry.Topology;

public class PointMComparerer<T> : IComparer<PointM>
   where T : IComparable
{
    public int Compare(PointM x, PointM y)
    {
        return ((T)x.M).CompareTo((T)y.M);
    }
}
