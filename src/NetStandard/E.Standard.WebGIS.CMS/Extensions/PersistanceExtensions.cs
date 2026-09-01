using System;
using System.Diagnostics;
using System.Reflection;

using E.Standard.CMS.Core;
using E.Standard.WebGIS.CMS.Reflection;

namespace E.Standard.WebGIS.CMS.Extensions;

public static class PersistanceExtensions
{
    public static T BindCmsNode<T>(this T instance, CmsNode cmsNode) where T : class
    {
        foreach (var property in instance.GetType().GetProperties())
        {
            var names = property.GetCustomAttributes<PersistNameAttribute>(true) ?? [];

            foreach (var name in names)
            {
                if (property.PropertyType.IsEnum)
                {
                    property.SetValue(instance, cmsNode.Load(name.PersistName,
                        name.DefaultValue is not null
                           ? Convert.ToInt32(name.DefaultValue)
                           : (int)0));
                }

                else if (property.PropertyType.IsValueType)
                {
                    property.SetValue(instance,
                        cmsNode.Load(
                                name.PersistName,
                                Convert.ChangeType(name.DefaultValue ?? 0, property.PropertyType)
                        ));

                }

                else if (property.PropertyType == typeof(string))
                {
                    var stringValue = cmsNode.LoadString(name.PersistName);
                    if(String.IsNullOrEmpty(stringValue) && name.DefaultValue is not null)
                    {
                        stringValue = name.DefaultValue.ToString();
                    }
                    property.SetValue(instance, stringValue);
                }

                else if (property.PropertyType.IsArray)
                {
                    string arrayString = cmsNode.LoadString(name.PersistName);

                    if (!String.IsNullOrEmpty(arrayString))
                    {
                        property.SetValue(instance,
                            System.Text.Json.JsonSerializer.Deserialize(arrayString, property.PropertyType));
                    }
                }

                else
                {
                    throw new NotImplementedException($"{instance.GetType()}.{property.Name}: Can't bind CMS Variabile of type {property.PropertyType}!");
                }
            }
        }

        return instance;
    }
}
