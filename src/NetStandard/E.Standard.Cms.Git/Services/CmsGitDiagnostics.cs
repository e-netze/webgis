using LibGit2Sharp;

namespace E.Standard.Cms.Git.Services;

public static class CmsGitDiagnostics
{
    /// <summary>
    /// Loads the native libgit2 library. Throws, if the native library is missing for the current platform (e.g. Docker image without linux binaries)
    /// </summary>
    public static string LibGit2Version()
        => GlobalSettings.Version.ToString();
}