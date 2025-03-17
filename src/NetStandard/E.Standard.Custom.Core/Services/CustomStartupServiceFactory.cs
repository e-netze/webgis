using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace E.Standard.Custom.Core.Services;

public class CustomStartupServiceFactory
{
    public static IEnumerable<ICustomStartupService> LoadCustomStartupServices(WebGISAppliationTarget target)
    {
        List<ICustomStartupService> customStartupServices = new List<ICustomStartupService>();

        try
        {
            var assemblyFileInfo = new FileInfo($"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/E.Standard.Custom.{target}.dll");
            if (assemblyFileInfo.Exists)
            {
                var assembly = Assembly.LoadFrom(assemblyFileInfo.FullName);

                customStartupServices.AddRange(assembly.GetTypes().Where(t => typeof(ICustomStartupService).IsAssignableFrom(t))
                                                                  .Where(t =>
                                                                  {
                                                                      var attribute = t.GetCustomAttribute<CustomServiceAttribute>();
                                                                      return attribute != null && attribute.Target.HasFlag(target);
                                                                  })
                                                                  .Select(t => (ICustomStartupService)Activator.CreateInstance(t)));
            }
        }
        catch { }

        return customStartupServices.ToArray();
    }
}
