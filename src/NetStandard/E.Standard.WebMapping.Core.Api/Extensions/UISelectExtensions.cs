using E.Standard.WebMapping.Core.Api.UI.Elements;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.Extensions;

static public class UISelectExtensions
{
    static public UISelect AddOption(this UISelect select, UISelect.Option option)
    {
        if (select.options == null)
        {
            select.options = new List<UISelect.Option>();
        }

        select.options.Add(option);

        return select;
    }

    static public UISelect AddOptions(this UISelect select, params UISelect.Option[] options)
        => select.AddOptions((IEnumerable<UISelect.Option>)options);

    static public UISelect AddOptions(this UISelect select, IEnumerable<UISelect.Option> options)
    {
        if (options == null)
        {
            return select;
        }

        if (select.options == null)
        {
            select.options = new List<UISelect.Option>();
        }
        else if (!(select.options is List<UISelect.Option>))
        {
            select.options = new List<UISelect.Option>(select.options);
        }

        ((List<UISelect.Option>)select.options).AddRange(options);

        return select;
    }

    static public UISelect.Option WithLabel(this UISelect.Option option, string label)
    {
        if (option != null)
        {
            option.label = label;
        }

        return option;
    }

    static public UISelect.Option WithValue(this UISelect.Option option, string value)
    {
        if (option != null)
        {
            option.value = value;
        }

        return option;
    }
}
