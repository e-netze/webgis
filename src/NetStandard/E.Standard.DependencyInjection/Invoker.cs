using E.Standard.DependencyInjection.Abstractions;
using System.Reflection;

namespace E.Standard.DependencyInjection;

public class Invoker
{
    public static TResult? Invoke<TResult>(
            object instance,
            string methodName,
            IDependencyProvider dependencyProvider
        )
        => Invoke<TResult>(instance, instance!.GetType().GetMethod(methodName)!, dependencyProvider);

    public static TResult? Invoke<TResult>(
            object instance,
            MethodInfo methodInfo,
            IDependencyProvider dependencyProvider
        )
    {
        return (TResult?)methodInfo.Invoke(
                        instance,
                        GetDependencies(methodInfo, dependencyProvider).ToArray()
                    );
    }

    public static Task<TResult?> InvokeAsync<TResult>(
            object instance,
            string methodName,
            IDependencyProvider dependencyProvider
        )
        => InvokeAsync<TResult>(instance, instance!.GetType().GetMethod(methodName)!, dependencyProvider);

    public static Task<TResult?> InvokeAsync<TResult>(
            object instance,
            MethodInfo methodInfo,
            IDependencyProvider dependencyProvider
        )
    {
        return (Task<TResult?>)methodInfo.Invoke(
                        instance,
                        GetDependencies(methodInfo, dependencyProvider).ToArray()
                    )!;
    }

    static public IEnumerable<object> GetDependencies(
            MethodInfo methodInfo,
            IDependencyProvider dependencyProvider)
    {
        foreach (var parameterInfo in methodInfo.GetParameters())
        {
            yield return dependencyProvider.GetDependency(parameterInfo.ParameterType);
        }
    }
}