using E.Standard.DependencyInjection.Abstractions;
using E.Standard.Localization.Abstractions;
using E.Standard.Localization.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using Microsoft.Extensions.Localization;
using System;

namespace E.Standard.Api.App;
public class ToolDependencyProvider : IDependencyProvider
{
    private readonly IBridge _bridge;
    private readonly ApiToolEventArguments _arguments;
    private readonly IStringLocalizer _stringLocalizer;

    public ToolDependencyProvider(
            IBridge bride,
            ApiToolEventArguments arguments,
            IStringLocalizer stringLocalizer
        )
    {
        _bridge = bride;
        _arguments = arguments;
        _stringLocalizer = stringLocalizer;
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
            Type localizerType = typeof(Localizer<>).MakeGenericType(genericArgument);

            return Activator.CreateInstance(localizerType, _stringLocalizer);
        }

        throw new ArgumentException($"ToolDependencyProvider: Can't resolve dependency {dependencyType}");
    }

    public static ILocalizer<T> GetLocalizer<T>(IStringLocalizer stringLocalizer)
    {
        return new Localizer<T>(stringLocalizer);
    }

    private class Localizer<T> : ILocalizer<T>
    {
        private readonly IStringLocalizer _stringLocalizer;
        private string _localizationNamespace;

        public Localizer(IStringLocalizer stringLocalizer)
        {
            _stringLocalizer = stringLocalizer;

            _localizationNamespace = typeof(T).GetLocalizationNamespace();
        }

        public string Localize(string key)
        {
            var val = _stringLocalizer[$"{_localizationNamespace}.{key}"];

            if (val.ResourceNotFound)  // fallback. Without namespace
            {
                val = _stringLocalizer[key];
            }

            return val.Value;
        }
    }
}
