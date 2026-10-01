namespace E.Standard.Localization.Test;

public class MarkdownLocalizerTests
{
    private readonly string _testDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestResources");

    public MarkdownLocalizerTests()
    {
        SetupTestFiles();
    }

    private void SetupTestFiles()
    {
        foreach (var languages in new string[] { "de", "en" })
        {
            if (!Directory.Exists(Path.Combine(_testDirectory, languages)))
            {
                Directory.CreateDirectory(Path.Combine(_testDirectory, languages));
            }
        }

        File.WriteAllText(Path.Combine(_testDirectory, "de", "tools.mapmarkup.md"), @"
# name: MapMarkup
# tools: Werkzeuge
## drawline: Linie Zeichnen
### note1: Hinweis
Zeichnen Sie bitte eine Linie mit mindestens zwei Stützpunkten.

### note2: Hinweis
Die Linie darf sich nicht selbst überschneiden.
        ");

        File.WriteAllText(Path.Combine(_testDirectory, "en", "tools.mapmarkup.md"), @"
# name: Map-Markup
# tools: Tools
## drawline: Draw Line
###note1: Notice
Please draw a line with at least two support points.

###note2: Notice
The line must not intersect itself.
        ");
    }

    private MarkdownLocalizer CreateLocalizer(string language)
    {
        return new MarkdownLocalizer(language, _testDirectory);
    }

    [Fact]
    public void GetHeader_ReturnsCorrectHeader()
    {
        var localizer = CreateLocalizer("de");

        Assert.Equal("Linie Zeichnen", localizer["tools.mapmarkup.tools.drawline"].Value);
        Assert.Equal("Hinweis", localizer["tools.mapmarkup.tools.drawline.note1"].Value);
    }

    [Fact]
    public void GetBody_ReturnsCorrectBody()
    {
        var localizer = CreateLocalizer("de");

        Assert.Equal("Zeichnen Sie bitte eine Linie mit mindestens zwei Stützpunkten.",
            localizer["tools.mapmarkup.tools.drawline.note1:body"].Value);

        Assert.Equal("Die Linie darf sich nicht selbst überschneiden.",
            localizer["tools.mapmarkup.tools.drawline.note2:body"].Value);
    }

    [Fact]
    public void FallbackToKey_WhenKeyNotFound()
    {
        var localizer = CreateLocalizer("de");

        Assert.Equal("nonexistent.key", localizer["nonexistent.key"].Value);
        Assert.Equal("nonexistent.key:body", localizer["nonexistent.key:body"].Value);
    }

    [Fact]
    public void EnglishLocalization_ReturnsCorrectValues()
    {
        var localizer = CreateLocalizer("en");

        Assert.Equal("Map-Markup", localizer["tools.mapmarkup.name"].Value);
        Assert.Equal("Draw Line", localizer["tools.mapmarkup.tools.drawline"].Value);
        Assert.Equal("Notice", localizer["tools.mapmarkup.tools.drawline.note1"].Value);
        Assert.Equal("Please draw a line with at least two support points.",
            localizer["tools.mapmarkup.tools.drawline.note1:body"].Value);
    }

    [Fact]
    public void SupportsDifferentLanguages()
    {
        var localizerDe = CreateLocalizer("de");
        var localizerEn = CreateLocalizer("en");

        Assert.NotEqual(localizerDe["tools.mapmarkup.tools.drawline"].Value,
                        localizerEn["tools.mapmarkup.tools.drawline"].Value);

        Assert.NotEqual(localizerDe["tools.mapmarkup.tools.drawline.note1:body"].Value,
                        localizerEn["tools.mapmarkup.tools.drawline.note1:body"].Value);
    }

    [Fact]
    public void OverrideTranslations_ReplaceAndExtendBaseTranslations()
    {
        var overridePath = CreateOverrideRoot("de",
            """
            # tools: Override tools
            ## drawline: Override draw line
            ### note1: Override note
            Override body.
            ## added: Added translation
            """);

        var localizer = new MarkdownLocalizer("de", _testDirectory, overridePath);

        Assert.Equal("Override draw line", localizer["tools.mapmarkup.tools.drawline"].Value);
        Assert.Equal("Override note", localizer["tools.mapmarkup.tools.drawline.note1"].Value);
        Assert.Equal("Override body.", localizer["tools.mapmarkup.tools.drawline.note1:body"].Value);
        Assert.Equal("Added translation", localizer["tools.mapmarkup.tools.added"].Value);
        Assert.Equal("MapMarkup", localizer["tools.mapmarkup.name"].Value);
        Assert.Equal("Die Linie darf sich nicht selbst überschneiden.",
            localizer["tools.mapmarkup.tools.drawline.note2:body"].Value);
    }

    [Fact]
    public void TranslationCache_IsScopedToLanguageAndPaths()
    {
        var firstResourcePath = CreateResourceRoot("de", "Base one");
        var secondResourcePath = CreateResourceRoot("de", "Base two");
        var overridePath = CreateOverrideRoot("de", "# tools: Tools\n## drawline: Override DE");
        WriteTranslationFile(overridePath, "en", "# tools: Tools\n## drawline: Override EN");

        var firstBaseLocalizer = new MarkdownLocalizer("de", firstResourcePath);
        var secondBaseLocalizer = new MarkdownLocalizer("de", secondResourcePath);
        var overrideDe = new MarkdownLocalizer("de", _testDirectory, overridePath);
        var overrideEn = new MarkdownLocalizer("en", _testDirectory, overridePath);

        Assert.Equal("Base one", firstBaseLocalizer["tools.mapmarkup.tools.drawline"].Value);
        Assert.Equal("Base two", secondBaseLocalizer["tools.mapmarkup.tools.drawline"].Value);
        Assert.Equal("Override DE", overrideDe["tools.mapmarkup.tools.drawline"].Value);
        Assert.Equal("Override EN", overrideEn["tools.mapmarkup.tools.drawline"].Value);
    }

    [Fact]
    public void MissingOverrideDirectory_UsesBaseTranslations()
    {
        var missingOverridePath = Path.Combine(_testDirectory, $"missing-override-{Guid.NewGuid():N}");

        var localizer = new MarkdownLocalizer("de", _testDirectory, missingOverridePath);

        Assert.Equal("Linie Zeichnen", localizer["tools.mapmarkup.tools.drawline"].Value);
    }

    private string CreateOverrideRoot(string language, string content)
    {
        var overridePath = Path.Combine(_testDirectory, $"override-{Guid.NewGuid():N}");
        WriteTranslationFile(overridePath, language, content);
        return overridePath;
    }

    private string CreateResourceRoot(string language, string drawLineHeader)
    {
        var resourcePath = Path.Combine(_testDirectory, $"resources-{Guid.NewGuid():N}");
        WriteTranslationFile(resourcePath, language, $"# tools: Tools\n## drawline: {drawLineHeader}");
        return resourcePath;
    }

    private static void WriteTranslationFile(string rootPath, string language, string content)
    {
        var languagePath = Path.Combine(rootPath, language);
        Directory.CreateDirectory(languagePath);
        File.WriteAllText(Path.Combine(languagePath, "tools.mapmarkup.md"), content);
    }
}
