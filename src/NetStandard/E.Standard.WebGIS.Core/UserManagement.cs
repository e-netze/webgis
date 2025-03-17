using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace E.Standard.WebGIS.Core;

public enum UserType
{
    None,
    ApiSubscriber,
    WindowsUser,
    WindowsGroup,
    PVPId,
    PVPUser,
    PVPRole,
    TokenUser,
    TokenRole,
    ClientId,
    CloudPortalLogin
}

public class UserManagement
{
    static public bool AllowWildcards = false;

    static public string UserTypePrefix(UserType userType)
    {
        switch (userType)
        {
            case UserType.ApiSubscriber:
                return "subscriber";
            case UserType.WindowsUser:
                return "nt-user";
            case UserType.WindowsGroup:
                return "nt-group";
            case UserType.PVPId:
                return "pvp-id";
            case UserType.PVPUser:
                return "pvp-user";
            case UserType.PVPRole:
                return "pvp-role";
            case UserType.TokenUser:
                return "token-user";
            case UserType.TokenRole:
                return "token-role";
            case UserType.ClientId:
                return "client";
            case UserType.CloudPortalLogin:
                return "mapportal-login";
        }

        return String.Empty;
    }

    static public string AppendUserPrefix(string username, UserType userType)
    {
        return AppendUserPrefix(username, UserTypePrefix(userType));
    }

    static public string AppendUserPrefix(string username, string prefix)
    {
        if (String.IsNullOrEmpty(prefix) ||
            String.IsNullOrWhiteSpace(username) ||
            username == "*" ||
            username.StartsWith($"{prefix}::"))
        {
            return username;
        }

        return $"{prefix}::{username}";
    }

    static public string[] AppendUserPrefix(string[] names, UserType userType)
    {
        return AppendUserPrefix(names, UserTypePrefix(userType));
    }

    static public string[] AppendUserPrefix(string[] names, string prefix)
    {
        List<string> ret = new List<string>();

        if (names != null)
        {
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == null)
                {
                    continue;
                }

                ret.Add(AppendUserPrefix(names[i], prefix));
            }
        }

        return ret.ToArray();
    }

    static public bool IsAllowed(string userRole, string aclRole)
    {
        return IsAllowed(new string[] { userRole }, new string[] { aclRole });
    }
    static public bool IsAllowed(string userRole, string[] aclRoles)
    {
        return IsAllowed(new string[] { userRole }, aclRoles);
    }
    static public bool IsAllowed(string[] userRoles, string[] aclRoles)
    {
        if (userRoles != null)
        {
            foreach (var userRole in userRoles)
            {
                foreach (var aclRole in aclRoles)
                {
                    if (aclRole == null)
                    {
                        continue;
                    }

                    string prefixWildcard = (userRole.Contains("::") ? userRole.Substring(0, userRole.IndexOf("::") + 2) + "%" : " ").ToLower();

                    if (aclRole == "*" || userRole.ToLower() == aclRole.ToLower() || prefixWildcard == aclRole.ToLower())
                    {
                        return true;
                    }

                    if (AllowWildcards && aclRole.Contains("*"))
                    {
                        if (Regex.IsMatch(userRole.ToLower(), WildCardToRegular(aclRole.ToLower())))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    #region Wildcard Testing

    private static String WildCardToRegular(String value)
    {
        // If you want to implement both "*" and "?"
        //return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";

        // If you want to implement "*" only
        return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
    }

    #endregion
}
