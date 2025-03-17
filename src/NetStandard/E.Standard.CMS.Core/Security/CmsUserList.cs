using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.CMS.Core.Security;

public class CmsUserList : List<CmsUser>
{
    public CmsUserList()
    {
    }
    public CmsUserList(IEnumerable<CmsUser> user)
        : base(user)
    {
    }

    new public void Add(CmsUser user)
    {
        if (user == null)
        {
            return;
        }

        // if user/role with same exits => return => first winns
        if (this.Any(u => u?.Name?.Equals(user.Name, StringComparison.InvariantCultureIgnoreCase) == true))
        {
            return;
        }

        base.Add(user);
    }

    public CmsUserList AllowedItems
    {
        get
        {
            CmsUserList l = new CmsUserList();
            foreach (CmsUser u in this)
            {
                if (u.Allowed)
                {
                    l.Add(u);
                }
            }

            return l;
        }
    }

    public CmsUserList DeniedItems
    {
        get
        {
            CmsUserList l = new CmsUserList();
            foreach (CmsUser u in this)
            {
                if (!u.Allowed)
                {
                    l.Add(u);
                }
            }

            return l;
        }
    }
}
