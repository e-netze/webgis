using System;

namespace E.Standard.CMS.Core.Reflection;

public class CmsPersistableAttribute : Attribute
{
    public CmsPersistableAttribute(string name)
    {
        this.Name = name;
    }

    public string Name { get; }
}
