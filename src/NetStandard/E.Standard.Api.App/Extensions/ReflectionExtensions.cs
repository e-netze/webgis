using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using System;
using System.Reflection;

namespace E.Standard.Api.App.Extensions;

public static class ReflectionExtensions
{
    static public T GetCustomAttribute<T>(this Type type, bool inherit = true)
        where T : Attribute
    {
        object[] attributes = type.GetCustomAttributes(typeof(T), true);
        if (attributes != null && attributes.Length >= 1)
        {
            return (T)attributes[0];
        }

        return null;
    }

    static public T GetCustomAttribute<T>(this PropertyInfo pi)
        where T : Attribute
    {
        object[] attributes = pi.GetCustomAttributes(typeof(T), true);

        if (attributes != null && attributes.Length >= 1)
        {
            return (T)attributes[0];
        }

        return null;
    }

    static public T GetCustomAttributeOrDefault<T>(this Type type, bool inherit = true)
        where T : Attribute, new()
    {
        object[] attributes = type.GetCustomAttributes(typeof(T), true);
        if (attributes != null && attributes.Length >= 1)
        {
            return (T)attributes[0];
        }

        return Activator.CreateInstance<T>();
    }

    static public T EncryptSecureProperties<T>(this T o, ICryptoService cryptoService)
    {
        if (o == null)
        {
            return default(T);
        }

        foreach (var pi in o.GetType().GetProperties())
        {
            if (pi.PropertyType == typeof(string) && pi.GetCustomAttribute<SecureStringAttribute>() != null)
            {
                pi.SetValue(o, ((string)pi.GetValue(o))?.EncryptStringProperty(cryptoService));
            }
        }
        return o;
    }

    static public T DecryptSecureProperties<T>(this T o, ICryptoService cryptoService)
    {
        if (o == null)
        {
            return default(T);
        }

        foreach (var pi in o.GetType().GetProperties())
        {
            if (pi.PropertyType == typeof(string) && pi.GetCustomAttribute<SecureStringAttribute>() != null)
            {
                pi.SetValue(o, ((string)pi.GetValue(o))?.DecryptStringProperty(cryptoService));
            }
        }
        return o;
    }
}
