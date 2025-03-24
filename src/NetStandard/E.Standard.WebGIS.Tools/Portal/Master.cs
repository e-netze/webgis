using E.Standard.Json;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.IO;
using E.Standard.WebMapping.Core.Api.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Tools.Portal;

[Export(typeof(IApiButton))]
[ToolStorageId(@"WebGIS.Tools.Portal.Maps\{0}\{1}")]   // 0 -> PageId, 1 -> Category
[ToolStorageIsolatedUserAttribute(false)]
[ToolStorageFriendType(typeof(Portal))]
public class Master : IApiServerButton, IStorageInteractions
{
    internal const string MasterBlobName = "__master_template";

    #region IApiServerButton Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e) => null;

    #endregion

    #region IApiButton Member

    public string Name => "Master";

    public string Container => String.Empty;

    public string Image => String.Empty;

    public string ToolTip => "Master";

    public bool HasUI
    {
        get { return false; }
    }

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

    [ServerToolCommand("set-master")]
    public ApiEventResponse OnSaveMap(IBridge bridge, ApiToolEventArguments e)
    {
        string name = MasterBlobName;
        string category = Category(bridge).ToMapNameOrCategory();
        string master = e["master"];

        if (!String.IsNullOrWhiteSpace(master))
        {
            bridge.Storage.Save(name, e["master"]);
        }
        else
        {
            bridge.Storage.Remove(name, WebMapping.Core.Api.IO.StorageBlobType.Normal, true);
        }

        return new ApiEventResponse();
    }

    [ServerToolCommand("get-master")]
    public ApiEventResponse MapJson(IBridge bridge, ApiToolEventArguments e)
    {
        string category = e["category"];
        string map = MasterBlobName;

        string master = bridge.Storage.LoadString(map);

        return new ApiRawStringEventResponse(master, "application/json");
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

        [JsonProperty(PropertyName = "hidden")]
        [System.Text.Json.Serialization.JsonPropertyName("hidden")]
        public bool Hidden { get; set; }

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
            this.Visibliity = PortalPageVisibility.Visible;
            Author = String.Empty;
        }

        public PortalPageVisibility Visibliity { get; set; }
        public string Author { get; set; }
    }

    #endregion
}
