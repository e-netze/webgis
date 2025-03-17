using System;

namespace E.Standard.WebMapping.Core.Api.Reflection;


[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ToolCmsConfigParameterAttribute : System.Attribute
{
    public ToolCmsConfigParameterAttribute(string cmsParameterName, Type type = null)
    {
        this.CmsParameterName = cmsParameterName;
        this.ValueType = type != null ? type : typeof(String);
    }

    public string CmsParameterName
    {
        get;
        private set;
    }

    public Type ValueType
    {
        get;
        private set;
    }
}
