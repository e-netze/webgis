using E.Standard.CMS.Core.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.CMS.Core.Security;

public class CmsAuthItemList
{
    private readonly CmsAuthItem[] _cmsAuthItems;
    private readonly CmsAuthItem[] _cmsAllowedAuthItems;
    private readonly CmsAuthItem[] _cmsDeniedAuthItems;
    private int _count;

    public CmsAuthItemList(IEnumerable<CmsAuthItem> cmsUsers)
    {
        UniqueItemListBuilder builder = new();

        builder.AddRange(cmsUsers);

        _cmsAuthItems = builder.Build() ?? [];
        _count = _cmsAuthItems.Length;

        _cmsAllowedAuthItems = _cmsAuthItems?.Where(u => u.Allowed).ToArray();
        _cmsDeniedAuthItems = _cmsAuthItems?.Where(u => !u.Allowed).ToArray();
    }

    public CmsAuthItem[] Items => _cmsAuthItems ?? [];

    public CmsAuthItem[] AllowedItems => _cmsAllowedAuthItems;

    public CmsAuthItem[] DeniedItems => _cmsDeniedAuthItems;

    public int Count => _count;

    public CmsAuthItemList Clone()
    {
        CmsAuthItemList clone = new CmsAuthItemList(_cmsAuthItems.Select(u => u.Clone()));

        return clone;
    }

    #region Classes

    public class UniqueItemListBuilder
    {
        private UniqueListBuilder<CmsAuthItem> _builder = new();

        public UniqueItemListBuilder()
        {

        }

        public UniqueItemListBuilder(IEnumerable<CmsAuthItem> items)
        {
            AddRange(items);
        }

        public void Add(CmsAuthItem item)
        {
            if (item is null)
            {
                return;
            }

            // if user/role with same exits => return => first wins...
            _builder.Add(item, (candidate => candidate.Name?.Equals(item.Name, StringComparison.OrdinalIgnoreCase) == true));
        }

        public void AddRange(IEnumerable<CmsAuthItem> items)
        {
            foreach (var item in items)
            {
                this.Add(item);
            }
        }

        public void Remove(CmsAuthItem item)
        {
            _builder.Remove(item);
        }

        public void Clear() => _builder = new UniqueListBuilder<CmsAuthItem>();

        public void ForEeach(Action<CmsAuthItem> action)
        {
            foreach (var item in _builder.GetList())
            {
                action(item);
            }
        }

        public bool Any(Func<CmsAuthItem, bool> predicate)
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

        public CmsAuthItem FirstOfDefault(Func<CmsAuthItem, bool> predicate)
        {
            foreach (var item in _builder.GetList())
            {
                if (predicate(item))
                {
                    return item;
                }
            }

            return default;
        }

        public CmsAuthItem[] Build() => _builder.GetList().ToArray();
    }

    #endregion

    #region Static Members

    static public readonly CmsAuthItemList Empty = new CmsAuthItemList([]);

    static public readonly CmsAuthItemList Everyone = new CmsAuthItemList(new CmsUser[]
    {
        new CmsUser(CmsDocument.Everyone, true)
    });

    #endregion
}
