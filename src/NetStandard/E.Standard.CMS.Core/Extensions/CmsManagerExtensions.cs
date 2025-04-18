using E.Standard.CMS.Core.Security;
using System;

namespace E.Standard.CMS.Core.Extensions;

static internal class CmsManagerExtensions
{
    static public bool HasExclusivePostfix(this CmsAuthItem authItem)
        => authItem?.Name != null
        && authItem.Name.EndsWith(CmsDocument.AuthExclusivePostfix, StringComparison.InvariantCultureIgnoreCase);

    static public CmsAuthItem RemoveExclusivePostfixAndSetProperty(this CmsAuthItem authItem)
    {
        if (!authItem.HasExclusivePostfix())
        {
            return authItem;
        }

        var clone = authItem.Clone();
        clone.Name = clone.Name.Substring(0, clone.Name.Length - CmsDocument.AuthExclusivePostfix.Length);
        clone.IsExclusive = true;
        return clone;
    }

    static public bool IsAllowedAndNotExclusive(this CmsAuthItem authItem)
        => authItem != null
        && authItem.Allowed == true
        && authItem.HasExclusivePostfix() == false
        && authItem.IsExclusive == false;
}
