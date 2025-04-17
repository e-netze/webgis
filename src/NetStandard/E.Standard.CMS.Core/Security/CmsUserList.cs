using E.Standard.CMS.Core.Collections;
using E.Standard.Extensions.Formatting;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace E.Standard.CMS.Core.Security;

/*
public class CmsUserList_old : List<CmsUser>
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
        if (this.Any(u => u?.Name?.Equals(user.Name, StringComparison.OrdinalIgnoreCase) == true))
        {
            return;
        }

        base.Add(user);
    }

    new public void AddRange(IEnumerable<CmsUser> user)
    {
        if (user == null)
        {
            return;
        }

        foreach (CmsUser u in user)
        {
            this.Add(u);
        }
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
*/

public class CmsUserList
{
    private CmsUser[] _cmsUsers;
    private int _count;

    private CmsUserList()
    {
    }

    public CmsUserList(IEnumerable<CmsUser> cmsUsers)
    {
        UniqueItemListBuilder builder = new();
      
        builder.AddRange(cmsUsers);

        _cmsUsers = builder.Build();
        _count = _cmsUsers.Length;
    }

    public CmsUser[] Items => _cmsUsers ?? [];

    public CmsUser[] AllowedItems
        => _cmsUsers?.Where(u => u.Allowed).ToArray() ?? [];

    public CmsUser[] DeniedItems
        => _cmsUsers?.Where(u => !u.Allowed).ToArray() ?? [];

    public int Count => _count;

    public CmsUserList Clone()
    {
        CmsUserList clone = new CmsUserList();
        clone._cmsUsers = _cmsUsers.Select(u => u.Clone()).ToArray();

        return clone;
    }

    #region Classes

    public class UniqueItemListBuilder
    {
        private UniqueListBuilder<CmsUser> _builder = new();

        public UniqueItemListBuilder()
        {
            
        }

        public UniqueItemListBuilder(IEnumerable<CmsUser> items)
        {
            AddRange(items);
        }

        public void Add(CmsUser item)
        {
            if(item is null)
            {
                return;
            }

            // if user/role with same exits => return => first wins...
            _builder.Add(item, (candidate => candidate.Name?.Equals(item.Name, StringComparison.OrdinalIgnoreCase) == true));
        }

        public void AddRange(IEnumerable<CmsUser> items)
        {
            foreach(var item in items)
            {
                this.Add(item);
            }
        }

        public void Remove(CmsUser item)
        {
            _builder.Remove(item);
        }

        public void Clear() => _builder = new UniqueListBuilder<CmsUser>();

        public void ForEeach(Action<CmsUser> action)
        {
            foreach (var item in _builder.GetList())
            {
                action(item);
            }
        }

        public bool Any(Func<CmsUser, bool> predicate)
        {
            foreach (var item in _builder.GetList())
            {
                if (predicate(item))
                {
                    return true;
                }
            }
            return false;
        }

        public CmsUser[] Build() => _builder.GetList().ToArray();
    }

    #endregion

    #region Static Members

    static public readonly CmsUserList Empty = new CmsUserList([]);

    static public readonly CmsUserList Everyone = new CmsUserList(new CmsUser[]
    {
        new CmsUser(CmsDocument.Everyone, true)
    });

    #endregion
}
