using System;

namespace E.Standard.WebMapping.Core;

public class Attribute2 : Attribute
{
    public Attribute2(string name, object val)
    {
        Name = name;
        Value2 = val;
    }
    public object Value2;
    override public string Value
    {
        get { return Value2 != null ? Value2.ToString() : String.Empty; }
        set { Value2 = value; }
    }
}
