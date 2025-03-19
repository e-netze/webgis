using E.Standard.Localization.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.Extensions;

static public class EnumExtensions
{
    static public ICollection<UISelect.Option> ToUISelectOptions<TEnum>(ILocalizer localizer)
        where TEnum : Enum
    {
        List<UISelect.Option> options = new List<UISelect.Option>();

        foreach (var i in Enum.GetValues(typeof(TEnum)))
        {
            options.Add(new UISelect.Option() { value = ((int)i).ToString(), label = localizer.Localize(i.ToString()) });
        }

        return options;
    }

    static public IDictionary<string, string> ToDescriptionDictionary<TEnum>(ILocalizer localizer)
        where TEnum : Enum
    {
        var dict = new Dictionary<string, string>();

        foreach (var i in Enum.GetValues(typeof(TEnum)))
        {
            dict.Add(((int)i).ToString(), localizer.Localize($"{i}-description"));
        }

        return dict;
    }

    static public T ParseOrDefault<T>(this string value, T defaultValue) where T : struct, Enum
        => string.IsNullOrEmpty(value)
        ? defaultValue
        : Enum.TryParse<T>(value, true, out T result)
            ? result
            : defaultValue;
}
