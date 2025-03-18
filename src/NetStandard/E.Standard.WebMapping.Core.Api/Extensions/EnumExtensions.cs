using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.Extensions;

static public class EnumExtensions
{
    static public ICollection<UISelect.Option> ToUISelectOptions<T>(IBridge bridge)
        where T : Enum
    {
        List<UISelect.Option> options = new List<UISelect.Option>();

        foreach (var i in Enum.GetValues(typeof(T)))
        {
            options.Add(new UISelect.Option() { value = ((int)i).ToString(), label = bridge.LocalizeString(null, i.ToString()) });
        }

        return options;
    }

    static public IDictionary<string, string> ToDescriptionDictionary<T>(IBridge bridge)
        where T : Enum
    {
        var dict = new Dictionary<string, string>();

        foreach (var i in Enum.GetValues(typeof(T)))
        {
            dict.Add(((int)i).ToString(), bridge.LocalizeString(null, $"{i}-description"));
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
