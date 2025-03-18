namespace E.Standard.Localization.Services;
public class MarkdownLocalizerOptions
{
    public string DefaultLanguage { get; set; } = "en";

    public string[] SupportedLanguages { get; set; } = [];
}
