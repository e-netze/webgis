using System;
using System.Collections.Generic;

namespace E.Standard.Api.App.Extensions;

public static class HtmlExtensions
{
    public static IEnumerable<TItem> EnumToSelectList<TItem, TEnum>(this TEnum enumObj)
        where TEnum : struct, IComparable, IFormattable, IConvertible
    {
        List<TItem> items = new List<TItem>();

        foreach (var val in Enum.GetValues(typeof(TEnum)))
        {
            items.Add(CreateItem<TItem>(val.ToString(), val.ToString()));
        }

        return items;
    }

    public static IEnumerable<TItem> ToSelectList<TItem, T>(this IEnumerable<T> list)
    {
        List<TItem> items = new List<TItem>();

        foreach (var listItem in list)
        {
            items.Add(CreateItem<TItem>(
                listItem.ToString().Contains(".") ? listItem.ToString().Substring(listItem.ToString().LastIndexOf(".") + 1) : listItem.ToString(),
                listItem.ToString()
                ));
        }

        return items;
    }

    private static TItem CreateItem<TItem>(string text, string value)
    {
        var item = Activator.CreateInstance(typeof(TItem));

        var textProperty = item.GetType().GetProperty("Text");
        var valueProperty = item.GetType().GetProperty("value");

        if (textProperty != null)
        {
            textProperty.SetValue(item, text);
        }

        if (valueProperty != null)
        {
            valueProperty.SetValue(item, value);
        }

        return (TItem)item;
    }
}
