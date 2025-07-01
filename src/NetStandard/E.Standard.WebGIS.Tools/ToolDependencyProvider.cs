using E.Standard.DependencyInjection.Abstractions;
using E.Standard.Localization.Abstractions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using System;

namespace E.Standard.WebGIS.Tools;
internal class ToolDependencyProvider : IDependencyProvider
{
    private readonly IBridge _bridge;
    private readonly ApiToolEventArguments _arguments;
    private readonly ILocalizer _localizer;

    public ToolDependencyProvider(
            IBridge bride,
            ApiToolEventArguments arguments,
            ILocalizer localizer
        )
    {
        _bridge = bride;
        _arguments = arguments;
        _localizer = localizer;
    }

    public object GetDependency(Type dependencyType)
    {
        if (dependencyType == typeof(IBridge))
        {
            return _bridge;
        }

        if (dependencyType == typeof(ApiToolEventArguments))
        {
            return _arguments;
        }

        if (dependencyType.IsGenericType
            && dependencyType.GetGenericTypeDefinition() == typeof(ILocalizer<>))
        {
            Type genericArgument = dependencyType.GetGenericArguments()[0];

            // Use reflection to invoke CreateFor<T> with the genericArgument
            var createForMethod = typeof(ILocalizer)
                .GetMethod("CreateFor")
                .MakeGenericMethod(genericArgument);
            var localizerInstance = createForMethod.Invoke(_localizer, null);

            return localizerInstance;
        }

        throw new ArgumentException($"ToolDependencyProvider: Can't resolve dependency {dependencyType}");
    }
}
