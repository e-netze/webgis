using System;

namespace E.Standard.WebGIS.Core.Reflection;

[AttributeUsage(AttributeTargets.Class)]
public class ToolHelpAttribute : Attribute
{
    public ToolHelpAttribute(string urlPath, string urlPathDefaultTool = null)
    {
        UrlPath = urlPath;
        UrlPathDefaultTool = urlPathDefaultTool;
    }

    public string UrlPath { get; }

    public string UrlPathDefaultTool { get; }
}
