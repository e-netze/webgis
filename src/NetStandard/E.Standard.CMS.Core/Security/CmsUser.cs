namespace E.Standard.CMS.Core.Security;

public class CmsUser : CmsAuthItem
{
    public CmsUser(string name, bool allowed)
        : base(name, allowed)
    {
    }
    public CmsUser(string name, bool allowed, string inheritFrom)
        : base(name, allowed, inheritFrom)
    {
    }
}
