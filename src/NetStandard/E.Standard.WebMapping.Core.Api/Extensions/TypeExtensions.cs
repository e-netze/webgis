using E.Standard.Extensions.Reflection;
using E.Standard.WebMapping.Core.Api.Abstraction;
using System;

namespace E.Standard.WebMapping.Core.Api.Extensions;
static public class TypeExtensions
{
    static public bool IsApiToolOrButtonNonAsync(this Type type)
    {
        return type.ImplementsAnyInterface(
                                typeof(IApiServerButton),
                                typeof(IApiServerTool),
                                typeof(IApiServerToolLocalizable<>),
                                typeof(IApiClientTool));
    }

    static public bool IsApiToolOrButtonAsync(this Type type)
    {
        return type.ImplementsAnyInterface(   
                                typeof(IApiServerToolAsync),
                                typeof(IApiServerButtonAsync),
                                typeof(IApiServerToolLocalizableAsync<>));
    }

    static public bool IsApiToolOrButton(this Type type)
    {
        return type.IsApiToolOrButtonNonAsync() 
            || type.IsApiToolOrButtonAsync();
    }

    static public bool IsApiServerToolNonAsync(this Type type)
    {
        return type.ImplementsAnyInterface(
                                typeof(IApiServerTool),
                                typeof(IApiServerToolLocalizable<>));
    }

    static public bool IsApiServerToolAsync(this Type type)
    {
        return type.ImplementsAnyInterface(
                                typeof(IApiServerToolAsync),
                                typeof(IApiServerToolLocalizableAsync<>));
    }

    static public bool IsApiServerTool(this Type type)
    {
        return type.IsApiServerToolNonAsync()
            || type.IsApiServerToolAsync();
    }
}
