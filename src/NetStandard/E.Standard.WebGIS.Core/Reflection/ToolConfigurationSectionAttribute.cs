using System;

namespace E.Standard.WebGIS.Core.Reflection;

public class ToolConfigurationSectionAttribute : Attribute
{
    public ToolConfigurationSectionAttribute(string name)
    {
        this.SectionName = name;
    }

    public string SectionName { get; }
}
