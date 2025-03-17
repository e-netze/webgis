using E.Standard.Extensions.Compare;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

internal static class RasterAttributeTableExtensions
{
    static public string TranslateValue(this JsonRasterAttributeTable rasterAttributeTable, string value)
    {
        var classField = rasterAttributeTable.ClassField();

        if (String.IsNullOrEmpty(classField))
        {
            return value;
        }

        var feature = rasterAttributeTable?.Features?
                    .Where(f =>
                    {
                        var attriutes = (IDictionary<string, object>)f.Attributes;

                        return attriutes != null &&
                               attriutes.ContainsKey("Value") &&
                               value == attriutes["Value"]?.ToString();
                    })
                    .FirstOrDefault();

        if (feature == null)
        {
            return value;
        }

        var attributes = (IDictionary<string, object>)feature.Attributes;
        if (!attributes.ContainsKey(classField))
        {
            return value;
        }

        return attributes[classField]?.ToString().OrTake(value);
    }

    static public string ClassField(this JsonRasterAttributeTable rasterAttributeTable)
    {
        if (rasterAttributeTable?.Fields == null)
        {
            return null;
        }

        var firstStringField = rasterAttributeTable.Fields
                                                   .Where(f => "esriFieldTypeString".Equals(f.Type, StringComparison.OrdinalIgnoreCase))
                                                   .FirstOrDefault();

        if (firstStringField != null)
        {
            return firstStringField.Name;
        }

        var firstClassField = rasterAttributeTable.Fields
                                                   .Where(f => f.Name != null && f.Name.StartsWith("Class", StringComparison.OrdinalIgnoreCase))
                                                   .FirstOrDefault();

        return firstClassField?.Name;
    }
}
