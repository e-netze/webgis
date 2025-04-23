using webgis.deploy.Extensions;
using webgis.deploy.Services;

#if DEBUG || DEBUG_INTERNAL
string workDirectory = @"C:\deploy\webgis";
#else
string workDirectory = Environment.CurrentDirectory; // Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#endif

// -p local -v latest --download-latest --deploy-portal --deploy-api

Console.WriteLine($"******************************************");
Console.WriteLine($"*                                        *");
Console.WriteLine($"*      WebGIS.Deploy Tool {DeployVersionService.DeployToolVersion}      *");
#if INTERNAL
Console.WriteLine($"*             !!!!!!!!!!!!!!             *");
Console.WriteLine($"*             !! INTERNAL !!             *");
Console.WriteLine($"*             !!!!!!!!!!!!!!             *");
#endif
Console.WriteLine($"*                                        *");
Console.WriteLine($"******************************************");

Console.WriteLine($"Work-Directory: {workDirectory}");

bool runAutomated = false;

string profile = String.Empty,
       version = String.Empty;
bool downloadLatest = false;
bool deployCms = false,
     deployPortal = false,
     deployApi = false;

var consoleService = new ConsoleService();

try
{
    if (args != null)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h":
                case "--help":
                    consoleService.WriteHelp();
                    return 0;
                case "-p":
                case "--profile":
                    profile = args[i + 1];
                    runAutomated = true;
                    break;
                case "-d":
                case "--download-latest":
                    downloadLatest = true;
                    break;
                case "-v":
                case "--version":
                    version = args[i + 1];
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
            }
        }
    }

    var ioService = new IOService();
    var repoService = new DeployRepositoryService(ioService, workDirectory);
    var versionService = new DeployVersionService(repoService, ioService);
    var cssService = new CssService(repoService);
    var securityService = new SecurityService(repoService);

    securityService.Init();

    if (String.IsNullOrEmpty(profile))
    {
        profile = consoleService.ChooseFrom(repoService.Profiles(), "profile", allowNewVales: true, examples: "production, staging, test").Trim();

        repoService.CreateProfile(profile);
    }

#if !INTERNAL
    if (downloadLatest || consoleService.DoYouWant("to download the latest version from GitHub"))
    {
        var githubReleaseService = new GitHubReleaseService("e-netze", "webgis");

        foreach (var url in await githubReleaseService.GetLatestReleaseDownloadUrlsAsync())
        {
            var fileName = url.Split('/').Last();

            Console.WriteLine($"Found newest version of {fileName}");

            if (!versionService.Exits(fileName))
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        Console.Write("downloading ...");
                        var fileByptes = await client.GetByteArrayAsync(url);
                        Console.Write("write to disk ...");
                        await versionService.AppendAsync(fileName, fileByptes);
                        Console.WriteLine("done!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception on downloading file: {ex.Message}");
                    return 1;
                }
            }
            else
            {
                Console.WriteLine("... already exists in your download folder");
            }
        }
    }
#endif

    if (String.IsNullOrEmpty(version))
    {
        version = consoleService.ChooseFrom(versionService.GetVersions().Take(5), "version");
    }

    if ("latest".Equals(version, StringComparison.OrdinalIgnoreCase))
    {
        version = versionService.GetVersions().First();
    }

    if (String.IsNullOrEmpty(profile) ||
        String.IsNullOrEmpty(version))
    {
        consoleService.WriteHelp();
        return 1;
    }

    #region Create config overrides

    versionService.InitOverrides(profile, version);

    #endregion

    #region Create Modify-CSS

    versionService.CreateDefaultModifyCss(profile);

    #endregion

    Console.WriteLine($"Deploy version {version} to profile {profile}");
    if (!runAutomated && !consoleService.DoYouWantToContinue())
    {
        return 1;
    }

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
        consoleService.WriteBlock($"Create a new webgis repository {deployVersionModel.RepositoryPath}");

        versionService.ExtractZipFolderRecursive(version, "webgis-api/_setup/proto/_webgis-repository", deployVersionModel.RepositoryPath);
        versionService.ExtractZipFolderRecursive(version, "webgis-portal/_setup/proto/_webgis-repository", deployVersionModel.RepositoryPath);
        versionService.ExtractZipFolderRecursive(version, "webgis-cms/_setup/proto/_webgis-repository", deployVersionModel.RepositoryPath);
    }

    var targetExists = Directory.Exists(Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version)));

    if (!runAutomated)
    {
        deployApi = deployApi || consoleService.DoYouWant("to deploy WebGIS API");
        deployPortal = deployPortal || consoleService.DoYouWant("to deploy WebGIS Portal");
        deployCms = deployCms || consoleService.DoYouWant("to deploy WebGIS CMS");
    }

    Console.WriteLine();
    Console.WriteLine($"Deploy version {version}");

    if (!targetExists)
    {
        if (deployApi)
        {
            Console.WriteLine("Deploy WebGIS API:");
            versionService.ExtractZipFolderRecursive(version, "webgis-api", Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version), "webgis-api"));
        }

        if (deployPortal)
        {
            Console.WriteLine("Deploy WebGIS Portal:");
            versionService.ExtractZipFolderRecursive(version, "webgis-portal", Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version), "webgis-portal"));
        }

        if (deployCms)
        {
            Console.WriteLine("Deploy WebGIS CMS:");
            versionService.ExtractZipFolderRecursive(version, "webgis-cms", Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version), "webgis-cms"));
        }

        if (profile == "local" || profile == "local_internal")
        {
            Console.WriteLine("Deploy WebGIS Scripts:");
            versionService.ExtractZipFolderRecursive(version, "_scripts", Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version), "_scripts"));

            Console.WriteLine("Deploy WebGIS Scripts:");
            versionService.ExtractZipFolderRecursive(version, "_requirements", Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version), "_requirements"));

            Console.WriteLine("Copy root files");
            versionService.ExtractFiles(version, "", Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version)));
        }
    }
    else
    {
        consoleService.WriteBlock("Warning: version already deployed");
    }

    Console.WriteLine("Append keys.config");
    securityService.CopyKeysConfigTo(Path.Combine(deployVersionModel.RepositoryPath, "security", "keys"));

    Console.WriteLine("Overrides");
    if (deployApi)
    {
        versionService.CopyOverrides(profile, "webgis-api", Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version)), deployVersionModel);
    }

    if (deployPortal)
    {
        versionService.CopyOverrides(profile, "webgis-portal", Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version)), deployVersionModel);
    }

    if (deployCms)
    {
        versionService.CopyOverrides(profile, "webgis-cms", Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version)), deployVersionModel);
    }

    string portalConfigPath = Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version), "webgis-portal", "_config", "portal.config");
    var company = portalConfigPath.GetXmlConfigValue("company");

    #region Create Admin/Author User

    // current author id: 11702178
    //var subscriberDb = SubscriberDb.Create(@"fs:c:\temp\test-subscriberdb");
    //subscriberDb.CreateApiSubscriber(new SubscriberDb.Subscriber()
    //{
    //    Name = "heinzi",
    //    Email = "heinzi@no-mail.com",
    //    Password = "paßW0rd"
    //});

    #endregion

    #region CSS Modify

    Console.WriteLine("Create Custom CSS");
    cssService.ModifyDefaultCss(profile, Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version)), company);
    cssService.ModifyPortalCss(profile, Path.Combine(deployVersionModel.ProfileTargetInstallationPath(profile, version)), company);

    #endregion

    consoleService.WriteBlock($"Deployment WebGIS {version} succeeded", '#');
}
catch (Exception ex)
{
    consoleService.WriteBlock($"Error: {ex.Message}", '!');

    return 1;
}

if (!runAutomated)
{
    Console.Write("Press ENTER to quit...");
    Console.ReadLine();
}

return 0;