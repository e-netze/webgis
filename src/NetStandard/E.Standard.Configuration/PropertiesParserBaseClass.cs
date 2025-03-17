using E.Standard.Security.App.Services.Abstraction;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Reflection;

namespace E.Standard.Configuration;

public class PropertiesParserBaseClass
{
    virtual public void Parse(IConfigValueParser parser)
    {
        foreach (var propertyInfo in this.GetType().GetProperties())
        {
            if (propertyInfo.GetCustomAttribute<JsonPropertyAttribute>() == null)
            {
                continue;
            }

            if (propertyInfo.PropertyType == typeof(string))
            {
                var propertyValue = propertyInfo.GetValue(this)?.ToString();

                if (!String.IsNullOrWhiteSpace(propertyValue))
                {
                    propertyInfo.SetValue(this, parser.Parse(propertyValue));
                }
            }
            else if (propertyInfo.PropertyType == typeof(string[]) && propertyInfo.GetValue(this) != null)
            {
                var propertyValues = (string[])propertyInfo.GetValue(this);

                propertyInfo.SetValue(this, propertyValues.Select(p => parser.Parse(p)).ToArray());
            }
        }
    }
}
