using System;

namespace E.Standard.CMS.Core.Reflection;

public class CmsUIAttribute : Attribute
{
    public string PrimaryDisplayProperty { get; set; }
}
