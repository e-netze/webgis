using E.Standard.Json;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Extensions;
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Portal;

[Export(typeof(IApiButton))]
[ToolStorageId(@"WebGIS.Tools.Portal.Maps\{0}\{1}")]   // 0 -> PageId, 1 -> Category
[ToolStorageIsolatedUserAttribute(false)]
[ToolStorageFriendType(typeof(Portal))]
public class Publish : IApiServerButton, IStorageInteractions
{
    const string PublishButtonId = "webgis.tools.portal.publish";

    #region IApiServerButton Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        string pageId = bridge.CurrentEventArguments["page-id"];
        string mapCategory = bridge.Storage.DecryptName(bridge.CurrentEventArguments["map-category"]);
        string mapName = bridge.Storage.DecryptName(bridge.CurrentEventArguments["map-name"]);
        var metadata = MapMetadata(bridge, $"{mapCategory}/{mapName}");

        return new ApiEventResponse()
            .AddUIElements(
                new UIDiv()
                    .AsDialog()
                    .WithDialogTitle("Karte öffentlichen")
                    .WithStyles(UICss.NarrowFormMarginAuto)
                    .AddChildren(
                        new UILabel()
                            .WithLabel("Portal"),
                        new UIInputText(true)
                            .WithId("page-publish-page-id")
                            .WithLabel(pageId)
                            .AsToolParameter(),
                        new UILabel()
                            .WithLabel("Kategorie"),
                        new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge, this.GetType(), "autocomplete-categories"), 0)
                            .WithId("page-publish-category")
                            .WithLabel(mapCategory)
                            .AsToolParameter(UICss.ToolAutocompleteParameter),
                        new UILabel()
                            .WithLabel("Name"),
                        new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge, this.GetType(), "autocomplete-maps"), 0)
                            .WithId("page-publish-mapname")
                            .WithLabel(mapName)
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
                        new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "publish-map")
                            .WithId(PublishButtonId)
                            .WithText("Veröffentlichen"),

                        new UIHidden()
                            .WithId("page-publish-mapjson")
                            .AsToolParameter(UICss.AutoSetterMapSerialization),
                        new UIHidden()
                            .WithId("page-publish-graphics-geojson")
                            .AsToolParameter(UICss.AutoSetterMapGraphicsGeoJson),
                        new UIHidden()
                            .WithId("page-publish-map-description")
                            .AsToolParameter(UICss.AutoSetterMapBuilderRawMapDescription),
                        new UIHidden()
                            .WithId("page-publish-html-meta-tags")
                            .AsToolParameter(UICss.AutoSetterMapBuilderHtmlMetaTags)))
            .AddUISetters(
                new UISetter("page-publish-visibility", metadata.Visibility == PortalPageVisibility.Hidden ? "Versteckt" : "Sichtbar"),
                new UISetter("page-publish-optimized", metadata.OptimizedForValue.ToString()));
    }

    #endregion

    #region IApiButton Member

    public string Name => "Karte veröffentlichen";

    public string Container => String.Empty;

    public string Image => String.Empty;

    public string ToolTip => "Karte veröffentlichen";

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

    [ServerToolCommand("publish-map")]
    public ApiEventResponse OnSaveMap(IBridge bridge, ApiToolEventArguments e)
    {
        string name = MapName(bridge).ToMapNameOrCategory();
        string category = Category(bridge).ToMapNameOrCategory();

        var visibility = MapVisibility(bridge);

        bridge.Storage.Save(name, e["page-publish-mapjson"]);

        string geoJson = e["page-publish-graphics-geojson"];
        if (!String.IsNullOrWhiteSpace(geoJson) && geoJson.Length > 2)
        {
            bridge.Storage.Save(name + ".graphics", geoJson, WebMapping.Core.Api.IO.StorageBlobType.Data);
        }

        string mapDescription = e["page-publish-map-description"];
        if (!String.IsNullOrWhiteSpace(mapDescription))
        {
            byte[] data = System.Text.Encoding.Unicode.GetBytes(mapDescription);
            bridge.Storage.Save(bridge.Storage.AppendToName(name, ".description"), data, WebMapping.Core.Api.IO.StorageBlobType.Data);
        }

        bridge.Storage.Save(name,
            JSerializer.Serialize(new Metadata()
            {
                Visibility = MapVisibility(bridge),
                OptimizedFor = GetOptimized(bridge),
                Author = bridge.CurrentUser?.Username,
                HtmlMetaTags = e["page-publish-html-meta-tags"]
            }),
            WebMapping.Core.Api.IO.StorageBlobType.Metadata);

        return new ApiEventResponse()
            .CloseUIDialog();
    }

    [ServerToolCommand("upload-map-image")]
    public ApiEventResponse OnUploadContentImage(IBridge bridge, ApiToolEventArguments e)
    {
        string map = e["map"];
        if (!IsMapAuthor(bridge, map))
        {
            throw new Exception("Not authorized");
        }

        byte[] data = Convert.FromBase64String(bridge.SecurityDecryptString(e["page-map-imagedata"]));

        bridge.Storage.Save(bridge.Storage.AppendToName(map, ".image"), data, WebMapping.Core.Api.IO.StorageBlobType.Data);

        return new ApiRawStringEventResponse(String.Empty, "text/html");
    }

    [ServerToolCommand("map-image")]
    public ApiEventResponse OnContentImage(IBridge bridge, ApiToolEventArguments e)
    {
        string map = e["map"];

        byte[] data = null;
        string mapImage = bridge.Storage.AppendToName(map, ".image");
        if (bridge.Storage.Exists(mapImage, WebMapping.Core.Api.IO.StorageBlobType.Data))
        {
            data = bridge.Storage.Load(mapImage, WebMapping.Core.Api.IO.StorageBlobType.Data);
        }
        else
        {
            MemoryStream ms = new MemoryStream(Properties.Resources.map_64_g);
            data = ms.ToArray();
        }

        return new ApiRawBytesEventResponse(data, "image/jpeg");
    }

    [ServerToolCommand("map-update-description")]
    public ApiEventResponse OnUpdateContentDescription(IBridge bridge, ApiToolEventArguments e)
    {
        string map = e["map"];
        if (!IsMapAuthor(bridge, map))
        {
            throw new Exception("Not authorized");
        }

        byte[] data = System.Text.Encoding.Unicode.GetBytes(e["description"]);

        // ToDo: Sercuriry!!! darf nur Map Author!!!
        bridge.Storage.Save(bridge.Storage.AppendToName(map, ".description"), data, WebMapping.Core.Api.IO.StorageBlobType.Data);

        return new ApiRawStringEventResponse(String.Empty, "text/html");
    }

    [ServerToolCommand("map-description")]
    public ApiEventResponse OnContentDescription(IBridge bridge, ApiToolEventArguments e)
    {
        string map = e["map"];

        string mapDescription = bridge.Storage.AppendToName(map, ".description");

        if (bridge.Storage.Exists(mapDescription, WebMapping.Core.Api.IO.StorageBlobType.Data))
        {
            var description = System.Text.Encoding.Unicode.GetString(bridge.Storage.Load(mapDescription, WebMapping.Core.Api.IO.StorageBlobType.Data));

            if (!true.Equals(e.GetBoolean("raw")))
            {
                description = description.RemoveSectionsPlaceholders();
            }

            return new ApiRawStringEventResponse(description, "text/plain");
        }

        return new ApiRawStringEventResponse(String.Empty, "text/plain");
    }

    [ServerToolCommand("autocomplete-categories")]
    public ApiEventResponse OnAutocompleteCategories(IBridge bridge, ApiToolEventArguments e)
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

    [ServerToolCommand("autocomplete-maps")]
    public ApiEventResponse OnAutocompleteMaps(IBridge bridge, ApiToolEventArguments e)
    {
        List<string> values = new List<string>();
        string term = e["term"].ToLower();

        foreach (string name in bridge.Storage.GetNames())
        {
            if (name.StartsWith("app@"))
            {
                continue;
            }

            if (name.ToLower().Contains(term))
            {
                values.Add(name);
            }
        }

        values.Sort();

        return new ApiRawJsonEventResponse(values.ToArray());
    }

    [ServerToolCommand("mapjson")]
    public ApiEventResponse MapJson(IBridge bridge, ApiToolEventArguments e)
    {
        string category = e["category"];
        string map = e["map"];

        if (!bridge.Storage.Exists(map))
        {
            throw new Exception("Karte existiert nicht oder Sie sind nicht berechtigt diese Karte anzuzeigen");
        }

        string mapJson = bridge.Storage.LoadString(map).ReplaceLegacyMapJsonItems();
        string geoJson = bridge.Storage.LoadString(
            bridge.Storage.AppendToName(map, ".graphics"), WebMapping.Core.Api.IO.StorageBlobType.Data);

        string masterJson1 = null, masterJson0 = null;

        if (e["add_master"] == "true" && !map.Equals(Master.MasterBlobName, StringComparison.InvariantCultureIgnoreCase))
        {
            masterJson1 = bridge.Storage.LoadString(Master.MasterBlobName)
                                        .ReplaceLegacyMapJsonItems();

            #region Search in root directory

            var cat1 = bridge.CurrentEventArguments["page-publish-category"];
            var cat2 = bridge.CurrentEventArguments["category"];

            bridge.CurrentEventArguments["page-publish-category"] = null;
            bridge.CurrentEventArguments["category"] = null;
            masterJson0 = bridge.Storage.LoadString(Master.MasterBlobName)
                                        .ReplaceLegacyMapJsonItems();

            bridge.CurrentEventArguments["page-publish-category"] = cat1;
            bridge.CurrentEventArguments["category"] = cat2;

            #endregion
        }

        Metadata metadata = null;
        if (e["add_meta"] == "true")
        {
            metadata = MapMetadata(bridge, map);
        }

        string description = null;
        if (e["add_description"] == "true")
        {
            string mapDescription = bridge.Storage.AppendToName(map, ".description");

            if (bridge.Storage.Exists(mapDescription, WebMapping.Core.Api.IO.StorageBlobType.Data))
            {
                description = System.Text.Encoding.Unicode.GetString(bridge.Storage.Load(mapDescription, WebMapping.Core.Api.IO.StorageBlobType.Data));
            }
        }

        //return new ApiRawStringEventResponse(mapJson, "application/json");
        return new MapJsonResponse()
        {
            Graphics = String.IsNullOrWhiteSpace(geoJson) ? null : new GraphicsResponse(bridge) { Elements = geoJson },
            SerializationMapJson = mapJson,
            Master1Json = masterJson1,
            Master0Json = masterJson0,

            HtmlMetaTags = metadata?.HtmlMetaTags,
            MapDescription = description
        };
    }

    [ServerToolCommand("map-categories")]
    async public Task<ApiEventResponse> MapCategories(IBridge bridge, ApiToolEventArguments e)
    {
        if (String.IsNullOrEmpty(PageId(bridge)) || !String.IsNullOrEmpty(Category(bridge)))
        {
            throw new ArgumentException();
        }

        List<string> values = new List<string>(
            bridge.Storage.GetNames().Where(n => !Master.MasterBlobName.Equals(n))
            );

        values = values.OrderNames(bridge.Storage.GetItemOrder()).ToList();

        if ((await bridge.GetUserFavoriteItemsAsync(this, "CategoryMaps"))
                         .Where(favMapName =>
                             {
                                 try
                                 {
                                     string category = favMapName.Split('/')[0],
                                            mapName = favMapName.Split('/')[1];

                                     if (category.StartsWith("_"))
                                     {
                                         return false;
                                     }

                                     e["category"] = category;  // damit im Storage im richtigen Verzeichnis gesucht wird
                                     return bridge.Storage.Exists(mapName); // Karte kommt in diesen Portal nicht vor (muss abgefragt werden, weil in bein Favs keine PortalId migespeichert wird)
                                 }
                                 catch
                                 {
                                     return false;
                                 }
                             }
                         )
                         .FirstOrDefault() != null)
        {
            values.Insert(0, "_Favoriten");
        }

        return new ApiRawJsonEventResponse(new Categories()
        {
            CategoriesArray = values.ToArray()
        });
    }

    [ServerToolCommand("category-maps")]
    async public Task<ApiEventResponse> CategoryMaps(IBridge bridge, ApiToolEventArguments e)
    {
        if (String.IsNullOrEmpty(PageId(bridge)) || String.IsNullOrEmpty(Category(bridge)))
        {
            throw new ArgumentException();
        }

        List<Map> maps = new List<Map>();

        string mapCategory = e["category"];

        if (Category(bridge) == "_Favoriten")
        {
            foreach (var favMapName in await bridge.GetUserFavoriteItemsAsync(this, "CategoryMaps"))
            {
                string category = favMapName.Split('/')[0],
                       mapName = favMapName.Split('/')[1];

                e["category"] = category;  // damit im Storage im richtigen Verzeichnis gesucht wird
                if (category.StartsWith("_") || !bridge.Storage.Exists(mapName))
                {
                    continue;   // Karte kommt in diesen Portal nicht vor (muss abgefragt werden, weil in bein Favs keine PortalId migespeichert wird)
                }

                var metadata = MapMetadata(bridge, mapName);

                string description = null;
                var descriptionBytes = bridge.Storage.Load($"{mapName}.description", WebMapping.Core.Api.IO.StorageBlobType.Data);
                if (descriptionBytes != null)
                {
                    try
                    {
                        description = System.Text.Encoding.Unicode.GetString(descriptionBytes).RemoveSectionsPlaceholders();
                    }
                    catch { }
                }

                maps.Add(new Map(mapName)
                {
                    Category = category,
                    Hidden = metadata.Visibility == PortalPageVisibility.Hidden,
                    OptimizedFor = metadata.OptimizedFor.ToString().ToLower(),
                    Description = description
                });
            }
        }
        else
        {
            foreach (var mapName in bridge.Storage.GetNames())
            {
                MapJsonClass mapJson = null;

                if (e.GetBoolean("add-services"))
                {
                    var mapJsonString = bridge.Storage.LoadString(mapName);
                    mapJson = JSerializer.Deserialize<MapJsonClass>(mapJsonString);
                }

                var metadata = MapMetadata(bridge, mapName);

                if (metadata.Visibility == PortalPageVisibility.Hidden && !metadata.Author.Equals(bridge.CurrentUser?.Username))
                {
                    continue;
                }

                string description = null;
                var descriptionBytes = bridge.Storage.Load($"{mapName}.description", WebMapping.Core.Api.IO.StorageBlobType.Data);
                if (descriptionBytes != null)
                {
                    try
                    {
                        description = System.Text.Encoding.Unicode.GetString(descriptionBytes).RemoveSectionsPlaceholders();
                    }
                    catch { }
                }

                maps.Add(new Map(mapName)
                {
                    Hidden = metadata.Visibility == PortalPageVisibility.Hidden,
                    OptimizedFor = metadata.OptimizedFor.ToString().ToLower(),
                    Services = mapJson?.Services?.Select(s => s.Id).ToArray(),
                    Description = description
                });
            }

            maps.Sort(new Map.MapAlpahethicSorter());

            var order = bridge.Storage.GetItemOrder();
            if (order != null)
            {
                maps.Sort(new Map.MapNamesOrderSorter(order));
            }
        }

        return new ApiRawJsonEventResponse(new
        {
            category = mapCategory,
            maps = maps.ToArray()
        });
    }

    [ServerToolCommand("delete-map")]
    public ApiEventResponse DeleteMap(IBridge bridge, ApiToolEventArguments e)
    {
        string map = e["map"];
        if (!IsMapAuthor(bridge, map))
        {
            throw new Exception("Not authorized");
        }

        bridge.Storage.Remove(map, WebMapping.Core.Api.IO.StorageBlobType.Normal, true);

        return new ApiRawStringEventResponse(String.Empty, "text/html");
    }

    [ServerToolCommand("remove-category")]
    public ApiEventResponse DeleteCategory(IBridge bridge, ApiToolEventArguments e)
    {
        string pageId = e["page"], category = e["category"];

        if (bridge.Storage.Remove(String.Empty, WebMapping.Core.Api.IO.StorageBlobType.Folder))
        {
            return new ApiRawStringEventResponse("{\"success\":true}", "application/json");
        }
        else
        {
            throw new Exception("Can't remove folder. Maybe not empty");
        }
    }

    [ServerToolCommand("remove-allcategories")]
    public ApiEventResponse DeleteAllCategories(IBridge bridge, ApiToolEventArguments e)
    {
        if (!bridge.Storage.Remove(String.Empty, type: WebMapping.Core.Api.IO.StorageBlobType.Folder, recursive: true))
        {
            throw new Exception("Can't remove categories and maps...");
        }

        return null;
    }

    [ServerToolCommand("get-map-html-meta-tags")]
    public ApiEventResponse GetMapHtmlMetaTags(IBridge bridge, ApiToolEventArguments e)
    {
        string map = e["map"];

        var metadata = MapMetadata(bridge, map);

        return new ApiRawStringEventResponse(metadata?.HtmlMetaTags ?? String.Empty, "text/plain");
    }

    [ServerToolCommand("set-item-order")]
    public ApiEventResponse OnSetItemOrder(IBridge bridge, ApiToolEventArguments e)
    {
        if (String.IsNullOrEmpty(PageId(bridge)))
        {
            throw new ArgumentException();
        }

        var method = bridge.SecurityDecryptString(e["sorting-method"]);
        var items = JSerializer.Deserialize<string[]>(bridge.SecurityDecryptString(e["sorting-items"]));

        if (method == "categories")
        {
            bridge.Storage.SetItemOrder(items);
        }
        else if (method == "maps" && !String.IsNullOrWhiteSpace(e["category"]))
        {
            bridge.Storage.SetItemOrder(items);
        }

        return new ApiRawStringEventResponse(String.Empty, "text/html");
    }

    #region Result Classes

    public class Map
    {
        public Map()
        {
            this.Hidden = false;
        }
        public Map(string name)
            : this()
        {
            this.Name = name;
        }

        [JsonProperty(PropertyName = "name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "displayname")]
        [System.Text.Json.Serialization.JsonPropertyName("displayname")]
        public string DisplayName
        {
            get
            {
                if (Name?.StartsWith("app@") == true)
                {
                    return Name.Substring(4);
                }

                return Name;
            }
        }

        [JsonProperty(PropertyName = "category", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("category")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string Category { get; set; }

        [JsonProperty(PropertyName = "services", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("services")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string[] Services { get; set; }

        [JsonProperty(PropertyName = "description", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "hidden")]
        [System.Text.Json.Serialization.JsonPropertyName("hidden")]
        public bool Hidden { get; set; }

        [JsonProperty(PropertyName = "optimized_for")]
        [System.Text.Json.Serialization.JsonPropertyName("optimized_for")]
        public string OptimizedFor { get; set; }

        public static Map[] FormNames(string[] names)
        {
            List<Map> maps = new List<Map>();

            if (names != null)
            {
                foreach (var name in names)
                {
                    maps.Add(new Map(name));
                }
            }

            maps.Sort(new MapAlpahethicSorter());
            return maps.ToArray();
        }

        internal class MapAlpahethicSorter : IComparer<Map>
        {
            public int Compare(Map x, Map y)
            {
                return x.DisplayName.CompareTo(y.DisplayName);
            }
        }

        internal class MapNamesOrderSorter : IComparer<Map>
        {
            private readonly string[] _namesOrder;

            public MapNamesOrderSorter(string[] namesOrder)
            {
                _namesOrder = namesOrder;
            }

            public int Compare(Map x, Map y)
            {
                if (_namesOrder.Contains(x.Name) && _namesOrder.Contains(y.Name))
                {
                    return _namesOrder.IndexOf(x.Name).CompareTo(_namesOrder.IndexOf(y.Name));
                }

                if (_namesOrder.Contains(x.Name))
                {
                    return -1;
                }

                if (_namesOrder.Contains(y.Name))
                {
                    return 1;
                }

                return x.DisplayName.CompareTo(y.DisplayName);
            }
        }
    }

    public class Categories
    {
        [JsonProperty(PropertyName = "categories")]
        [System.Text.Json.Serialization.JsonPropertyName("categories")]
        public string[] CategoriesArray { get; set; }
    }

    private class MapJsonPropto
    {
        public userdataProto userdata { get; set; }

        public class userdataProto
        {
            public metaProto meta { get; set; }

            public class metaProto
            {
                public string author { get; set; }
            }
        }
    }

    #endregion

    #region Classes

    public class MapJsonClass
    {
        [JsonProperty("services")]
        [System.Text.Json.Serialization.JsonPropertyName("services")]
        public IEnumerable<Service> Services { get; set; }

        public class Service
        {
            [JsonProperty("id")]
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public string Id { get; set; }
        }
    }

    #endregion

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

    private string MapName(IBridge bridge)
    {
        if (!String.IsNullOrWhiteSpace(bridge.CurrentEventArguments["page-publish-mapname"]))
        {
            return bridge.CurrentEventArguments["page-publish-mapname"];
        }

        return String.Empty;
    }

    private PortalPageVisibility MapVisibility(IBridge bridge)
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

    private bool IsMapAuthor(IBridge bridge, string map)
    {
        try
        {
            string mapJson = bridge.Storage.LoadString(map).ReplaceLegacyMapJsonItems();
            MapJsonPropto mapJsonProto = JSerializer.Deserialize<MapJsonPropto>(mapJson);

            return mapJsonProto.userdata != null && mapJsonProto.userdata.meta != null &&
                 bridge.CurrentUser.Username.Equals(mapJsonProto.userdata.meta.author);
        }
        catch
        {
            return false;
        }
    }

    #endregion

    private Metadata MapMetadata(IBridge bridge, string mapName)
    {
        string metadataString = bridge.Storage.LoadString(mapName, WebMapping.Core.Api.IO.StorageBlobType.Metadata);

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

        public string HtmlMetaTags { get; set; }

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
