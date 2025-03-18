using E.Standard.Localization.Reflection;
using E.Standard.WebMapping.Core.Api.Abstraction;
using Microsoft.Extensions.Localization;
using System.Reflection;

namespace E.Standard.Localization.Extensions;

public static class ApiButtonExtentions
{
    static public string LocalizedName(this IApiButton? button, IStringLocalizer localizer)
        => button.LocalizeButtonProperty(localizer, "name", () => button?.Name);

    static public string LocalizedToolTip(this IApiButton? button, IStringLocalizer localizer)
        => button.LocalizeButtonProperty(localizer, "name:body", () => button?.ToolTip);

    static public string LocalizedContainer(this IApiButton? button, IStringLocalizer localizer)
        => button.LocalizeButtonProperty(localizer, "container", () => button?.Container);

    static public string LocalizeButtonProperty(this IApiButton? button, IStringLocalizer localizer, string subKey, Func<string?> valueProvider)
    {
        if (button is null)
        {
            return "";
        }

        var localizationNamespace = button.GetType().GetCustomAttribute<LocalizationNamespaceAttribute>() switch
        {
            null => $"tools.{button.GetType().Name.ToLowerInvariant()}",
            LocalizationNamespaceAttribute lsa => lsa.Namespace
        };

        string key = $"{localizationNamespace}.{subKey}";
        var name = localizer[key];

        return name.ResourceNotFound
            ? valueProvider() ?? ""
            : name.Value;
    }
}