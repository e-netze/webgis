using System;
using System.Collections.Generic;

namespace E.Standard.ActiveDirectory;

static public class ActiveDirectoryFactory
{
    static Dictionary<Type, Type> _implementations = new Dictionary<Type, Type>();

    static public void AddInterfaceImplementation<InterfaceType>(Type implementation)
    {
        _implementations[typeof(InterfaceType)] = implementation;
    }

    static public InterfaceType InterfaceImplementation<InterfaceType>(string initializationString = "")
    {
        if (!_implementations.ContainsKey(typeof(InterfaceType)))
        {
            throw new NotImplementedException(typeof(InterfaceType).ToString() + " is not implementet for your Environment");
        }

        var instance = (InterfaceType)Activator.CreateInstance(_implementations[typeof(InterfaceType)])!;

        if (instance is IInititalize)
        {
            ((IInititalize)instance).Initialize(initializationString);
        }

        return instance;
    }
}
