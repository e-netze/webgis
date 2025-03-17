using Microsoft.Extensions.Localization;
using System.Globalization;

// usage:
// services.AddSingleton<IStringLocalizerFactory, MarkdownLocalizerFactory>();

public class MarkdownLocalizerFactory : IStringLocalizerFactory
{
    public IStringLocalizer Create(Type resourceSource)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return new MarkdownLocalizer(culture);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return new MarkdownLocalizer(culture);
    }
}
