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

        try
        {
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
        catch (FormatException ex)
        {
            var type = field.Type switch
            {
                "esriFieldTypeDouble" => typeof(double),
                "esriFieldTypeSingle" => typeof(float),
                "esriFieldTypeSmallInteger" => typeof(short),
                "esriFieldTypeInteger" => typeof(int),
                _ => typeof(object)
            };
            throw new Exception($"Can't format input '{value}' to a correct {type.Name}!", ex);
        }
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
