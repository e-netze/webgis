using System.Text.Encodings.Web;
using System.Text.Json;

namespace WebGIS.UI.Tests.Infrastructure;

/// <summary>
/// Golden-master snapshots stored in the test project source folder:
/// Snapshots/&lt;type&gt;/&lt;variant&gt;.snap.json
///
/// - missing snapshot      => written, test is Inconclusive
/// - WEBGIS_UPDATE_SNAPSHOTS=1 => snapshot is overwritten, test is Inconclusive
/// - mismatch              => &lt;variant&gt;.received.snap.json is written next to it, test fails
/// </summary>
internal static class Snapshot
{
    public const string UpdateEnvironmentVariable = "WEBGIS_UPDATE_SNAPSHOTS";

    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static bool UpdateMode =>
        Environment.GetEnvironmentVariable(UpdateEnvironmentVariable) == "1";

    public static string Format(JsonElement element)
        => JsonSerializer.Serialize(element, _writeOptions).ReplaceLineEndings("\n") + "\n";

    public static void AssertMatches(string type, string variant, JsonElement actual)
    {
        ValidateName(type);
        ValidateName(variant);

        var directory = Path.Combine(RepoPaths.Snapshots, type);
        var snapshotPath = Path.Combine(directory, $"{variant}.snap.json");
        var receivedPath = Path.Combine(directory, $"{variant}.received.snap.json");
        var actualText = Format(actual);

        Directory.CreateDirectory(directory);

        if (!File.Exists(snapshotPath) || UpdateMode)
        {
            var existed = File.Exists(snapshotPath);
            File.WriteAllText(snapshotPath, actualText);
            DeleteIfExists(receivedPath);
            Assert.Inconclusive(
                $"Snapshot {(existed ? "updated" : "created")}: {snapshotPath}. Review it and re-run the test.");
        }

        var expectedText = File.ReadAllText(snapshotPath).ReplaceLineEndings("\n");
        if (expectedText == actualText)
        {
            DeleteIfExists(receivedPath);
            return;
        }

        File.WriteAllText(receivedPath, actualText);
        Assert.Fail(
            $"Snapshot mismatch for {type}/{variant}.{Environment.NewLine}" +
            $"Expected: {snapshotPath}{Environment.NewLine}" +
            $"Received: {receivedPath}{Environment.NewLine}" +
            FirstDifference(expectedText, actualText));
    }

    private static string FirstDifference(string expected, string actual)
    {
        var e = expected.Split('\n');
        var a = actual.Split('\n');
        for (int i = 0; i < Math.Max(e.Length, a.Length); i++)
        {
            var el = i < e.Length ? e[i] : "<eof>";
            var al = i < a.Length ? a[i] : "<eof>";
            if (el != al)
            {
                return $"First difference at line {i + 1}:{Environment.NewLine}" +
                       $"  expected: {el.Trim()}{Environment.NewLine}" +
                       $"  received: {al.Trim()}";
            }
        }
        return string.Empty;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException($"Invalid snapshot name '{name}'");
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
