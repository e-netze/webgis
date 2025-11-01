// build.cs
using E.Standard.Platform;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Serilog;
using System;
using System.IO;
using System.IO.Compression;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main()
    {
        return Execute<Build>(x => x.Compile);
    }

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly string Configuration = "Release"; //IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Platform to build - win-x64/linux-x64")]
    readonly string Platform = SystemInfo.IsLinux ? "linux-x64" : "win-x64";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            /*
            DotNetTasks.DotNetClean(s => s
                .SetProject(RootDirectory / "src" / "webgis.sln")
                .SetConfiguration(Configuration)
                .SetRuntime(Platform)
                .SetVerbosity(DotNetVerbosity.quiet)
            );
            */
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            /*
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(RootDirectory / "src" / "NetCore" / "Web" / "Cms" / "webgis-cms.csproj")
                .SetRuntime(Platform)
            );
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(RootDirectory / "src" / "NetCore" / "Web" / "Api" / "webgis-api.csproj")
                .SetRuntime(Platform)
            );
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(RootDirectory / "src" / "NetCore" / "Web" / "Portal" / "webgis-portal.csproj")
                .SetRuntime(Platform)
            );
            DotNetTasks.DotNetRestore(s => s
                .SetProjectFile(RootDirectory / "src" / "NetCore" / "Console" / "cms.tools" / "cms.tools.csproj")
                .SetRuntime(Platform)
            );
            */
        });

    Target Compile => _ => _
        .Before(DeployCleanIt)
        .DependsOn(Restore)
        .Executes(() =>
        {
            Log.Information($"Compile WebGIS {Version} for platform {Platform}");

            Log.Information("Compile WebGIS CMS");

            bool isInternal = Configuration.Contains("Internal", StringComparison.OrdinalIgnoreCase);

            (RootDirectory / "publish" / Platform / "cms" / "artifacts").DeleteDirectory();
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "src" / "NetCore" / "Web" / "Cms" / "webgis-cms.csproj")
                .SetConfiguration(Configuration)
                .SetProperty("DeployOnBuild", "true")
                //.SetOutputDirectory(RootDirectory / "publish" / Platform / "cms" / "artifacts")
                .SetPublishProfile(isInternal ? $"{Platform}_internal" : Platform)
                .SetRuntime(Platform)
            //.EnableNoRestore()
            );

            Log.Information("Compile WebGIS API");

            (RootDirectory / "publish" / Platform / "api" / "artifacts").DeleteDirectory();
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "src" / "NetCore" / "Web" / "Api" / "webgis-api.csproj")
                .SetConfiguration(Configuration)
                .SetProperty("DeployOnBuild", "true")
                //.SetOutputDirectory(RootDirectory / "publish" / Platform / "api" / "artifacts")
                .SetPublishProfile(isInternal ? $"{Platform}_internal" : Platform)
                .SetRuntime(Platform)
            //.EnableNoRestore()
            );

            Log.Information("Compile WebGIS Portal");

            (RootDirectory / "publish" / Platform / "portal" / "artifacts").DeleteDirectory();
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "src" / "NetCore" / "Web" / "Portal" / "webgis-portal.csproj")
                .SetConfiguration(Configuration)
                .SetProperty("DeployOnBuild", "true")
                //.SetOutputDirectory(RootDirectory / "publish" / Platform / "portal" / "artifacts")
                .SetPublishProfile(isInternal ? $"{Platform}_internal" : Platform)
                .SetRuntime(Platform)
            //.EnableNoRestore()
            );

            Log.Information("Compile WebGIS Tools");

            (RootDirectory / "publish" / Platform / "tools" / "cms.tools" / "artifacts").DeleteDirectory();
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "src" / "NetCore" / "Console" / "cms.tools" / "cms.tools.csproj")
                .SetConfiguration(Configuration)
                .SetProperty("DeployOnBuild", "true")
                .SetOutputDirectory(RootDirectory / "publish" / Platform / "tools" / "cms.tools" / "artifacts")
                .SetPublishProfile(Platform)
            //.EnableNoRestore()
            );
        });

    Target DeployCleanIt => _ => _
        .Executes(() =>
        {
            var globs = new[]
            {
                "api/artifacts/_config/**/*.*",
                "api/artifacts/wwwroot/content/styles/e/**/*.*",

                "portal/artifacts/_config/**/*.*",
                "portal/artifacts/_templates/E-*.*",
                "portal/artifacts/mapbuilder/dev/**/*.*",
                "portal/artifacts/wwwroot/scripts/portals/dev/**/*.*",
                "portal/artifacts/wwwroot/content/portals/dev/**/*.*",
                "portal/artifacts/wwwroot/content/companies/e/**/*.*",
                "portal/artifacts/wwwroot/content/companies/e/backgrounds/**/*.*",

                "cms/artifacts/_config/**/*.*"
            };

            foreach (var pattern in globs)
            {
                foreach (var globFile in (RootDirectory / "publish" / Platform).GlobFiles(pattern))
                {
                    Log.Information($"Deleting {globFile}");
                    globFile.DeleteFile();
                }
            }

            Log.Information($"Set Version {Version} to webgis.js");
            var jsGlobs = new[]
            {
                "api/artifacts/wwwroot/scripts/api/webgis.js",
                "api/artifacts/wwwroot/scripts/api/api.min.js"
            };

            foreach (var pattern in jsGlobs)
            {
                foreach (var globFile in (RootDirectory / "publish" / Platform).GlobFiles(pattern))
                {
                    Log.Information($"Modifing {globFile}");
                    globFile.WriteAllText(globFile.ReadAllText()
                        .Replace("{{currentJavascriptVersion}}", Version),
                        System.Text.Encoding.UTF8);
                }
            }
        });

    [Parameter("Versions‑/Build‑Label")]
    readonly string Version = WebGISVersion.Version.ToString();

    AbsolutePath DeployRoot => (AbsolutePath)(SystemInfo.IsLinux ? "~/deploy/webgis" : @"c:\deploy\webgis");
    AbsolutePath DeployDir => DeployRoot / Version;
    AbsolutePath DownloadDir => DeployRoot / "download";

    Target Deploy => _ => _
        .DependsOn(Test)
        .DependsOn(Compile)
        .DependsOn(DeployCleanIt)
        .Executes(() =>
        {
            bool isInternal = Configuration.Contains("Internal", StringComparison.OrdinalIgnoreCase);

            Log.Information($"Deploy WebGIS {Version} for platform {Platform}");

            if (Platform.Equals("linux-x64", StringComparison.Ordinal))
            {
                Log.Information($"Builder docker images: {Version}");

                string platformDir = isInternal
                    ? @"R:\core\web\webgis7\linux64"
                    : RootDirectory / "publish" / Platform;
                string imagePostfix = isInternal ? "-internal" : "";

                ProcessTasks.StartProcess("docker",
                    $"build -t webgis-cms{imagePostfix}:{Version} -f Dockerfile .",
                    workingDirectory: Path.Combine(platformDir, "cms"),
                    logger: (oType, txt) =>
                    {
                        Log.Information($"{txt}");
                    })
                    .AssertZeroExitCode();
                ProcessTasks.StartProcess("docker",
                    $"build -t webgis-api{imagePostfix}:{Version} -f Dockerfile .",
                    workingDirectory: Path.Combine(platformDir, "api"),
                    logger: (oType, txt) =>
                    {
                        Log.Information($"{txt}");
                    })
                    .AssertZeroExitCode();
                ProcessTasks.StartProcess("docker",
                    $"build -t webgis-portal{imagePostfix}:{Version} -f Dockerfile .",
                    workingDirectory: Path.Combine(platformDir, "portal"),
                    logger: (oType, txt) =>
                    {
                        Log.Information($"{txt}");
                    })
                    .AssertZeroExitCode();

                // tag to latest
                ProcessTasks.StartProcess("docker",
                    $"tag webgis-cms{imagePostfix}:{Version} webgis-cms{imagePostfix}:latest",
                    logger: (oType, txt) =>
                    {
                        Log.Information($"{txt}");
                    })
                    .AssertZeroExitCode();
                ProcessTasks.StartProcess("docker",
                    $"tag webgis-api{imagePostfix}:{Version} webgis-api{imagePostfix}:latest",
                    logger: (oType, txt) =>
                    {
                        Log.Information($"{txt}");
                    })
                    .AssertZeroExitCode();
                ProcessTasks.StartProcess("docker",
                    $"tag webgis-portal{imagePostfix}:{Version} webgis-portal{imagePostfix}:latest",
                    logger: (oType, txt) =>
                    {
                        Log.Information($"{txt}");
                    })
                    .AssertZeroExitCode();
                return;
            }

            Log.Information($"Deploy ZIP File: {Version}");

            // 1. Mirror Source (ROBOMIRROR‑Äquivalent)
            Log.Information("Copy webgis-api");
            (DeployDir / Version).CreateOrCleanDirectory();
            (RootDirectory / "publish" / Platform / "api" / "artifacts").CopyToDirectory(
                DeployDir / Version,
                ExistsPolicy.MergeAndOverwrite);
            (DeployDir / Version / "artifacts").Rename("webgis-api");

            Log.Information("Copy webgis-portal");
            (RootDirectory / "publish" / Platform / "portal" / "artifacts").CopyToDirectory(
                DeployDir / Version,
                ExistsPolicy.MergeAndOverwrite);
            (DeployDir / Version / "artifacts").Rename("webgis-portal");

            Log.Information("Copy webgis-cms");
            (RootDirectory / "publish" / Platform / "cms" / "artifacts").CopyToDirectory(
                DeployDir / Version,
                ExistsPolicy.MergeAndOverwrite);
            (DeployDir / Version / "artifacts").Rename("webgis-cms");


            Log.Information("Copy webgis-tools");
            (DeployDir / Version / "webgis-tools").CreateOrCleanDirectory();
            (RootDirectory / "publish" / Platform / "tools" / "cms.tools" / "artifacts").CopyToDirectory(
                DeployDir / Version / "webgis-tools",
                ExistsPolicy.MergeAndOverwrite);
            (DeployDir / Version / "webgis-tools" / "artifacts").Rename("cms.tools");

            // 2. Overrides
            Log.Information("Copy api overrides");
            (RootDirectory / "publish" / Platform / "api" / "override" / "_setup").CopyToDirectory(
                                     DeployDir / Version / "webgis-api",
                                     ExistsPolicy.MergeAndOverwrite);
            Log.Information("Copy portal overrides");
            (RootDirectory / "publish" / Platform / "portal" / "override" / "_setup").CopyToDirectory(
                                     DeployDir / Version / "webgis-portal",
                                     ExistsPolicy.MergeAndOverwrite);
            Log.Information("Copy cms overrides");
            (RootDirectory / "publish" / Platform / "cms" / "override" / "_setup").CopyToDirectory(
                                     DeployDir / Version / "webgis-cms",
                                     ExistsPolicy.MergeAndOverwrite);

            // 3. Requirements & Scripts
            Log.Information("Copy _requirements");
            (DeployDir / Version / "_requirements").CreateOrCleanDirectory();
            (RootDirectory / "publish" / Platform / "_requirements").CopyToDirectory(
                DeployDir / Version,
                ExistsPolicy.MergeAndOverwrite);

            Log.Information("Copy _scripts");
            (DeployDir / Version / "_scripts").CreateOrCleanDirectory();
            (RootDirectory / "publish" / Platform / "_scripts").CopyToDirectory(
                DeployDir / Version,
                ExistsPolicy.MergeAndOverwrite);

            // 4. Start‑Batchfiles
            Log.Information("Copy start-webgis.bat");
            (RootDirectory / "publish" / Platform / "start-webgis.bat").CopyToDirectory(DeployDir / Version);
            Log.Information("Copy start-webgis-cms.bat");
            (RootDirectory / "publish" / Platform / "start-webgis-cms.bat").CopyToDirectory(DeployDir / Version);

            // 5. ZIP‑Archiv erstellen
            if (!Directory.Exists(DownloadDir))
            {
                DownloadDir.CreateDirectory();
            }

            var platform = Platform.Replace("-x64", "64");
            var configPostfix = Configuration.Contains("_") ? Configuration.Substring(Configuration.IndexOf("_")).ToLower() : "";
            var zipFile = DownloadDir / $"webgis{configPostfix}-{platform}-{Version}.zip";

            Log.Information($"Zip Directory: {DeployDir} => {zipFile}");
            if (File.Exists(zipFile))
            {
                File.Delete(zipFile);
            }
            //ZipFile.CreateFromDirectory(DeployDir, zipFile,
            //                            CompressionLevel.Fastest,
            //                            includeBaseDirectory: false);

            // ‑‑‑ Variante B: SevenZip (nur wenn du 7z bevorzugst)
            //SevenZip(x => x
            //    .SetCommand("a")              // add
            //    .AddArguments("-tzip")        // Format
            //    .SetArchiveFile(zipFile)
            //    .AddPath(DeployDir));

            // Starte 7z.exe mit Argumenten
            //ProcessTasks.StartProcess("7z",
            //    $"a -tzip \"{zipFile}\" \"{DeployDir}\\*\"")
            //    .AssertZeroExitCode();

            DeployDir.ZipTo(
                zipFile,
                compressionLevel: CompressionLevel.SmallestSize,
                fileMode: FileMode.CreateNew);

            // 6. temporäres Deploy‑Verzeichnis aufräumen (rmdir /s /q)
            DeployDir.DeleteDirectory();
        });

    Target Test => _ => _
        .Before(Compile)
        .Executes(() =>
        {
            Log.Information($"Run tests for WebGIS {Version} on platform {Platform}");

            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(RootDirectory / "src" / "NetStandard" / "E.Standard.CMS.Core.Test" / "E.Standard.CMS.Core.Test.csproj")
                .SetProcessWorkingDirectory(RootDirectory)
                .SetVerbosity(DotNetVerbosity.minimal)
            );
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(RootDirectory / "src" / "NetStandard" / "E.Standard.Extensions.Test" / "E.Standard.Extensions.Test.csproj")
                .SetProcessWorkingDirectory(RootDirectory)
            );
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(RootDirectory / "src" / "NetStandard" / "E.Standard.Json.Test" / "E.Standard.Json.Test.csproj")
                .SetProcessWorkingDirectory(RootDirectory)
            );
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(RootDirectory / "src" / "NetStandard" / "E.Standard.Localization.Test" / "E.Standard.Localization.Test.csproj")
                .SetProcessWorkingDirectory(RootDirectory)
            );
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(RootDirectory / "src" / "NetStandard" / "E.Standard.Web.Test" / "E.Standard.Web.Test.csproj")
                .SetProcessWorkingDirectory(RootDirectory)
            );
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(RootDirectory / "src" / "NetStandard" / "E.Standard.WebGIS.Tools.Tests" / "E.Standard.WebGIS.Tools.Tests.csproj")
                .SetProcessWorkingDirectory(RootDirectory)
            );
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(RootDirectory / "src" / "NetStandard" / "E.Standard.WebMapping.GeoServices.Tests" / "E.Standard.WebMapping.GeoServices.Tests.csproj")
                .SetProcessWorkingDirectory(RootDirectory)
            );
        });
}
