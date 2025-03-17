using E.Standard.Custom.Core.Models;
using E.Standard.Json;
using System;

namespace E.Standard.Custom.Core.Extensions;

static public class StringExtensions
{
    static public EventMetadata ToEventMetadata(this string str, string username = null)
    {
        try
        {
            if (!String.IsNullOrEmpty(str))
            {
                var result = JSerializer.Deserialize<EventMetadata>(str);
                result.Username = username;
                return result;
            }
        }
        catch { }

        return null;
    }
}
