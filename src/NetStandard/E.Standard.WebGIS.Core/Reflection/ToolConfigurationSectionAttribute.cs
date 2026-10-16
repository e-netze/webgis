using System;

namespace E.Standard.WebGIS.Core.Reflection;

public class ToolConfigurationSectionAttribute : Attribute
{
    public const string TocContextMenu = "toc-contextmenu";

    /// <param name="name">section name in api.config</param>
    /// <param name="isToolSection">true: section "tool-{name}", false: section "{name}" (eg. toc-contextmenu)</param>
    public ToolConfigurationSectionAttribute(string name, bool isToolSection = true)
    {
        this.SectionName = name;
        this.IsToolSection = isToolSection;
    }

    public string SectionName { get; }

    public bool IsToolSection { get; }

    public string FullSectionName => IsToolSection ? $"tool-{SectionName}" : SectionName;
}
