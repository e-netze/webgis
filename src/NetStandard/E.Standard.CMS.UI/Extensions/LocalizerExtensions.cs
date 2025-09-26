#nullable enable

using E.Standard.Localization.Abstractions;
using System;

namespace E.Standard.CMS.UI.Extensions;

static public class LocalizerExtensions
{
    static public string LocalizeOrDefault(
            this ILocalizer? localizer,
            string key,
            string defaultValue)
    {
        if (localizer is null || String.IsNullOrEmpty(key))
        {
            return defaultValue;
        }
        var localized = localizer.Localize(key, defaultValue: ILocalizer.LocalizerDefaultValue.Null);
        if (String.IsNullOrEmpty(localized))
        {
            return defaultValue;
        }

        return localized;
    }
}
