using System;
using System.Linq;

namespace E.Standard.Extensions.Reflection;
public static class TypeExtensions
{
    static public bool ImplementsAnyInterface(this Type type, params Type[] interfaces)
    {
        return interfaces.Any(interfaceType =>
            interfaceType.IsGenericTypeDefinition
                ? type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType)
                : interfaceType.IsAssignableFrom(type)
        );
    }
}
