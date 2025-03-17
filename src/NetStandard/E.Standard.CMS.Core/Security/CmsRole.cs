namespace E.Standard.CMS.Core.Security;

public class CmsRole : CmsUser
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
