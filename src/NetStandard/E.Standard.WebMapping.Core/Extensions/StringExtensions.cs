using System;

namespace E.Standard.WebMapping.Core.Extensions;

static public class StringExtensions
{
    static public bool IsImageContentType(this string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

}
