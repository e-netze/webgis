namespace E.Standard.CMS.Core.Security;

public class CmsUser
{
    public CmsUser(string name, bool allowed)
    {
        Name = name;
        Allowed = allowed;
        InheritFrom = string.Empty;

        Ignore = false;
        IsExclusive = false;
    }
    public CmsUser(string name, bool allowed, string inheritFrom)
    {
        Name = name;
        Allowed = allowed;
        InheritFrom = inheritFrom;

        Ignore = false;
        IsExclusive = false;
    }

    public string Name { get; set; }
    public string InheritFrom { get; set; }
    public bool Allowed { get; set; }
    public bool Ignore { get; set; }
    public bool IsExclusive { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is CmsUser)
        {
            var user = (CmsUser)obj;

            return user.Name == Name &&
                   user.Allowed == Allowed &&
                   user.InheritFrom == InheritFrom &&
                   user.Ignore == Ignore &&
                   IsExclusive == IsExclusive;
        }

        return false;
    }

    public CmsUser Clone()
    {
        CmsUser clone = this is CmsRole
            ? new CmsRole(Name, Allowed)
            : new CmsUser(Name, Allowed);

        clone.InheritFrom = InheritFrom;
        clone.Ignore = Ignore;
        clone.IsExclusive = IsExclusive;
        clone.Title = Title;
        clone.Description = Description;

        return clone;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
