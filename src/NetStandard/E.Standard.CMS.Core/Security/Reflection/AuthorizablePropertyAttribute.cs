using System;

namespace E.Standard.CMS.Core.Security.Reflection;

public class AuthorizablePropertyAttribute : Attribute
{
    private string _tagName = string.Empty;
    private object _defaultValue = null;

    public AuthorizablePropertyAttribute(string propertyTagName, object defaultvalue)
    {
        _tagName = propertyTagName;
        _defaultValue = defaultvalue;
    }

    public string TagName
    {
        get { return _tagName; }
    }
    public object DefaultValue
    {
        get { return _defaultValue; }
    }
}
