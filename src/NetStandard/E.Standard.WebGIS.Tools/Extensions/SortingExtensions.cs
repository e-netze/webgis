using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Extensions;

static public class SortingExtensions
{
    static public IEnumerable<string> OrderNames(this IEnumerable<string> names, string[] order)
    {
        List<string> result;
        if (order == null)
        {
            return names;
        }
        else
        {
            result = new List<string>();

            foreach (string name in order)
            {
                if (names.Contains(name))
                {
                    result.Add(name);
                }
            }

            foreach (var name in names)
            {
                if (!result.Contains(name))
                {
                    result.Add(name);
                }
            }
        }

        return result;
    }

    static public int IndexOf<T>(this T[] array, T value)
    {
        if (array != null)
        {
            for (int i = 0, to = array.Length; i < to; i++)
            {
                if (array[i] == null && value == null)
                {
                    return i;
                }

                if (array[i].Equals(value))
                {
                    return i;
                }
            }
        }

        return -1;
    }
}
