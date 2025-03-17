using E.Standard.Json;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.IO;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Tools.Portal;

[Export(typeof(IApiButton))]
[ToolStorageId(@"WebGIS.Tools.Portal.Maps\{0}\{1}")]   // 0 -> PageId, 1 -> Category
[ToolStorageIsolatedUserAttribute(false)]
public class PublishApp : IApiServerButton, IStorageInteractions
{
    const string PublishButtonId = "webgis.tools.portal.publishapp";

    #region IApiServerButton Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        string pageId = bridge.CurrentEventArguments["page-id"];
        string templateCategory = bridge.Storage.DecryptName(bridge.CurrentEventArguments["app-category"]);
        string templateName = bridge.Storage.DecryptName(bridge.CurrentEventArguments["app-name"]);
        string description = bridge.CurrentEventArguments["app-description"];
        var metadata = AppMetadata(bridge, templateCategory + "/app@" + templateName);

        return new ApiEventResponse()
            .AddUIElement(
                new UIDiv()
                    .AsDialog()
                    .WithDialogTitle("App veröffentlichen")
                    .WithStyles(UICss.NarrowFormMarginAuto)
                    .AddChildren(
                        new UILabel()
                            .WithLabel("Portal"),
                        new UIInputText(true)
                            .WithId("page-publish-page-id")
                            .WithLabel(pageId)
                            .AsToolParameter(UICss.ToolAutocompleteParameter),

                        new UILabel()
                            .WithLabel("Kategorie"),
                        new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge, this.GetType(), "autocomplete-categories"), 0)
                            .WithId("page-publish-category")
                            .WithLabel(templateCategory)
                            .AsToolParameter(UICss.ToolAutocompleteParameter),

                        new UILabel()
                            .WithLabel("Name"),
                        new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge, this.GetType(), "autocomplete-apps"), 0)
                            .WithId("page-publish-templatename")
                            .WithLabel(templateName)
                            .AsToolParameter(UICss.ToolAutocompleteParameter),

                        new UILabel()
                            .WithLabel("Sichtbarkeit (auf Portalseite)"),
                        new UISelect(new string[] { "Sichtbar", "Versteckt" })
                            .WithId("page-publish-visibility")
                            .AsToolParameter(),

                        new UILabel()
                            .WithLabel("Optimiert für:"),
                        new UISelect()
                            .WithId("page-publish-optimized")
                            .AsToolParameter()
                            .AddOptions(
                                new UISelect.Option()
                                    .WithValue("0")
                                    .WithLabel("Alle Plattformen"),
                                new UISelect.Option()
                                    .WithValue("1")
                                    .WithLabel("Großes Display: Desktop/Laptop/Tablet"),
                                new UISelect.Option()
                                    .WithValue("2")
                                    .WithLabel("Kleines Display: Mobile Endgeräte (Handy)")),

                        new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "publish-app")
                            .WithId(PublishButtonId)
                            .WithText("Veröffentlichen"),

                        new UIHidden()
                            .WithId("page-publish-app")
                            .AsToolParameter("webgis-template-json"),
                        new UIHidden()
                            .WithId("page-app-description")
                            .AsToolParameter()
                            .WithValue(description)))
            .AddUISetters(
                new UISetter("page-publish-visibility", metadata.Visibility == PortalPageVisibility.Hidden ? "Versteckt" : "Sichtbar"),
                new UISetter("page-publish-optimized", metadata.OptimizedForValue.ToString()));
    }

    #endregion

    #region IApiButton Member

    public string Name => "App veröffentlichen";

    public string Container => String.Empty;

    public string Image => String.Empty;

    public string ToolTip => "App veröffentlichen";

    public bool HasUI => false;

    #endregion

    #region IStorageInteractions Member

    public string StoragePathFormatParameter(IBridge bridge, int index)
    {
        if (bridge != null && bridge.CurrentEventArguments != null)
        {
            switch (index)
            {
                case 0:
                    return PageId(bridge);
                case 1:
                    return Category(bridge);
            }
        }
        return String.Empty;
    }

    #endregion

    #region Commands

    [ServerToolCommand("publish-app")]
    public ApiEventResponse OnSaveTemplate(IBridge bridge, ApiToolEventArguments e)
    {
        string name = TemplateName(bridge);

        bridge.Storage.Save($"app@{name}", e["page-publish-app"]);
        bridge.Storage.Save(bridge.Storage.AppendToName($"app@{name}", ".description"),
                            System.Text.Encoding.Unicode.GetBytes(e["page-app-description"] ?? String.Empty),
                            StorageBlobType.Data);
        bridge.Storage.Save($"app@{name}",
                            JSerializer.Serialize(new Metadata()
                            {
                                Visibility = AppVisibility(bridge),
                                OptimizedFor = GetOptimized(bridge),
                                Author = bridge.CurrentUser?.Username
                            }),
                            StorageBlobType.Metadata);

        return new ApiEventResponse()
            .CloseUIDialog();
    }

    [ServerToolCommand("upload-app-image")]
    public ApiEventResponse OnUploadContentImage(IBridge bridge, ApiToolEventArguments e)
    {
        string app = e["app"];
        byte[] data = Convert.FromBase64String(bridge.SecurityDecryptString(e["page-app-imagedata"]));

        app = app.ToLower().StartsWith("app@") ? app : "app@" + app;


        // ToDo: Sercuriry!!! darf nur Map Author!!!
        bridge.Storage.Save(bridge.Storage.AppendToName(app, ".image"), data, StorageBlobType.Data);

        return new ApiRawStringEventResponse(String.Empty, "text/html");
    }

    [ServerToolCommand("app-json")]
    public ApiEventResponse AppJson(IBridge bridge, ApiToolEventArguments e)
    {
        string app = e["app"];

        app = app.ToLower().StartsWith("app@") ? app : $"app@{app}";

        string mapDescription = bridge.Storage.AppendToName(app, ".description");

        if (bridge.Storage.Exists(mapDescription, StorageBlobType.Data))
        {
            var description = System.Text.Encoding.Unicode.GetString(bridge.Storage.Load(mapDescription, WebMapping.Core.Api.IO.StorageBlobType.Data));
        }

        string templateJson = bridge.Storage.LoadString(app);

        return new ApiRawStringEventResponse(templateJson, "application/json");
    }

    [ServerToolCommand("app-description")]
    public ApiEventResponse OnContentDescription(IBridge bridge, ApiToolEventArguments e)
    {
        string app = e["app"];

        app = app.ToLower().StartsWith("app@") ? app : $"app@{app}";

        string mapDescription = bridge.Storage.AppendToName(app, ".description");

        if (bridge.Storage.Exists(mapDescription, WebMapping.Core.Api.IO.StorageBlobType.Data))
        {
            var description = System.Text.Encoding.Unicode.GetString(bridge.Storage.Load(mapDescription, WebMapping.Core.Api.IO.StorageBlobType.Data));

            return new ApiRawStringEventResponse(description, "text/plain");
        }

        return new ApiRawStringEventResponse(String.Empty, "text/plain");
    }

    [ServerToolCommand("autocomplete-categories")]
    public ApiEventResponse OnAutocompleteCategory(IBridge bridge, ApiToolEventArguments e)
    {
        List<string> values = new List<string>();
        string term = e["term"].ToLower();
        string portal = e["page-publish-page-id"];

        foreach (string name in bridge.Storage.GetDirectoryNames(subDir: portal, filter: "%"))
        {
            string catName = name;
            if (catName.ToLower().Contains(term))
            {
                values.Add(catName);
            }
        }

        values.Sort();

        return new ApiRawJsonEventResponse(values.ToArray());
    }

    [ServerToolCommand("autocomplete-apps")]
    public ApiEventResponse OnAutocompleteTemplates(IBridge bridge, ApiToolEventArguments e)
    {
        List<string> values = new List<string>();
        string term = e["term"].ToLower();

        foreach (string name in bridge.Storage.GetNames())
        {
            if (!name.ToLower().StartsWith("app@"))
            {
                continue;
            }

            string templateName = name.Substring(4);

            if (templateName.ToLower().Contains(term))
            {
                values.Add(templateName);
            }
        }

        values.Sort();

        return new ApiRawJsonEventResponse(values.ToArray());
    }

    [ServerToolCommand("delete-app")]
    public ApiEventResponse DeleteMap(IBridge bridge, ApiToolEventArguments e)
    {
        string app = e["app"];

        var metadata = AppMetadata(bridge, $"app@{app}");

        if (metadata.Author != bridge.CurrentUser?.Username)
        {
            throw new Exception("Not authorized");
        }

        bridge.Storage.Remove($"app@{app}", WebMapping.Core.Api.IO.StorageBlobType.Normal, true);

        return new ApiRawStringEventResponse(String.Empty, "text/html");
    }

    #endregion

    #region Helpers

    #region Helper

    private string PageId(IBridge bridge)
    {
        if (!String.IsNullOrWhiteSpace(bridge.CurrentEventArguments["page-publish-page-id"]))
        {
            return bridge.CurrentEventArguments["page-publish-page-id"];
        }

        if (!String.IsNullOrWhiteSpace(bridge.CurrentEventArguments["page-id"]))
        {
            return bridge.CurrentEventArguments["page-id"];
        }

        if (!String.IsNullOrWhiteSpace(bridge.CurrentEventArguments["page"]))
        {
            return bridge.CurrentEventArguments["page"];
        }

        return String.Empty;
    }

    private string Category(IBridge bridge)
    {
        if (!String.IsNullOrWhiteSpace(bridge.CurrentEventArguments["page-publish-category"]))
        {
            return bridge.CurrentEventArguments["page-publish-category"];
        }

        if (!String.IsNullOrWhiteSpace(bridge.CurrentEventArguments["category"]))
        {
            return bridge.CurrentEventArguments["category"];
        }

        return String.Empty;
    }

    private string TemplateName(IBridge bridge)
    {
        if (!String.IsNullOrWhiteSpace(bridge.CurrentEventArguments["page-publish-templatename"]))
        {
            return bridge.CurrentEventArguments["page-publish-templatename"];
        }

        return String.Empty;
    }

    #endregion

    private PortalPageVisibility AppVisibility(IBridge bridge)
    {
        switch (bridge.CurrentEventArguments["page-publish-visibility"]?.ToLower())
        {
            case "versteckt":
                return PortalPageVisibility.Hidden;
            default:
                return PortalPageVisibility.Visible;

        }
    }

    private PortalPageOptimized GetOptimized(IBridge bridge)
    {
        switch (bridge.CurrentEventArguments["page-publish-optimized"]?.ToLower())
        {
            case "1":
                return PortalPageOptimized.Desktop;
            case "2":
                return PortalPageOptimized.Mobile;
            default:
                return PortalPageOptimized.AllPlatforms;

        }
    }

    private Metadata AppMetadata(IBridge bridge, string mapName)
    {
        string metadataString = bridge.Storage.LoadString(mapName, StorageBlobType.Metadata);

        return String.IsNullOrWhiteSpace(metadataString) ?
            new Metadata() :
            JSerializer.Deserialize<Metadata>(metadataString);
    }

    class Metadata
    {
        public Metadata()
        {
            this.Visibility = PortalPageVisibility.Visible;
            Author = String.Empty;
        }

        [JsonProperty("Visibliity")]
        [System.Text.Json.Serialization.JsonPropertyName("Visibliity")]  // Tippfehler => kommt jetzt leider in allen Metadata Files vor!!
        public PortalPageVisibility Visibility { get; set; }
        public string Author { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public PortalPageOptimized OptimizedFor
        {
            get
            {
                return (PortalPageOptimized)OptimizedForValue;
            }
            set
            {
                OptimizedForValue = (int)value;
            }
        }

        [JsonProperty("optimized")]
        [System.Text.Json.Serialization.JsonPropertyName("optimized")]
        public int OptimizedForValue { get; set; }
    }

    #endregion
}
