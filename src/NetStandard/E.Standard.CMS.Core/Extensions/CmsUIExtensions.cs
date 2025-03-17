using E.Standard.CMS.Core.Reflection;
using System;
using System.ComponentModel;
using System.Reflection;

namespace E.Standard.CMS.Core.Extensions;

static public class CmsUIExtensions
{
    static public string CmsUIPrimaryDisplayPropertyValue(this object obj)
    {
        var cmsUIAttribute = obj?.GetType().GetCustomAttribute<CmsUIAttribute>();
        if (!String.IsNullOrEmpty(cmsUIAttribute?.PrimaryDisplayProperty))
        {
            var propertyNames = cmsUIAttribute.PrimaryDisplayProperty.Split('.');
            for (int i = 0; i < propertyNames.Length; i++)
            {
                var propertyInfo = obj?.GetType().GetProperty(propertyNames[i]);
                if (propertyInfo != null)
                {
                    if (i == propertyNames.Length - 1)
                    {
                        var displayFieldAttrubite = propertyInfo.GetCustomAttribute<DisplayNameAttribute>();

                        return $"{displayFieldAttrubite?.DisplayName ?? propertyInfo.Name}: {propertyInfo.GetValue(obj)?.ToString()}";
                    }
                    else
                    {
                        obj = propertyInfo.GetValue(obj);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        return null;
    }
}
