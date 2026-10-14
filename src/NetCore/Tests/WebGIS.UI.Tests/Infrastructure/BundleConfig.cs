using System.Text.Json;

namespace WebGIS.UI.Tests.Infrastructure;

/// <summary>
/// Reads the input files of a bundle from the Api project's bundleconfig.json,
/// so the tests always load the scripts in the same order as the production bundle.
/// </summary>
internal static class BundleConfig
{
    public const string ApiBundle = "wwwroot/scripts/api/api.min.js";
    public const string ApiUiBundle = "wwwroot/scripts/api/api-ui.min.js";
    public const string WebGISApiBundle = "wwwroot/scripts/api/webgis-api.min.js";

    /// <summary>
    /// Returns the bundle definition (the entry with the given outputFileName) as a JSON element.
    /// </summary>
    public static JsonElement GetBundle(string outputFileName)
    {
        foreach (var bundle in _document.Value.RootElement.EnumerateArray())
        {
            if (bundle.TryGetProperty("outputFileName", out var output)
                && string.Equals(output.GetString(), outputFileName, StringComparison.OrdinalIgnoreCase))
            {
                return bundle;
            }
        }

        throw new InvalidOperationException($"Bundle '{outputFileName}' not found in {RepoPaths.BundleConfig}");
    }

    /// <summary>
    /// Returns the outputFileNames in the order they are defined in bundleconfig.json.
    /// </summary>
    public static IReadOnlyList<string> GetOutputFileNames()
        => _document.Value.RootElement.EnumerateArray()
            .Where(b => b.TryGetProperty("outputFileName", out _))
            .Select(b => b.GetProperty("outputFileName").GetString()!)
            .ToList();

    private static readonly Lazy<JsonDocument> _document = new(() =>
        JsonDocument.Parse(
            File.ReadAllText(RepoPaths.BundleConfig),
            new JsonDocumentOptions
            {
                // bundleconfig.json contains // and /* */ comments
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }));

    /// <summary>
    /// Returns the input files of the bundle as web paths relative to wwwroot, e.g. "/scripts/api/webgis.js".
    /// </summary>
    public static IReadOnlyList<string> GetInputFiles(string outputFileName)
        => GetBundle(outputFileName)
            .GetProperty("inputFiles")
            .EnumerateArray()
            .Select(f => ToWebPath(f.GetString()!))
            .ToList();

    public static string ToWebPath(string bundlePath)
    {
        const string prefix = "wwwroot/";
        var path = bundlePath.Replace('\\', '/');
        if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            path = path[prefix.Length..];
        }
        return "/" + path.TrimStart('/');
    }
}
