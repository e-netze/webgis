namespace E.Standard.Custom.Core.Models;

public class CustomSecurityPrefix
{
    public CustomSecurityPrefix() { }
    public CustomSecurityPrefix(string n, string t)
    {
        this.name = n;
        this.type = t;
    }

    public string name { get; set; }
    public string type { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is CustomSecurityPrefix)
        {
            return ((CustomSecurityPrefix)obj).name == name && ((CustomSecurityPrefix)obj).type == type;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
