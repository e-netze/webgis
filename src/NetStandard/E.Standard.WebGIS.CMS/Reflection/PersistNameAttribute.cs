#nullable enable

using System;

namespace E.Standard.WebGIS.CMS.Reflection;

public class PersistNameAttribute : Attribute
{
    public PersistNameAttribute(string persistName)
    {
        PersistName = persistName;
    }

    public string PersistName { get; }

    public object? DefaultValue { get; set; } = null;
}

public class PersistNodeName : PersistNameAttribute
{
    public PersistNodeName() : base("name")
    {
        
    }
}
