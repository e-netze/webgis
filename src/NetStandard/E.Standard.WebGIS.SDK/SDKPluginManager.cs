using E.Standard.WebGIS.SDK.DataLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace E.WebGIS.SDK;

public class SDKPluginManager
{
    public SDKPluginManager(string rootPath)
    {
        this.RootPath = rootPath;

        FindIntegratorEndpoints();
    }

    internal SDKPluginManager() { }

    public string RootPath { get; private set; }

    private void FindIntegratorEndpoints()
    {
        try
        {
            if (this.DataLinqEndpoints == null)
            {
                var dir = new DirectoryInfo(this.RootPath);
                if (dir.Exists)
                {
                    foreach (var dllFile in new DirectoryInfo(this.RootPath).GetFiles("*.dll"))
                    {
                        try
                        {
                            Assembly assembly = Assembly.LoadFrom(dllFile.FullName);

                            var endpoints = new List<IDataLinqEndpoint>();
                            endpoints.AddRange(assembly.GetTypes()
                                .Where(t => typeof(IDataLinqEndpoint).IsAssignableFrom(t))
                                .Select(t => (IDataLinqEndpoint)Activator.CreateInstance(t))
                                .ToArray());

                            this.DataLinqEndpoints = endpoints;
                        }
                        catch { }
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

    public IEnumerable<IDataLinqEndpoint> DataLinqEndpoints { get; private set; }

    public T GetPlugin<T>(string typeName)
    {
        if (typeof(T) == typeof(IDataLinqEndpoint))
        {
            return (T)DataLinqEndpoints.Where(i => i.GetType().ToString().ToLower() == typeName.ToLower()).FirstOrDefault();
        }

        return default(T);
    }
    public T CreatePluginInstance<T>(string typeName)
    {
        var plugin = GetPlugin<T>(typeName);
        if (plugin == null)
        {
            return default(T);
        }

        return (T)Activator.CreateInstance(plugin.GetType());
    }
}
