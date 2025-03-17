using System.Text.RegularExpressions;
using webgis.deploy.Reflection;

namespace webgis.deploy.Extensions;

static class ModelPropertyAttributeExtensions
{
    static public string GetDefaultValue(this ModelPropertyAttribute modelPropertyAttribute, object model)
    {
        var defaultValue = modelPropertyAttribute.DefaultValue;

        if (!String.IsNullOrEmpty(defaultValue))
        {
            var modelType = model.GetType();

            var matches = Regex.Matches(defaultValue, @"{([^{}]*)");
            var propertyNames = matches.Cast<Match>().Select(m => m.Groups[1].Value).Distinct();

            foreach (var propertyName in propertyNames)
            {
                defaultValue = defaultValue.Replace($"{{{propertyName}}}", modelType.GetProperty(propertyName)?.GetValue(model)?.ToString());
            }
        }

        return defaultValue;
    }
}
