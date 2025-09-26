using E.Standard.CMS.Core.Exceptions;
using E.Standard.Localization.Reflection;
using System;
using System.Reflection;

namespace E.Standard.CMS.Core.Extensions;

public static class TypeExtensions
{
    static public object CmsPropertyDefaultValue(this Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        if (type == typeof(string))
        {
            return string.Empty;
        }

        throw new ArgumentException($"Can't determine default value for type {type}");
    }

    static public object CmsCreateInstance(this Type type, CmsItemTransistantInjectionServicePack servicePack)
    {
        if (type == null)
        {
            throw new CmsCreateInstanceException("Can't create instance: Type == null");
        }

        try
        {
            if (type.GetConstructor(new Type[] { typeof(CmsItemTransistantInjectionServicePack) }) != null)
            {
                return Activator.CreateInstance(type, new[] { servicePack }, null);
            }
            else
            {
                return Activator.CreateInstance(type);
            }
        }
        catch (Exception ex)
        {
            throw new CmsCreateInstanceException($"Can't create instance from type {type}", ex);
        }
    }

    static public string GetLocalizationNamespace(this Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var lsa = type.GetCustomAttribute<LocalizationNamespaceAttribute>();
        if (lsa is not null)
        {
            return lsa.Namespace;
        }

        return type.Name.ToLowerInvariant();
    }
}
