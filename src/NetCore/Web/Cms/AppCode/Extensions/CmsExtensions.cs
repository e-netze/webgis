using Cms.Models.Json;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.Json;
using E.Standard.WebGIS.CmsSchema.TypeEditor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Cms.AppCode.Extensions;

static public class CmsExtensions
{
    //static T GetCustomAttribute<T>(this PropertyInfo propertyInfo)
    //{
    //    return (T)propertyInfo.GetCustomAttribute()
    //}

    public static bool IsValueTypeOrString(this Type t)
    {
        return t.IsValueType || t == typeof(string);
    }

    public static void LoadData(this IUIControl control, string data)
    {
        if (control != null)
        {
            var formData = JSerializer.Deserialize<IEnumerable<NameValue>>(data);

            foreach (var nv in formData)
            {
                var input = control.GetControl(nv.Name) as IInputUIControl;
                if (input != null)
                {
                    input.Value = nv.Value;
                }
            }
            control.SetDirty(false);
        }
    }

    public static void LoadData(this ITypeEditor editor, string data)
    {
        if (editor is IUIControl)
        {
            ((IUIControl)editor).LoadData(data);
        }
        else
        {
            throw new Exception("Can't load TypeEditor data");
        }
    }

    public static string ToFullTypeName(this Type type)
    {
        return type.ToString() + ", " + type.Assembly.ToString();
    }

    public static string GetDescription(this Enum GenericEnum)
    {
        Type genericEnumType = GenericEnum.GetType();
        MemberInfo[] memberInfo = genericEnumType.GetMember(GenericEnum.ToString());
        if ((memberInfo != null && memberInfo.Length > 0))
        {
            var attribute = memberInfo[0].GetCustomAttribute<DescriptionAttribute>(false);
            if (attribute != null)
            {
                return attribute.Description;
            }
        }
        return GenericEnum.ToString();
    }

    public static (object instance, string instancePath) ClosestInstance(this CMSManager cmsManager,
                                                                         CmsItemTransistantInjectionServicePack servicePack,
                                                                         string path)
    {
        try
        {
            string[] pathParts = path.Split('/');

            for (int i = 0; i < pathParts.Length; i++)
            {
                var instancePath = string.Join("/", pathParts.Take(pathParts.Length - i));

                var instance = cmsManager.SchemaNodeInstance(servicePack, instancePath, true, true);

                if (instance != null)
                {
                    return (instance, instancePath);
                }
            }
        }
        catch { }

        return (null, null);
    }
}
