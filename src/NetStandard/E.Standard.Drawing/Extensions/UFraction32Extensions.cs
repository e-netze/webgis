using ExifLibrary;
using System;

namespace E.Standard.Drawing.Extensions;

static internal class UFraction32Extensions
{
    static public double ToGeoCoord(this MathEx.UFraction32[] fractions)
    {
        double unit = 1.0, coord = 0;

        for (var i = 0; i < Math.Max(fractions.Length, 3); i++)
        {
            coord += (double)fractions[i].Numerator / fractions[i].Denominator / unit;
            unit *= 60.0;
        }

        return coord;
    }
}
