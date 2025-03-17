using E.Standard.Extensions.Compare;
using E.Standard.WebMapping.Core.Api.Abstraction;
using Microsoft.Extensions.Localization;

namespace E.Standard.Localization.Extensions;

public static class ApiButtonExtentions
{
    static public string LocalizedName(this IApiButton? button, IStringLocalizer localizer)
    {
        if (button is null) return "";

        string key = $"tools.{button.GetType().Name.ToLowerInvariant()}.name";
        var name = localizer[key];

        return name.ResourceNotFound
            ? button.Name
            : name.Value;
    }

    static public string LocalizedToolTip(this IApiButton? button, IStringLocalizer localizer)
    {
        if (button is null) return "";

        string key = $"tools.{button.GetType().Name.ToLowerInvariant()}.name:body";
        var name = localizer[key];

        return name.ResourceNotFound
            ? button.ToolTip
            : name.Value;
    }
}