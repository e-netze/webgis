using E.Standard.Api.App.Configuration;
using E.Standard.Configuration.Services;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api.Abstraction;
using System;
using System.Collections.Generic;
using System.Reflection;


namespace Api.Core.AppCode.Extensions;

static internal class ToolExtensions
{
    static public Dictionary<string, string> ToolConfiguration(this IApiButton button, ConfigurationService configService)
    {
        Dictionary<string, string> toolConfig = null;

        var toolConfigAttribute = button?.GetType().GetCustomAttribute<ToolConfigurationSectionAttribute>();
        if (toolConfigAttribute != null && !String.IsNullOrWhiteSpace(toolConfigAttribute.SectionName))
        {
            toolConfig = new Dictionary<string, string>();
            foreach (var key in configService.GetPathsStartWith(ApiConfigKeys.ToKey($"tool-{toolConfigAttribute.SectionName}:")))
            {
                toolConfig.Add(key.Substring(key.LastIndexOf(":") + 1), configService[key]);
            }
        }

        return toolConfig;
    }
}
