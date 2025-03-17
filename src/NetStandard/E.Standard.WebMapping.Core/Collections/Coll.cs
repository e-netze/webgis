using E.Standard.ThreadSafe;

namespace E.Standard.WebMapping.Core.Collections;

public class Coll : ThreadSafeList<object>
{
    public Coll(System.Collections.IEnumerable c)
    {
        foreach (object obj in c)
        {
            this.Add(obj);
        }
    }
}
