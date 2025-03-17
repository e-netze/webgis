using Api.Core.AppCode.Json.Converters;
using System.Text.Json;

namespace Api.Core.AppCode.Extensions;

static public class JsonSerializerOptionsExtensions
{
    static public void AddServerDefaults(this JsonSerializerOptions options)
    {
        if (options.Converters != null)
        {
            options.Converters.Add(new UIElementConverter());
            options.Converters.Add(new UISetterConverter());
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
