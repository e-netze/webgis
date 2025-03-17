using System;

namespace E.Standard.WebGIS.Core.Reflection;

public class ExportAttribute : Attribute
{
    public ExportAttribute(Type type)
    {
        this.ExportType = type;
    }

    public Type ExportType { get; set; }
}
