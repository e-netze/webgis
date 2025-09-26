using E.Standard.CMS.Core.Extensions;
using E.Standard.Localization.Abstractions;
using E.Standard.Localization.Extensions;
using Microsoft.Extensions.Localization;
using System;

namespace E.Standard.CMS.Core;

public class CmsLocalizer<T> : ILocalizer<T>
{
    private readonly IStringLocalizer _stringLocalizer;
    private string _localizationNamespace;

    public CmsLocalizer(IStringLocalizer stringLocalizer)
    {
        _stringLocalizer = stringLocalizer;

        _localizationNamespace = typeof(T).GetLocalizationNamespace();
    }

    public ILocalizer<TClass> CreateFor<TClass>()
    {
        return new CmsLocalizer<TClass>(_stringLocalizer);
    }

    public string Localize(string key, ILocalizer.LocalizeMode mode = ILocalizer.LocalizeMode.NamespaceWithFallbackToKey, ILocalizer.LocalizerDefaultValue defaultValue = ILocalizer.LocalizerDefaultValue.OriginalKey)
    => mode switch
    {
        ILocalizer.LocalizeMode.NamespaceWithFallbackToKey => LocalizeNamespaceWithFallbackToKey(key, defaultValue),
        ILocalizer.LocalizeMode.NamespaceOnly => LocalizeNamespaceOnly(key, defaultValue),
        ILocalizer.LocalizeMode.ExcactKeyOnly => LocalizeExcactKeyOnly(key, defaultValue),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    private string LocalizeNamespaceWithFallbackToKey(string key, ILocalizer.LocalizerDefaultValue defaultValue = ILocalizer.LocalizerDefaultValue.OriginalKey)
    {
        var val = _stringLocalizer[$"{_localizationNamespace}.{key}"];

        if (val.ResourceNotFound)  // fallback. Without namespace
        {
            val = _stringLocalizer[key];
        }

        return val.GetValue(defaultValue);
    }

    private string LocalizeNamespaceOnly(string key, ILocalizer.LocalizerDefaultValue defaultValue = ILocalizer.LocalizerDefaultValue.OriginalKey)
    {
        var val = _stringLocalizer[$"{_localizationNamespace}.{key}"];
        return val.GetValue(defaultValue);
    }

    private string LocalizeExcactKeyOnly(string key, ILocalizer.LocalizerDefaultValue defaultValue = ILocalizer.LocalizerDefaultValue.OriginalKey)
    {
        var val = _stringLocalizer[key];
        return val.GetValue(defaultValue);
    }
}
