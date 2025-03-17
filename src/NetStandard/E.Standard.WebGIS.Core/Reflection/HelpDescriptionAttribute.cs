using System;

namespace E.Standard.WebGIS.Core.Reflection;

public class HelpDescriptionAttribute : Attribute
{
    public HelpDescriptionAttribute(string description)
    {
        this.Description = description;
    }
    public string Description { get; set; }
}
