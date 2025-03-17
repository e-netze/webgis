using System;
using System.Collections.Concurrent;

namespace E.Standard.WebGIS.Core.Services;

public static class ServiceHub
{
    static private ConcurrentDictionary<Type, object> _services = new ConcurrentDictionary<Type, object>();

    static public void AddService<T>(object instance) where T : IHubService
    {
        _services[typeof(T)] = instance;
    }

    static public T GetService<T>() where T : IHubService
    {
        var type = typeof(T);
        if (!_services.ContainsKey(type))
        {
            throw new NotImplementedException(type.ToString() + " is implementet in this environment");
        }

        var instance = (T)_services[type];

        if (instance.Singleton == true)
        {
            return (T)Activator.CreateInstance(instance.GetType());
        }

        return instance;
    }
}
