using System;

namespace E.Standard.Extensions.Compare;

static public class FloatingPointCompareExtensions
{
    static public bool EqualDoubleValue(this double x, double y, double epsilon = 1e-7)
    {
        if (Math.Abs(x - y) < epsilon)
        {
            return true;
        }

        return false;
    }

    static public bool EqualFloatValue(this float x, float y, float epsilon = 1e-7f)
    {
        if (Math.Abs(x - y) < epsilon)
        {
            return true;
        }

        return false;
    }
}
