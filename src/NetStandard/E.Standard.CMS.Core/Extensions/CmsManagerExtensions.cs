using E.Standard.CMS.Core.Security;
using System;

namespace E.Standard.CMS.Core.Extensions;

static internal class CmsManagerExtensions
{
    static public bool HasExclusivePostfix(this CmsUser user)
        => user?.Name != null
        && user.Name.EndsWith(CmsDocument.AuthExclusivePostfix, StringComparison.InvariantCultureIgnoreCase);

    static public CmsUser RemoveExclusivePostfixAndSetProperty(this CmsUser user)
    {
        if (!user.HasExclusivePostfix())
        {
            return user;
        }

        var clone = user.Clone();
        clone.Name = clone.Name.Substring(0, clone.Name.Length - CmsDocument.AuthExclusivePostfix.Length);
        clone.IsExclusive = true;
        return clone;
    }

    static public bool IsAllowedAndNotExclusive(this CmsUser user)
        => user != null
        && user.Allowed == true
        && user.HasExclusivePostfix() == false
        && user.IsExclusive == false;
}
