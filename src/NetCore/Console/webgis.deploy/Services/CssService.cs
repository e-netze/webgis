using E.Standard.Json;
using ExCSS;
using System.Collections.Specialized;
using System.Text;
using webgis.deploy.Extensions;
using webgis.deploy.Models;

namespace webgis.deploy.Services;

internal class CssService
{
    private readonly DeployRepositoryService _deployRepositoryService;

    public CssService(DeployRepositoryService deployRepositoryService)
    {
        _deployRepositoryService = deployRepositoryService;
    }

    public void ModifyDefaultCss(string profile, string targetPath, string company)
    {
        ModifyCSS(profile, Path.Combine(targetPath, "webgis-api", "wwwroot", "content", "styles", "default.css"), company);
    }

    public void ModifyPortalCss(string profile, string targetPath, string company)
    {
        ModifyCSS(profile, Path.Combine(targetPath, "webgis-portal", "wwwroot", "content", "portal.css"), Path.Combine("companies", company));
    }

    #region Perform Modification

    private void ModifyCSS(string profile, string originalCssFile, string subFolder)
    {
        var originalFi = new FileInfo(originalCssFile);
        Console.WriteLine($"Build {originalFi.Name} css overrides...");

        if (!originalFi.Exists)
        {
            Console.WriteLine($"Canceld: {originalFi.FullName} not exits");
            return;
        }

        var cssModifyDi = new DirectoryInfo(Path.Combine(_deployRepositoryService.RepositoryRootDirectory, "profiles", profile, "css-modify", originalFi.Name));
        if (!cssModifyDi.Exists)
        {
            Console.WriteLine($"Canceld: {cssModifyDi.FullName} not exits");
            return;
        }

        var cssModifyFi = new FileInfo(Path.Combine(cssModifyDi.FullName, "modify.json"));
        if (!cssModifyFi.Exists)
        {
            Console.WriteLine($"Canceld: {cssModifyFi.FullName} not exits");
            return;
        }

        var cssAppendFi = new FileInfo(Path.Combine(cssModifyDi.FullName, "append.css"));

        string targetPath = Path.Combine(originalFi.Directory.FullName, subFolder, originalFi.Name);

        ModifyCSS(originalFi.FullName, cssModifyFi.FullName, targetPath, cssAppendFi.Exists ? cssAppendFi.FullName : string.Empty);
    }

    private void ModifyCSS(string originalCssFile, string configFile, string outputFile, string appendFile)
    {
        var config = JSerializer.Deserialize<ModifyCssModel>(System.IO.File.ReadAllText(configFile));

        var parser = new StylesheetParser();
        var stylesheet = parser.Parse(System.IO.File.ReadAllText(originalCssFile));

        StringBuilder sbAll = new StringBuilder();

        var tempSelector = "-";

        foreach (var rule in stylesheet.Children)
        {
            StringBuilder sbShrink = new StringBuilder();

            foreach (IStylesheetNode child in rule.Children)
            {
                if (child is ISelector)
                {
                    tempSelector = ((ISelector)child).Text;
                }
                if (child is StyleDeclaration)
                {
                    var declaration = (StyleDeclaration)child;

                    if (HasModificationInRule(declaration, config, sbShrink))
                    {
                        sbAll.Append(tempSelector);
                        sbAll.Append(" {" + Environment.NewLine);

                        if (config.Mode == "shrink")
                        {
                            sbAll.Append(sbShrink.ToString());
                        }
                        else
                        {
                            foreach (var property in declaration.Declarations)
                            {
                                sbAll.Append($"    {property.Name}: {ReplacePropertyValue(property, config)}; {Environment.NewLine}");
                            }
                        }
                        sbAll.Append("}" + Environment.NewLine);
                    }
                }
            }
        }
        //Console.WriteLine(sbAll.ToString());

        if (System.IO.File.Exists(outputFile))
        {
            Console.WriteLine("Delete existing file: " + outputFile);
            System.IO.File.Delete(outputFile);
        }

        Console.WriteLine("Create file: " + outputFile);

        var fi = new FileInfo(outputFile);
        if (!fi.Directory.Exists)
        {
            fi.Directory.Create();
        }

        System.IO.File.WriteAllText(outputFile, sbAll.ToString());

        if (!String.IsNullOrWhiteSpace(appendFile))
        {
            Console.WriteLine("Append " + appendFile);
            System.IO.File.AppendAllText(outputFile, System.IO.File.ReadAllText(appendFile));
            //Console.WriteLine(System.IO.File.ReadAllText(appendFile));
        }
    }

    #endregion

    #region Helper Methods

    private bool HasModificationInRule(StyleDeclaration declaration, ModifyCssModel config, StringBuilder sbShrink)
    {
        bool modificationInRule = false;
        var properties = ToNameValue(declaration);

        foreach (string property in properties.Keys)
        {
            foreach (var modifier in config.ModifierDefinitions)
            {
                string pattern = modifier.Pattern;
                if (pattern.IsColor())
                {
                    pattern = pattern.ToCssRgbColor();
                }
                if (properties[property].ToLower().Contains(pattern))
                {
                    modificationInRule = true;
                    if (config.Mode == "shrink")
                    {
                        sbShrink.Append($"    {property}: {properties[property].ToLower().Replace(pattern, modifier.Replace)};{Environment.NewLine}");

                        //Console.WriteLine(modifier.Add != null ? modifier.Add.ToString() : "add == null");
                        //if (modifier.Add != null)
                        //{
                        //    foreach(var addKey in modifier.Add.Keys.Where(k => k != property))
                        //    {
                        //        sbShrink.Append($"    {addKey}: {modifier.Add[addKey]};{Environment.NewLine}");
                        //    }
                        //}
                    }
                }
            }
        }
        return modificationInRule;
    }

    private string ReplacePropertyValue(Property property, ModifyCssModel config)
    {
        foreach (var modifier in config.ModifierDefinitions)
        {
            string pattern = modifier.Pattern;
            if (pattern.IsColor())
            {
                pattern = pattern.ToCssRgbColor();
            }
            if (property.Value.ToLower().Contains(pattern))
            {
                return property.Value.Replace(pattern, modifier.Replace);
            }
        }
        return property.Value;
    }

    private NameValueCollection ToNameValue(StyleDeclaration declaration)
    {
        NameValueCollection nvc = new NameValueCollection();
        foreach (var nameValue in declaration.CssText.Split(';').Where(s => !String.IsNullOrEmpty(s) && s.Contains(":")))
        {
            int pos = nameValue.IndexOf(":");
            nvc[nameValue.Substring(0, pos)] = nameValue.Substring(pos + 1).Trim();
        }
        return nvc;
    }

    #endregion
}
