using System;

namespace E.Standard.CMS.Core.Security.Reflection;

public class AuthorizablePropertyArrayAttribute : Attribute
{
    private string _tagName = string.Empty;

    public AuthorizablePropertyArrayAttribute(string propertyTagName)
    {
        _tagName = propertyTagName;
    }

    public string TagName
    {
        get { return _tagName; }
    }
}
