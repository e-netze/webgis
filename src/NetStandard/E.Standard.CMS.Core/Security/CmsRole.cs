namespace E.Standard.CMS.Core.Security;

public class CmsRole : CmsAuthItem
{
    public CmsRole(string name, bool allowed)
        : base(name, allowed)
    {
    }
    public CmsRole(string name, bool allowed, string inheritFrom)
        : base(name, allowed, inheritFrom)
    {
    }
}
