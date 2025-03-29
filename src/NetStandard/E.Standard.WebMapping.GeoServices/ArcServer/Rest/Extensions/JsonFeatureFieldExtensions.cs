#nullable enable

using E.Standard.Platform;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using System;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

static internal class JsonFeatureFieldExtensions
{
    static public object? TypedValueOrDefault(this JsonFeatureField field, string value)
    {
        // "esriFieldTypeBlob" | "esriFieldTypeDate" | "esriFieldTypeDouble" | "esriFieldTypeGeometry" | "esriFieldTypeGlobalID" | "esriFieldTypeGUID" | 
        // "esriFieldTypeInteger" | "esriFieldTypeOID" | "esriFieldTypeRaster" | "esriFieldTypeSingle" | "esriFieldTypeSmallInteger" | "esriFieldTypeString" | "esriFieldTypeXML"

        object? typedValue = field.Type switch
        {
            "esriFieldTypeDouble"
                => field.ValueTypeOrDefault(value, (v) => v.ToPlatformDouble()),
            "esriFieldTypeSingle"
                => field.ValueTypeOrDefault(value, (v) => v.ToPlatformFloat()),
            "esriFieldTypeSmallInteger"
                => field.ValueTypeOrDefault(value, (v) => Convert.ToInt16(v.Replace(",", "."))),
            "esriFieldTypeInteger"
                => field.ValueTypeOrDefault(value, (v) => Convert.ToInt32(value.Replace(",", "."))),
            _ => value
        };

        return typedValue;
    }

    static private object? ValueTypeOrDefault<T>(
                this JsonFeatureField field,
                string value,
                Func<string, T> func)
    {
        if (String.IsNullOrEmpty(value))
        {
            if (field.Nullable == true)
            {
                return null;
            }

            return default(T);
            //throw new Exception($"Field {field.Name} can not be NULL!");
        }

        return func(value);
    }
}
