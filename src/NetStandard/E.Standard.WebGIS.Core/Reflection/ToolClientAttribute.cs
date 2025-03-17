using System;

namespace E.Standard.WebGIS.Core.Reflection;

public class ToolClientAttribute : Attribute
{
    public ToolClientAttribute(string client)
    {
        this.ClientName = client;
    }

    public string ClientName { get; set; }
}
