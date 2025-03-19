using E.Standard.Localization.Reflection;
using E.Standard.WebMapping.Core.Api.Abstraction;
using System.Reflection;

namespace E.Standard.Localization.Extensions;
static public class TypeExtensions
{
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

        if (typeof(IApiButton).IsAssignableFrom(type))
        {
            return $"tools.{type.Name.ToLowerInvariant()}";
        }

        return type.Name.ToLowerInvariant();
    }
}
