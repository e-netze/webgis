using System;

namespace E.Standard.WebMapping.Core.Api.Reflection;


[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ServerToolCommandAttribute : System.Attribute
{
    public ServerToolCommandAttribute(string method)
    {
        this.Method = method;
    }

    public string Method { get; set; }
}
