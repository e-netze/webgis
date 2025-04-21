// build.cs
using E.Standard.WebGIS.Core;
using NuGet.Common;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using System;
using System.IO;
using System.IO.Compression;
using static Nuke.Common.IO.PathConstruction;

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
    readonly Configuration Configuration = Configuration.Release; //IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Platform to build - Default is 'win-x64'")]
    readonly string Platform = "win-x64";

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
        .Before(PackageCleanIt)
        .DependsOn(Restore)
        .Executes(() =>
        {
            
            Log.Information("Build WebGIS CMS");

            (RootDirectory / "publish" / Platform / "cms" / "artifacts").DeleteDirectory();
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "src" / "NetCore" / "Web" / "Cms" / "webgis-cms.csproj")
                .SetConfiguration(Configuration)
                .SetProperty("DeployOnBuild", "true")
                //.SetOutputDirectory(RootDirectory / "publish" / Platform / "cms" / "artifacts")
                .SetPublishProfile(Platform)
                .SetRuntime(Platform)
            //.EnableNoRestore()
            );
            
            Log.Information("Build WebGIS API");

            (RootDirectory / "publish" / Platform / "api" / "artifacts").DeleteDirectory();
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "src" / "NetCore" / "Web" / "Api" / "webgis-api.csproj")
                .SetConfiguration(Configuration)
                .SetProperty("DeployOnBuild", "true")
                //.SetOutputDirectory(RootDirectory / "publish" / Platform / "api" / "artifacts")
                .SetPublishProfile(Platform)
                .SetRuntime(Platform)
            //.EnableNoRestore()
            );

            Log.Information("Build WebGIS Portal");

            (RootDirectory / "publish" / Platform / "portal" / "artifacts").DeleteDirectory();
            DotNetTasks.DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "src" / "NetCore" / "Web" / "Portal" / "webgis-portal.csproj")
                .SetConfiguration(Configuration)
                .SetProperty("DeployOnBuild", "true")
                //.SetOutputDirectory(RootDirectory / "publish" / Platform / "portal" / "artifacts")
                .SetPublishProfile(Platform)
                .SetRuntime(Platform)
            //.EnableNoRestore()
            );
            
            Log.Information("Build WebGIS Tools");

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

    Target PackageCleanIt => _ => _
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
                foreach(var globFile in (RootDirectory / "publish" / Platform).GlobFiles(pattern))
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

    AbsolutePath DeployRoot => (AbsolutePath)@"c:\deploy\webgis";
    AbsolutePath DeployDir => DeployRoot / Version;
    AbsolutePath DownloadDir => DeployRoot / "download";

    Target Package => _ => _
        //.DependsOn(Test)
        //.DependsOn(Compile)
        .DependsOn(PackageCleanIt)
        .Executes(() =>
        {
            if(Platform.Equals("linux-x64", StringComparison.Ordinal))
            {
                Log.Information($"Builder docker images: {Version}");

                ProcessTasks.StartProcess(
                    (RootDirectory / "publish" / Platform / "docker" / "build-images.bat").ToString(),
                    arguments: Version,
                    workingDirectory: (RootDirectory / "publish" / Platform / "docker").ToString(),
                    logger: (oType, txt) =>
                    {
                        Log.Information($"{txt}");
                    })
                   .AssertZeroExitCode();

                return;
            } 
            
            Log.Information($"Package ZIP File: {Version}");

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
            (DeployDir / Version / "webgis-tools"/ "artifacts").Rename("cms.tools");

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
            if(!Directory.Exists(DownloadDir)) DownloadDir.CreateDirectory();

            var platform = Platform.Replace("-x64", "64");
            var zipFile = DownloadDir / $"webgis-{platform}-{Version}.zip";

            Log.Information($"Zip Directory: {DeployDir} => {zipFile}");
            if (File.Exists(zipFile)) File.Delete(zipFile);
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
            ProcessTasks.StartProcess("7z",
                $"a -tzip \"{zipFile}\" \"{DeployDir}\\*\"")
                .AssertZeroExitCode();

            // 6. temporäres Deploy‑Verzeichnis aufräumen (rmdir /s /q)
            DeployDir.DeleteDirectory();
        });

    Target Test => _ => _
        .Before(Compile)
        .Executes(() =>
        {
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
                .SetProjectFile(RootDirectory / "src" / "NetStandard" / "E.Standard.WebGIS.Tools.Tests" / "E.Standard.WebGIS.Tools.Tests.csproj")
                .SetProcessWorkingDirectory(RootDirectory)
            );
            DotNetTasks.DotNetTest(s => s
                .SetProjectFile(RootDirectory / "src" / "NetStandard" / "E.Standard.WebMapping.GeoServices.Tests" / "E.Standard.WebMapping.GeoServices.Tests.csproj")
                .SetProcessWorkingDirectory(RootDirectory)
            );
        });
}
