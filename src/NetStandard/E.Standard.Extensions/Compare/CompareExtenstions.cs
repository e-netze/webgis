using E.Standard.Extensions.Formatting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Extensions.Compare;

static public class CompareExtenstions
{
    static public string OrTake(this string currentStringValue, string alternativeStringValue)
        => String.IsNullOrEmpty(currentStringValue)
                ? alternativeStringValue
                : currentStringValue;


    static public int OrTake(this int currentIntValue, int alternativeIntValue)
        => currentIntValue > 0
                ? currentIntValue
                : alternativeIntValue;


    static public float OrTake(this float currentIntValue, float alternativeIntValue)
        => currentIntValue > 0f
                ? currentIntValue
                : alternativeIntValue;


    static public double OrTake(this double currentIntValue, double alternativeIntValue)
        => currentIntValue > 0d
                ? currentIntValue
                : alternativeIntValue;


    static public decimal OrTake(this decimal currentIntValue, decimal alternativeIntValue)
        => currentIntValue > 0
            ? currentIntValue
            : alternativeIntValue;


    static public int? OrTake(this int? currentIntValue, int? alternativeIntValue)
    {
        if (currentIntValue.HasValue)
        {
            if (!alternativeIntValue.HasValue)
            {
                return currentIntValue.Value;
            }

            return currentIntValue.Value.OrTake(alternativeIntValue.Value);
        }

        return alternativeIntValue;
    }

    static public T OrTakeEnum<T>(this T currentValue, T alternativeValue)
        where T : System.Enum
    {
        return Convert.ToInt32(currentValue) != 0 ? currentValue : alternativeValue;
    }

    static public T OrTake<T>(this string currentStringValue, T alternativeValue)
    {
        currentStringValue = currentStringValue.ToInvariantNumberString<T>();

        var value = String.IsNullOrWhiteSpace(currentStringValue) ?
            alternativeValue :
            (T)Convert.ChangeType(currentStringValue, typeof(T), System.Globalization.NumberFormatInfo.InvariantInfo);

        if (value == null || value.Equals(default(T)))  // 0, 0.0, null, ...
        {
            value = alternativeValue;
        }

        return value;
    }

    static public IEnumerable<T> OrEmpty<T>(this IEnumerable<T> values)
    {
        if (values == null)
        {
            return Enumerable.Empty<T>();
        }

        return values;
    }
    static public T[] OrTake<T>(this T[] currentValue, T[] alternativeValue)
    {
        if (currentValue == null || currentValue.Count() == 0)
        {
            return alternativeValue;
        }

        return currentValue;
    }

    static public bool EqualContent<T>(this IEnumerable<T> array, IEnumerable<T> canditateArray)
    {
        if (array == null && canditateArray == null)
        {
            return true;
        }

        if (array == null || canditateArray == null)
        {
            return false;
        }

        if (array.Count() != canditateArray.Count())
        {
            return false;
        }

        foreach (T arrayValue in array)
        {
            if (!canditateArray.Contains(arrayValue))
            {
                return false;
            }
        }

        foreach (var canditateValue in canditateArray)
        {
            if (!array.Contains(canditateValue))
            {
                return false;
            }
        }

        return true;
    }

    static public bool EqualContentWithEmptyAndNullIsEqual<T>(this IEnumerable<T> array, IEnumerable<T> canditateArray)
    {
        if (array == null && canditateArray == null)
        {
            return true;
        }

        if (array == null && canditateArray.Count() == 0)
        {
            return true;
        }

        if (canditateArray == null && array.Count() == 0)
        {
            return true;
        }

        return EqualContent(array, canditateArray);
    }

    static public short ValueOrDefault(this short? var)
    {
        return var.HasValue ? var.Value : (short)0;
    }

    static public int ValueOrDefault(this int? var)
    {
        return var.HasValue ? var.Value : 0;
    }

    static public long ValueOrDefault(this long? var)
    {
        return var.HasValue ? var.Value : 0;
    }

    static public float ValueOrDefault(this float? var)
    {
        return var.HasValue ? var.Value : 0;
    }

    static public double ValueOrDefault(this double? var)
    {
        return var.HasValue ? var.Value : 0;
    }
}
