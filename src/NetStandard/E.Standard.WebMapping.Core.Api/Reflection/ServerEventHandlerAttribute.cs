using System;

namespace E.Standard.WebMapping.Core.Api.Reflection;


[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ServerEventHandlerAttribute : System.Attribute
{
    public ServerEventHandlerAttribute(ServerEventHandlers handler)
    {
        this.Handler = handler;
    }

    public ServerEventHandlers Handler { get; set; }
}
