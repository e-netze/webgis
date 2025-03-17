using E.Standard.CMS.Core.Security;
using E.Standard.Extensions.Compare;
using System;
using System.Collections.Generic;
using System.Linq;
using static E.Standard.CMS.Core.CmsDocument;

namespace E.Standard.CMS.Core.Extensions;

static public class AuthNodeExtensions
{
    static public bool HasDeniedMembers(this AuthNode authNode)
    {
        if (authNode != null)
        {
            if (authNode.Users != null && authNode.Users.Where(user => user.Allowed == false).Count() > 0)
            {
                return true;
            }

            if (authNode.Roles != null && authNode.Roles.Where(role => role.Allowed == false).Count() > 0)
            {
                return true;
            }
        }

        return false;
    }

    static public bool IsEqual(this AuthNode authNode, AuthNode canditate)
    {
        if (authNode == null && canditate == null)
        {
            return true;
        }

        if (authNode != null && canditate == null)
        {
            return false;
        }

        if (authNode == null && canditate == null)
        {
            return false;
        }

        return authNode.Equals(canditate);
    }

    static public bool InList(this AuthNode authNode, IEnumerable<AuthNode> canditates)
    {
        if (canditates == null)
        {
            return false;
        }

        foreach (var canditate in canditates)
        {
            if (authNode.IsEqual(canditate))
            {
                return true;
            }
        }

        return false;
    }

    //static public AuthNode ReduceToExclusives(this AuthNode authNode)
    //{
    //    if (CmsDocument.UseAuthExclusives && authNode != null && authNode.HasExclusives())
    //    {
    //        var reducedAuthNode = new AuthNode();

    //        if (authNode.Users != null)
    //        {
    //            reducedAuthNode.Users.AddRange(authNode.Users.Where(u => u.IsExclusive));
    //        }

    //        if (authNode.Roles != null)
    //        {
    //            reducedAuthNode.Roles.AddRange(authNode.Roles.Where(r=>r.IsExclusive));
    //        }

    //        return reducedAuthNode;
    //    }

    //    return authNode;
    //}

    static public AuthNode IgnoreAllowedIfHasExclusives(this AuthNode authNode)
    {
        if (CmsDocument.UseAuthExclusives && authNode?.HasExclusives() == true)
        {
            var reducedAuthNode = authNode.Clone();

            reducedAuthNode.Users.EachIfNotNull((user) =>
            {
                if (user.IsExclusive == false && user.Allowed == true)
                {
                    user.Ignore = true;
                }
            });

            reducedAuthNode.Roles.EachIfNotNull((role) =>
            {
                if (role.IsExclusive == false && role.Allowed == true)
                {
                    role.Ignore = true;
                }
            });

            return reducedAuthNode;
        }

        return authNode;
    }

    static public bool HasExclusivePostfixes(this AuthNode authNode)
    {
        if (authNode?.Users != null &&
            authNode.Users.Any(u => u.HasExclusivePostfix()))
        {
            return true;
        }

        if (authNode?.Roles != null &&
            authNode.Roles.Any(r => r.HasExclusivePostfix()))
        {
            return true;
        }

        return false;
    }

    static public bool HasExclusives(this AuthNode authNode)
    {
        if (authNode?.Users != null &&
            authNode.Users.Any(u => u?.IsExclusive == true))
        {
            return true;
        }

        if (authNode?.Roles != null &&
            authNode.Roles.Any(r => r?.IsExclusive == true))
        {
            return true;
        }

        return false;
    }

    static public IEnumerable<CmsUser> RemoveIgnored(this CmsUserList userlist)
    {
        return userlist?.Where(u => u.Ignore == false) ?? Array.Empty<CmsUser>();
    }
}
