using System;

namespace E.Standard.WebMapping.Core.Api.Reflection;


[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ToolStorageFriendTypeAttribute : System.Attribute
{
    public ToolStorageFriendTypeAttribute(Type friendType)
    {
        this.FriendType = friendType;
    }

    public Type FriendType
    {
        get;
        private set;
    }
}
