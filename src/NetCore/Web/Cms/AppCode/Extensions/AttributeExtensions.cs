#nullable enable

using E.Standard.Localization.Abstractions;
using System;
using System.ComponentModel;

namespace Cms.AppCode.Extensions;

static internal class AttributeExtensions
{
    static public string? LocalizedDisplayName(
            this DisplayNameAttribute? attribute,
            ILocalizer localizer)
    {
        return attribute?.DisplayName switch
        {
            var dn when String.IsNullOrEmpty(dn) => String.Empty,
            var dn when dn!.StartsWith("#") => localizer.Localize(dn.Substring(1)),
            _ => attribute?.DisplayName
        };
    }

    static public string? LocalizedDescription(
            this DisplayNameAttribute? attribute,
            ILocalizer localizer)
    {
        return attribute?.DisplayName switch
        {
            var desc when desc!.StartsWith("#") => localizer.Localize($"{desc.Substring(1)}:body"),
            _ => null
        };
    }

    static public string? LocalizedCategory(
            this CategoryAttribute? attribute,
            ILocalizer localizer)
    {
        return attribute?.Category switch
        {
            var cat when String.IsNullOrEmpty(cat) => String.Empty,
            var cat when cat!.StartsWith("~~#") => $"~~{localizer.Localize(cat.Substring(3))}",
            var cat when cat!.StartsWith("~#") => $"~{localizer.Localize(cat.Substring(2))}",
            var cat when cat!.StartsWith("#") => localizer.Localize(cat.Substring(1)),
            _ => attribute?.Category
        };
    }
}
