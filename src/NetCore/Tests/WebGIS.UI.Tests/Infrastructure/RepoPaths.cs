namespace WebGIS.UI.Tests.Infrastructure;

/// <summary>
/// Resolves well-known repository paths by walking up from the test output directory
/// until the folder containing "src\webgis.sln" is found.
/// </summary>
internal static class RepoPaths
{
    private static readonly Lazy<string> _root = new(FindRepoRoot);

    public static string Root => _root.Value;

    public static string ApiProject => Path.Combine(Root, "src", "NetCore", "Web", "Api");

    public static string WwwRoot => Path.Combine(ApiProject, "wwwroot");

    public static string BundleConfig => Path.Combine(ApiProject, "bundleconfig.json");

    /// <summary>The test project's source directory (snapshots are committed here).</summary>
    public static string TestProject => Path.Combine(Root, "src", "NetCore", "Tests", "WebGIS.UI.Tests");

    public static string Snapshots => Path.Combine(TestProject, "Snapshots");

    public static string HarnessDirectory => Path.Combine(AppContext.BaseDirectory, "Harness");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "webgis.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Repository root (containing src\\webgis.sln) not found above '{AppContext.BaseDirectory}'.");
    }
}
