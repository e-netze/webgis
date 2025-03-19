using E.Standard.Localization.Abstractions;
using E.Standard.Localization.Services;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

// usage:
// services.AddScoped<IStringLocalizerFactory, MarkdownLocalizerFactory>();

class MarkdownLocalizerFactory : IStringLocalizerFactory
{
    private string _culture;
    private MarkdownLocalizerOptions _options;

    public MarkdownLocalizerFactory(
                ICultureProvider cultureProvider,
                IOptions<MarkdownLocalizerOptions> options)
    {
        _options = options.Value;

        _culture = _options.SupportedLanguages.Contains(cultureProvider.Culture)
                        ? cultureProvider.Culture
                        : _options.DefaultLanguage;
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
