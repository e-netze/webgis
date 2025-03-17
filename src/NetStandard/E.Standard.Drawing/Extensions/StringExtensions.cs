using E.Standard.Platform;

namespace E.Standard.Drawing.Extensions;

static internal class StringExtensions
{
    static public double FromGMS(this string gms)
    {
        gms = gms.Replace("°", " ").Replace("'", " ").Replace("\"", " ").Replace(",", ".");
        while (gms.Contains("  "))
        {
            gms = gms.Replace("  ", " ");
        }

        string[] p = gms.Split(' ');

        double ret = p[0].ToPlatformDouble();
        if (p.Length > 1)
        {
            ret += p[1].ToPlatformDouble() / 60.0;
        }

        if (p.Length > 2)
        {
            ret += p[2].ToPlatformDouble() / 3600.0;
        }

        return ret;
    }
}
