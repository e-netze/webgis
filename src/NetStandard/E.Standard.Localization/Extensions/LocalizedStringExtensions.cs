using E.Standard.Localization.Abstractions;
using Microsoft.Extensions.Localization;

namespace E.Standard.Localization.Extensions;

public static class LocalizedStringExtensions
{
    public static string? GetValue(this LocalizedString localizedString, ILocalizer.LocalizerDefaultValue defaultValue)
    {
        if (localizedString.ResourceNotFound)
        {
            return defaultValue switch
            {
                ILocalizer.LocalizerDefaultValue.OriginalKey => localizedString.Value,
                ILocalizer.LocalizerDefaultValue.Null => null,
                ILocalizer.LocalizerDefaultValue.EmptyString => string.Empty,
                _ => throw new ArgumentOutOfRangeException(nameof(defaultValue), defaultValue, null)
            };
        }
        return localizedString.Value;
    }
}
