using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api.Abstraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace E.Standard.WebGIS.Tools;

public class PluginManager
{
    public PluginManager(string rootPath)
    {
        if (String.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("PluginManger: rootPath is empty");
        }

        try
        {
            if (this.ApiButtons == null)
            {
                this.ApiButtons = new List<IApiButton>();
                var dir = new DirectoryInfo(rootPath);
                if (dir.Exists)
                {
                    foreach (var dllFile in dir.GetFiles("*.dll"))
                    {
                        try
                        {
                            Assembly assembly = Assembly.LoadFrom(dllFile.FullName);

                            this.ApiButtons.AddRange(assembly.GetTypes()
                                .Where(t =>
                                {
                                    var exportAttribute = t.GetCustomAttribute<ExportAttribute>();

                                    return exportAttribute != null && exportAttribute.ExportType == typeof(IApiButton);
                                })
                                .Select(t => (IApiButton)Activator.CreateInstance(t))
                                .ToArray());
                        }
                        catch (Exception/* ex*/)
                        {
                            //string message = ex.Message.ToString();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (ex is System.Reflection.ReflectionTypeLoadException)
            {
                var typeLoadException = ex as ReflectionTypeLoadException;
                var loaderExceptions = typeLoadException.LoaderExceptions;

                StringBuilder sb = new StringBuilder();
                foreach (var loaderException in loaderExceptions)
                {
                    sb.Append(loaderException.Message + "\n");
                }

                throw new Exception(sb.ToString());
            }

            throw;
        }
    }

    private List<IApiButton> ApiButtons { get; set; }

    public List<Type> ApiButtonTypes
    {
        get
        {
            List<Type> types = new List<Type>();
            if (this.ApiButtons != null)
            {
                foreach (IApiButton item in this.ApiButtons)
                {
                    types.Add(item.GetType());
                }
            }
            return types;
        }
    }

    public IApiButton CreateApiButtonInstance(Type type)
    {
        return Activator.CreateInstance(type) as IApiButton;
    }
}
