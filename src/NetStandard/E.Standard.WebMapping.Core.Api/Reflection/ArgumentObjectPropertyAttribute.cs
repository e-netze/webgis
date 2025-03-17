namespace E.Standard.WebMapping.Core.Api.Reflection;

public class ArgumentObjectPropertyAttribute : System.Attribute
{
    public ArgumentObjectPropertyAttribute(int index)
    {
        this.Index = index;
    }

    public int Index { get; set; }
}
