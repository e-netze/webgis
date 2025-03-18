using E.Standard.Localization.Abstractions;
using Microsoft.Extensions.Options;

namespace E.Standard.Localization.Services;
internal class MarkdownLocalizerInitializer : IMarkdownLocationInitializer
{
    public MarkdownLocalizerInitializer(IOptions<MarkdownLocalizerOptions> options)
    {
        foreach (var language in options.Value.SupportedLanguages)
        {
            var localizer = new MarkdownLocalizer(language);
        }
    }
}
