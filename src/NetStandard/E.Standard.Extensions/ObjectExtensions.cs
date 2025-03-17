using System;

namespace E.Standard.Extensions;

static public class ObjectExtensions
{
    static public string ToStringOrEmpty(this object obj)
        => obj?.ToString() ?? String.Empty;
}
