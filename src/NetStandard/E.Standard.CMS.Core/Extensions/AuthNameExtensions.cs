using System;

namespace E.Standard.CMS.Core.Extensions;

static public class AuthNameExtensions
{
    internal static bool IsEqualAuthName(this string userNameOrRole, string authName, bool useStrictAuthComparing)
    {
        if (userNameOrRole.Equals(authName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (useStrictAuthComparing == false)
        {
            /*
               if in the configuration (CMS) auth-category-prefixes are not defined, check if role 
               fits without prefix   
            */

            if (userNameOrRole.HasAuthNamePrefix()
                && authName.HasNoAuthNamePrefix()
                && userNameOrRole.EndsWith(authName, StringComparison.OrdinalIgnoreCase))
            {
                // userNameOrRole:  nt-role::gr.admins,  authName = Gr.Admins
                // : HasAuthNamePrefix(userNameOrRole) => true
                // : !HasAuthNamePrefix(authName) => true
                // userNameOrRole.EndsWith(authName, StringComparison.OrdinalIgnoreCase) => true
                //
                // so u just have to test the length:  (AuthCategoryPrefixSeperator = "::")
                // userNameOrRole.Length == prefixIndex + AuthCategoryPrefix.Length + authName.Length

                int prefixIndex = userNameOrRole.IndexOf(CmsDocument.AuthCategoryPrefixSeperator, StringComparison.Ordinal);

                if (userNameOrRole.Length == prefixIndex + CmsDocument.AuthCategoryPrefixSeperator.Length + authName.Length)
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool HasAuthNamePrefix(this string authName)
    {
        return authName.Contains(CmsDocument.AuthCategoryPrefixSeperator);
    }

    internal static bool HasNoAuthNamePrefix(this string authName) => HasAuthNamePrefix(authName) == false;

    internal static bool IsEveryone(this string authName)
    {
        return CmsDocument.Everyone.Equals(authName, StringComparison.OrdinalIgnoreCase);
    }
}
