using webgis.deploy.Extensions;
using webgis.deploy.Services;

#if DEBUG || DEBUG_INTERNAL
string workDirectory = @"C:\deploy\webgis";
#else
string workDirectory = Environment.CurrentDirectory;
#endif

// -p local -v latest --download-latest --deploy-portal --deploy-api

bool runAutomated = false,
     nonInteractive = false,
     noColor = false,
     verbose = false;

string profile = String.Empty,
       version = String.Empty;
#if !INTERNAL
bool downloadLatest = false;
#endif
bool deployCms = false,
     deployPortal = false,
     deployApi = false,
     portable = false;

try
{
    for (int i = 0; i < (args?.Length ?? 0); i++)
    {
        string NextArg() => i + 1 < args!.Length
            ? args[++i]
            : throw new ArgumentException($"Missing value for parameter {args[i]}");

        switch (args![i])
        {
            case "-h":
            case "--help":
                new ConsoleService(nonInteractive, noColor).WriteHelp();
                return 0;
            case "-p":
            case "--profile":
                profile = NextArg();
                runAutomated = true;
                break;
#if !INTERNAL
            case "-d":
            case "--download-latest":
                downloadLatest = true;
                break;
#endif
            case "-v":
            case "--version":
                version = NextArg();
                break;
            case "--cms":
            case "--deploy-cms":
                deployCms = true;
                break;
            case "--portal":
            case "--deploy-portal":
                deployPortal = true;
                break;
            case "--api":
            case "--deploy-api":
                deployApi = true;
                break;
            case "--portable":
                portable = true;
                break;
            case "--non-interactive":
                nonInteractive = true;
                break;
            case "--no-color":
                noColor = true;
                break;
            case "--verbose":
                verbose = true;
                break;
        }
    }
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

var consoleService = new ConsoleService(nonInteractive, noColor, verbose);
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

#if INTERNAL
const bool isInternal = true;
#else
const bool isInternal = false;
#endif

consoleService.WriteBanner(DeployVersionService.DeployToolVersion.ToString(), workDirectory, isInternal);

try
{
    var ioService = new IOService(consoleService);
    var repoService = new DeployRepositoryService(ioService, workDirectory);
    var versionService = new DeployVersionService(repoService, ioService, consoleService, portable);
    var cssService = new CssService(repoService, consoleService);
    var securityService = new SecurityService(repoService);

    consoleService.Quiet(securityService.Init);

    // 1. Profile

    if (String.IsNullOrEmpty(profile))
    {
        profile = consoleService.ChooseFrom(repoService.Profiles(), "profile", allowNewVales: true, examples: "production, staging, test").Trim();

        repoService.CreateProfile(profile);
    }

#if !INTERNAL
    // 2. Download

    if (!portable && (downloadLatest || consoleService.DoYouWant("to download the latest version from GitHub", defaultValue: consoleService.IsInteractive)))
    {
        consoleService.Section("Download");

        if (!await DownloadLatestAsync())
        {
            return 1;
        }
    }
#endif

    // 3. Version

    if (String.IsNullOrEmpty(version))
    {
        version = consoleService.ChooseFrom(versionService.GetVersions(), "version");
    }

    if ("latest".Equals(version, StringComparison.OrdinalIgnoreCase))
    {
        version = versionService.GetVersions().FirstOrDefault()
            ?? throw new Exception("No version found in your download folder");
    }

    if (String.IsNullOrEmpty(profile) ||
        String.IsNullOrEmpty(version))
    {
        consoleService.WriteHelp();
        return 1;
    }

    versionService.InitOverrides(profile, version);
    versionService.CreateDefaultModifyCss(profile);

    // 4. Confirm

    consoleService.WriteSummary(("Profile", profile), ("Version", version), ("Platform", webgis.deploy.Platform.PlatformName));
    if (!runAutomated && !consoleService.DoYouWantToContinue())
    {
        return 1;
    }

    // 5. Deploy settings

    var deployVersionModel = repoService.GetDeployModel(profile);
    if (consoleService.InputRequiredModelProperties(deployVersionModel))
    {
        repoService.SetDeployVersionModel(profile, deployVersionModel);
    }

    var webgisRepoPath = new DirectoryInfo(
        deployVersionModel.RepositoryPath.EndsWith("!")
            ? deployVersionModel.RepositoryPath.Substring(0, deployVersionModel.RepositoryPath.Length - 1)
            : deployVersionModel.RepositoryPath
        );

    if (!webgisRepoPath.Exists)
    {
        consoleService.Section("Repository");

        foreach (var app in new[] { "webgis-api", "webgis-portal", "webgis-cms" })
        {
            Extract($"Create repository ({app})", $"{app}/_setup/proto/_webgis-repository", deployVersionModel.RepositoryPath);
        }
    }

    var installationPath = deployVersionModel.ProfileTargetInstallationPath(profile, version);
    var targetExists = Directory.Exists(installationPath);

    // 6. Components

    if (!runAutomated)
    {
        var preselected = new List<string>() { "WebGIS API", "WebGIS Portal", "WebGIS CMS" };
        var selected = consoleService.ChooseMultiple(preselected, "components", preselected);

        deployApi = deployApi || selected.Contains("WebGIS API");
        deployPortal = deployPortal || selected.Contains("WebGIS Portal");
        deployCms = deployCms || selected.Contains("WebGIS CMS");
    }

    // 7. Extract

    consoleService.Section($"Deploy WebGIS {version}");

    if (!targetExists)
    {
        if (deployApi)
        {
            Extract("WebGIS API", "webgis-api", Path.Combine(installationPath, "webgis-api"));
        }

        if (deployPortal)
        {
            Extract("WebGIS Portal", "webgis-portal", Path.Combine(installationPath, "webgis-portal"));
        }

        if (deployCms)
        {
            Extract("WebGIS CMS", "webgis-cms", Path.Combine(installationPath, "webgis-cms"));
        }

        if (profile == "local" || profile == "local_internal")
        {
            Extract("WebGIS Scripts", "_scripts", Path.Combine(installationPath, "_scripts"));
            Extract("WebGIS Requirements", "_requirements", Path.Combine(installationPath, "_requirements"));

            consoleService.RunProgress("Root files",
                () => versionService.ExtractFiles(version, "", installationPath));
        }
    }
    else
    {
        consoleService.Warning("Version already deployed, files are not extracted again");
    }

    // 8. Keys, overrides, css

    consoleService.Section("Configure");

    securityService.CopyKeysConfigTo(Path.Combine(deployVersionModel.RepositoryPath, "security", "keys"));
    consoleService.Success("Security keys");

    foreach (var (deploy, app) in new[] { (deployApi, "webgis-api"), (deployPortal, "webgis-portal"), (deployCms, "webgis-cms") })
    {
        if (deploy)
        {
            List<string> overrideFiles = null;
            consoleService.RunProgress($"Overrides {app}",
                () => overrideFiles = versionService.CopyOverrides(profile, app, installationPath, deployVersionModel));

            if (overrideFiles?.Count > 0)
            {
                consoleService.WriteFileTree(app, overrideFiles);
            }
        }
    }

    string portalConfigPath = Path.Combine(installationPath, "webgis-portal", "_config", "portal.config");
    var company = portalConfigPath.GetXmlConfigValue("company");

    cssService.ModifyDefaultCss(profile, installationPath, company);
    cssService.ModifyPortalCss(profile, installationPath, company);
    cssService.ModifySiteCss(profile, installationPath);
    consoleService.Success("Custom CSS", $"({cssService.CreatedFiles} {(cssService.CreatedFiles == 1 ? "file" : "files")})");

    var components = new[] { (deployApi, "API"), (deployPortal, "Portal"), (deployCms, "CMS") }
        .Where(c => c.Item1)
        .Select(c => c.Item2);

    consoleService.WriteResult($"WebGIS {version} deployed", true,
        ("Profile", profile),
        ("Components", String.Join(", ", components)),
        ("Installation", installationPath),
        ("Repository", deployVersionModel.RepositoryPath),
        ("Duration", stopwatch.Elapsed.ToString(@"m\:ss") + " min"));

    void Extract(string description, string relativeSourcePath, string targetPath)
        => consoleService.RunProgress(description,
            () => versionService.ExtractZipFolderRecursive(version, relativeSourcePath, targetPath));

#if !INTERNAL
    async Task<bool> DownloadLatestAsync()
    {
        var githubReleaseService = new GitHubReleaseService("e-netze", "webgis");

        foreach (var url in await githubReleaseService.GetLatestReleaseDownloadUrlsAsync())
        {
            var fileName = url.Split('/').Last();

            if (versionService.Exits(fileName))
            {
                consoleService.Success($"Latest version {fileName}", "(already downloaded)");
                continue;
            }

            try
            {
                await consoleService.RunProgressAsync($"Download {fileName}", async () =>
                {
                    using var client = new HttpClient();
                    using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var total = response.Content.Headers.ContentLength ?? 0;
                    using var memory = new MemoryStream();
                    using var stream = await response.Content.ReadAsStreamAsync();

                    var buffer = new byte[81920];
                    int read;
                    while ((read = await stream.ReadAsync(buffer)) > 0)
                    {
                        memory.Write(buffer, 0, read);
                        consoleService.Report($"Download {fileName}", (int)(memory.Length / 1024 / 1024), (int)(total / 1024 / 1024));
                    }

                    await versionService.AppendAsync(fileName, memory.ToArray());
                }, unit: "MB");
            }
            catch (Exception ex)
            {
                consoleService.Error($"Exception on downloading file: {ex.Message}");
                return false;
            }
        }

        return true;
    }
#endif
}
catch (Exception ex)
{
    consoleService.WriteBlock($"Error: {ex.Message}", success: false);

    return 1;
}

if (!runAutomated && consoleService.IsInteractive)
{
    consoleService.WaitForEnter();
}

return 0;
