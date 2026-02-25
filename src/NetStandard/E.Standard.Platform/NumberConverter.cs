using Microsoft.Extensions.Primitives;
using System;
using System.Globalization;
using System.Linq;

namespace E.Standard.Platform;

static public class NumberConverter
{
    public static readonly CultureInfo GermanCultureInfo = CultureInfo.CreateSpecificCulture("de-DE");

    static public int? TryParseToInt32(this string value, int? defaultValue = 0)
    {
        if (int.TryParse(value, out int result))
        {
            return result;
        }

        return defaultValue;
    }
    static public double ToPlatformDouble(this string value)
    {
        if (SystemInfo.IsWindows)
        {
            return double.Parse(value.Replace(",", "."), SystemInfo.Nhi);
        }

        return double.Parse(value.Replace(",", SystemInfo.Cnf.NumberDecimalSeparator));
    }

    static public double[] ToPlattformDoubleArray(this string value, char separator = ',')
    {
        if (String.IsNullOrEmpty(value?.Trim()))
        {
            return new double[0];
        }

        return value.Split(separator)
                    .Select(d => d.ToPlatformDouble())
                    .ToArray();
    }

    static public double ToPlatformDouble(this StringValues value)
    {
        return value.ToString().ToPlatformDouble();
    }

    static public float ToPlatformFloat(this string value)
    {
        if (SystemInfo.IsWindows)
        {
            return float.Parse(value.Replace(",", "."), SystemInfo.Nhi);
        }

        return float.Parse(value.Replace(",", SystemInfo.Cnf.NumberDecimalSeparator));
    }

    static public bool TryToPlatformDouble(this string value, out double result)
    {
        if (SystemInfo.IsWindows)
        {
            return double.TryParse(value.Replace(",", "."), NumberStyles.Any, SystemInfo.Nhi, out result);
        }

        return double.TryParse(value.Replace(",", SystemInfo.Cnf.NumberDecimalSeparator), out result);
    }

    static public bool TryToPlatformFloat(this string value, out float result)
    {
        if (SystemInfo.IsWindows)
        {
            return float.TryParse(value.Replace(",", "."), NumberStyles.Any, SystemInfo.Nhi, out result);
        }

        return float.TryParse(value.Replace(",", SystemInfo.Cnf.NumberDecimalSeparator), out result);
    }

    static public string ToPlatformNumberString(this double value)
    {
        return value.ToString(SystemInfo.Nhi);
    }

    static public string ToPlatformNumberString(this float value)
    {
        return value.ToString(SystemInfo.Nhi);
    }

    static public string ToPlatformDistanceString(this double value)
    {
        return $"{value.ToString("N0", CultureInfo.InvariantCulture).Replace(",", ".")}m";
    }
}
