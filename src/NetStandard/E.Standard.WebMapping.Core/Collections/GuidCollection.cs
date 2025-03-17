using E.Standard.ThreadSafe;
using System;

namespace E.Standard.WebMapping.Core.Collections;

public class GuidCollection : ThreadSafeList<Guid>
{
    public GuidCollection()
        : base()
    {
    }
    public GuidCollection(Guid[] guids)
        : base()
    {
        if (guids != null)
        {
            foreach (Guid guid in guids)
            {
                this.Add(guid);
            }
        }
    }

    new public void Add(Guid guid)
    {
        if (base.Contains(guid))
        {
            return;
        }

        base.Add(guid);
    }
}
