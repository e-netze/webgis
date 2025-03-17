using System;

namespace E.Standard.Custom.Core.Reflection;

public class CustomServiceAttribute : Attribute
{
    public WebGISAppliationTarget Target { get; set; }
}
