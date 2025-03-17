using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Geometry;

internal class RingComparerAreaInv : IComparer<Ring>
{
    #region IComparer<Ring> Member

    public int Compare(Ring x, Ring y)
    {
        if (x == null || y == null)
        {
            return 0;
        }

        double A1 = x.Area;
        double A2 = y.Area;

        if (A1 < A2)
        {
            return 1;
        }

        if (A1 > A2)
        {
            return -1;
        }

        return 0;
    }

    #endregion
}
