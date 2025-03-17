using System.Reflection;

namespace E.Standard.Json.Test.Extensions;

static internal class TypeExtensions
{
    static public Type[] GetAllAssemblyTypes(this Type type, Func<Type, bool> predicate)
    {
        var assembly = type.Assembly;

        List<Type> collectedTypes = new();

        foreach (var assemblyType in assembly.GetTypes())
        {
            if (predicate(assemblyType))
            {
                collectedTypes.Add(assemblyType);
            }
        }

        return collectedTypes.ToArray();
    }

    static public bool HasJsonAttribute(this Type type)
    {
        foreach (var propertyInfo in type.GetProperties())
        {
            bool hasJsonAttribute
                = propertyInfo.GetCustomAttribute<Newtonsoft.Json.JsonIgnoreAttribute>(true) != null
                || propertyInfo.GetCustomAttribute<Newtonsoft.Json.JsonPropertyAttribute>(true) != null
                || propertyInfo.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>(true) != null
                || propertyInfo.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>(true) != null;

            if (hasJsonAttribute)
            {
                return true;
            }
        }

        return false;
    }
}
