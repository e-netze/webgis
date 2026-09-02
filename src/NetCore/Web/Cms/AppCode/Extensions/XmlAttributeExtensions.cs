#nullable enable

using System;

namespace webgis_cms.AppCode.Extensions;

static internal class XmlAttributeExtensions
{
    static public string? LocalizeAttribute(
            this System.Xml.XmlAttribute? attribute,
            E.Standard.Localization.Abstractions.ILocalizer localizer)
    {
        return attribute?.Value switch
        {
            var val when String.IsNullOrEmpty(val) => String.Empty,
            _ => localizer.Localize(attribute!.Value)
        };
    }
}
