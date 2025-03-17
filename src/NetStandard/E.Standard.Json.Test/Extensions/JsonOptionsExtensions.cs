using Api.Core.AppCode.Json.Converters;
using E.Standard.Json.Test.Converters;
using System.Text.Json;

namespace E.Standard.Json.Test.Extensions;

static public class JsonSerializerOptionsExtensions
{
    static public void AddServerDefaults(this JsonSerializerOptions options)
    {
        if (options.Converters != null)
        {
            options.Converters.Add(new UIElementConverter());
            options.Converters.Add(new UISetterConverter());
            options.Converters.Add(new DoubleConverter());
            options.Converters.Add(new FloatConverter());
        }
    }

    static public void AddServerDefaults(this JsonSerializerOptions[] optionsCollection)
    {
        foreach (var options in optionsCollection)
        {
            options.AddServerDefaults();
        }
    }
}
