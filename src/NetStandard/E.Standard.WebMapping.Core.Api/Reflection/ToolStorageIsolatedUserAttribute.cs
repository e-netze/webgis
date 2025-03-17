using System;

namespace E.Standard.WebMapping.Core.Api.Reflection;


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ToolStorageIsolatedUserAttribute : System.Attribute
{
    public ToolStorageIsolatedUserAttribute()
        : this(true)
    {

    }

    public ToolStorageIsolatedUserAttribute(bool isUserIsolated = true)
    {
        this.IsUserIsolated = isUserIsolated;
    }

    public bool IsUserIsolated
    {
        get;
        private set;
    }
}
