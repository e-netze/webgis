using E.Standard.CMS.Core.Reflection;
using System.ComponentModel;
using System.Reflection;

namespace E.Standard.CMS.Core.Extensions;

static public class ReflectionExtensions
{
    static public bool IsPersistableCmsProperty(this PropertyInfo propertyInfo)
    {
        return propertyInfo.GetCustomAttribute<CmsPersistableAttribute>() != null;
    }

    static public bool UsePersistableCmsPropertyEncryption(this PropertyInfo propertyInfo)
    {
        return
            propertyInfo.PropertyType == typeof(string) &&
              (propertyInfo.GetCustomAttribute<SecretPropertyAttribute>() != null ||
               propertyInfo.GetCustomAttribute<PasswordPropertyTextAttribute>() != null);
    }
}
