using System;

namespace E.Standard.Security.Core.Extensions;

static public class SecurityExtensions
{
    static public string GuidToBase64(this Guid guid)
    {
        return Convert.ToBase64String(guid.ToByteArray()).Replace("/", "-").Replace("+", "_").Replace("=", "");
    }

    static public Guid Base64ToGuid(this string base64)
    {
        Guid guid = default(Guid);
        base64 = base64.Replace("-", "/").Replace("_", "+") + "==";

        try
        {
            guid = new Guid(Convert.FromBase64String(base64));
        }
        catch (Exception ex)
        {
            throw new Exception("Invalid input", ex);
        }

        return guid;
    }
}
