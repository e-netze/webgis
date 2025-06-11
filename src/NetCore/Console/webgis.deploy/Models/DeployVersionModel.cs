using webgis.deploy.Reflection;

namespace webgis.deploy.Models;

internal class DeployVersionModel
{
    public string ProfileName { get; set; }

    [ModelProperty(Prompt = "Company",
                   DefaultValue = "my-company",
                   Placeholder = "company",
                   RegexPattern = "^[a-z0-9-]+$",
                   RegexNotMatchMessage = "only lowercache letters, numbers und '-' is allowed, eg 'my-company'")]
    public string Company { get; set; }

    [ModelProperty(Prompt = "Target installation path",
                   DefaultValue = "C:\\apps\\webgis")]
    public string TargetInstallationPath { get; set; }

    [ModelProperty(Prompt = "Repsitory path",
                   DefaultValue = "{TargetInstallationPath}/{ProfileName}/webgis-repository",
                   Placeholder = "api-repository-path")]
    public string RepositoryPath { get; set; }

    [ModelProperty(Prompt = "Api online url",
                   DefaultValue = "http://localhost:5001",
                   Placeholder = "api-onlineresource")]
    public string ApiOnlineResource { get; set; }

    [ModelProperty(Prompt = "Api internal url",
                   DefaultValue = "http://localhost:5001",
                   Placeholder = "api-internal-url")]
    public string ApiInternalUrl { get; set; }

    [ModelProperty(Prompt = "Portal online url",
                   DefaultValue = "http://localhost:5002",
                   Placeholder = "portal-onlineresource")]
    public string PortalOnlineResource { get; set; }

    [ModelProperty(Prompt = "Portal internal url",
                   DefaultValue = "http://localhost:5002",
                   Placeholder = "portal-internal-url")]
    public string PortalInternalUrl { get; set; }

    [ModelProperty(Prompt = "Default Calc Crs",
                   DefaultValue = "3857",
                   Placeholder = "default-calc-crs")]
    public string DefaultCalcCrs { get; set; } 

    //[ModelProperty(Prompt = "UI Color Code (light color)",
    //               DefaultValue = "#b5dbad",
    //               Placeholder = "color1")]
    //public string UiColor1 { get; set; }
    //[ModelProperty(Prompt = "UI Color Code",
    //               DefaultValue = "http://localhost:5002",
    //               Placeholder = "#82C828")]
    //public string UiColor2 { get; set; }
}
