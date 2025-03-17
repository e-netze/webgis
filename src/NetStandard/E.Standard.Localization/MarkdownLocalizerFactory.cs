using E.Standard.Localization;
using Microsoft.Extensions.Localization;
using System.Globalization;

// usage:
// services.AddScoped<IStringLocalizerFactory, MarkdownLocalizerFactory>();

class MarkdownLocalizerFactory : IStringLocalizerFactory
{
    private string _culture;
    public MarkdownLocalizerFactory(ICultureProvider cultureProvider)
    {
        _culture = cultureProvider.Culture;
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        //var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return new MarkdownLocalizer(_culture);
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        //var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return new MarkdownLocalizer(_culture);
    }
}
