using System;
using System.Linq;

using E.Standard.WebMapping.Core;

namespace Api.Core.AppCode.Extensions;

static public class StringExtensions
{
    static public T[] UrlParameterToArray<T>(this string str, char separator = ',')
    {
        if (String.IsNullOrEmpty(str) || "null".Equals(str, StringComparison.OrdinalIgnoreCase))
        {
            return new T[0];
        }

        return str.Split(separator).Select(s =>
                typeof(T) switch
                {
                    Type t when t == typeof(short) => (T)(object)short.Parse(s.Trim()),
                    Type t when t == typeof(int) => (T)(object)int.Parse(s.Trim()),
                    Type t when t == typeof(long) => (T)(object)long.Parse(s.Trim()),

                    Type t when t == typeof(string) => (T)(object)s.Trim(),

                    Type t when t == typeof(float) => (T)(object)float.Parse(s.Trim()),
                    Type t when t == typeof(double) => (T)(object)double.Parse(s.Trim()),

                    Type t when t == typeof(bool) => (T)(object)bool.Parse(s.Trim()),
                    _ => (T)Convert.ChangeType(s.Trim(), typeof(T)),
                }
            ).ToArray();
    }

    static public TimeEpochDefinition UrlParameterToTimeEpoch(this string str)
    {
        if (String.IsNullOrEmpty(str) || "null".Equals(str, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        var parts = str.Split(',');
        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid timeEpoch parameter");
        }
        return new TimeEpochDefinition()
        {
            StartTime = long.Parse(parts[0].Trim()),
            EndTime = long.Parse(parts[1].Trim())
        };
    }

    static public string RemoveSubscriberPrefix(this string userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            return userName;
        }

        // Remove "subscriber::" prefix if it exists
        if (userName.StartsWith("subscriber::", StringComparison.OrdinalIgnoreCase))
        {
            return userName.Substring("subscriber::".Length);
        }

        return userName;
    }
}
