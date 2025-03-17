namespace E.Standard.ActiveDirectory;

public class AdGroup : AdObject
{
    public AdGroup(string groupName, string name)
    {
        this.Groupname = groupName;
        this.Name = name;
    }
    public string Groupname { get; set; }
}
