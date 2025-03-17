using E.Standard.Json;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Models;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.IO;
using E.Standard.WebMapping.Core.Api.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace E.Standard.WebGIS.Tools.Portal;

[Export(typeof(IApiButton))]
[ToolStorageId(@"WebGIS.Tools.Portal.pages\{0}")]
[ToolStorageIsolatedUser(false)]
public class Portal : IApiServerButton, IStorageInteractions
{
    const string IndexIds = "page-ids";

    #region IApiServerButton Member

    /// <summary>
    /// Die Implementierung für das OnButtonClick Event für das Portal Werkzeug.
    /// Die Funktion regelt....
    /// </summary>
    /// <param name="bridge">Das Bridge Objekt wird vom webGIS 5 Framework übergeben</param>
    /// <returns>Die Funktion gibt ein ApiEventResponse Objekt an der webGIS 5 Framework zurück</returns>
    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        return null;
    }

    #endregion IApiServerButton Member

    #region IApiButton Member

    public string Container => String.Empty;

    public bool HasUI => false;

    public string Image => String.Empty;

    public string Name => "Portal";

    public string ToolTip => "Portal";

    #endregion IApiButton Member

    #region IStorageInteractions

    public string StoragePathFormatParameter(IBridge bridge, int index)
    {
        if (bridge != null && bridge.CurrentEventArguments != null)
        {
            switch (index)
            {
                case 0:
                    return !String.IsNullOrWhiteSpace(bridge.CurrentEventArguments["page-id"]) ? bridge.CurrentEventArguments["page-id"] : String.Empty;
            }
        }
        return String.Empty;
    }

    #endregion

    #region Commands

    [ServerToolCommand("update-page")]
    public ApiEventResponse OnUpdatePage(IBridge bridge, ApiToolEventArguments e)
    {
        string id = e["page-id"];

        string owner = PageOwnerName(bridge, id);
        if (!String.IsNullOrWhiteSpace(owner) && owner != bridge.CurrentUser.Username)
        {
            throw new Exception("Portal '" + id + "' is already created by user '" + owner + "'");
        }

        string json = e["page-json"];

        bridge.Storage.Save(id, json);

        string bannerImage = e["page-banner-image"];
        if (!String.IsNullOrWhiteSpace(bannerImage))
        {
            byte[] data = Convert.FromBase64String(bannerImage);
            bridge.Storage.Save(id + ".banner-image", data, WebMapping.Core.Api.IO.StorageBlobType.Data);
        }

        bridge.Storage.SetUniqueIndexItem(IndexIds, id, bridge.CurrentUser.Username);
        bridge.Storage.SetUniqueIndexItem(bridge.CurrentUser.Username, id, String.Empty);

        return null;
    }

    [ServerToolCommand("delete-page")]
    public ApiEventResponse OnDeletePage(IBridge bridge, ApiToolEventArguments e)
    {
        string id = e["page-id"];

        string owner = PageOwnerName(bridge, id);
        if (!String.IsNullOrWhiteSpace(owner) && owner != bridge.CurrentUser.Username)
        {
            throw new Exception("Portal '" + id + "' is created by user '" + owner + "'");
        }

        bridge.Storage.RemoveUniqueIndexItem(IndexIds, id);
        bridge.Storage.RemoveUniqueIndexItem(bridge.CurrentUser.Username, id);

        bridge.Storage.Remove(String.Empty, type: WebMapping.Core.Api.IO.StorageBlobType.Folder, recursive: true);

        return null;
    }

    [ServerToolCommand("list-user-pages")]
    public ApiEventResponse OnListUserPages(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiRawJsonEventResponse(bridge.Storage.GetUniqueIndexKeys(bridge.CurrentUser.Username));
    }

    [ServerToolCommand("user-page")]
    public ApiEventResponse OnUserPage(IBridge bridge, ApiToolEventArguments e)
    {
        string id = e["page-id"];

        string pageUser = bridge.Storage.GetUniqueIndexItem(IndexIds, id);
        if (String.IsNullOrWhiteSpace(pageUser) || pageUser.ToLower() != bridge.CurrentUser.Username.ToLower())
        {
            throw new Exception("Current user is not portal owner");
        }

        string jsonString = bridge.Storage.LoadString(id);
        return new ApiRawStringEventResponse(jsonString, "application/json");
    }

    [ServerToolCommand("page")]
    public ApiEventResponse OnPortal(IBridge bridge, ApiToolEventArguments e)
    {
        string id = e["page-id"];

        //string portalUrlName = PortalUrlName(bridge, id);
        //if(!String.IsNullOrWhiteSpace(portalUrlName))
        //    return new ApiRawStringEventResponse(bridge.Storage.LoadString(portalUrlName), "application/json");

        if (bridge.Storage.Exists(id, WebMapping.Core.Api.IO.StorageBlobType.Normal))
        {
            string jsonString = bridge.Storage.LoadString(id);
            return new ApiRawStringEventResponse(jsonString, "application/json");
        }

        return new ApiRawStringEventResponse("{}", "application/json");
    }

    [ServerToolCommand("pages")]
    public ApiEventResponse OnListPages(IBridge bridge, ApiToolEventArguments e)
    {
        string[] portalIds = bridge.Storage.GetNames();
        StringBuilder sb = new StringBuilder();

        sb.Append("[");
        int counter = 0;
        foreach (string id in portalIds)
        {
            var portalJsonString = bridge.Storage.LoadString(id + "/" + id);
            if (counter > 0)
            {
                sb.Append(",");
            }

            sb.Append(portalJsonString);
            counter++;
        }
        sb.Append("]");

        return new ApiRawStringEventResponse(sb.ToString(), "application/json");
    }

    [ServerToolCommand("page-banner")]
    public ApiEventResponse OnBannerImage(IBridge bridge, ApiToolEventArguments e)
    {
        string id = e["id"];
        string pageId = bridge.SecurityDecryptString(id);

        string banner = pageId + "/" + pageId + ".banner-image";

        byte[] data = null;
        if (bridge.Storage.Exists(banner, WebMapping.Core.Api.IO.StorageBlobType.Data))
        {
            data = bridge.Storage.Load(banner, WebMapping.Core.Api.IO.StorageBlobType.Data);
        }
        else
        {
            MemoryStream ms = new MemoryStream(Properties.Resources.default_banner);
            data = ms.ToArray();
        }

        return new ApiRawBytesEventResponse(data, "image/jpeg");
    }

    [ServerToolCommand("page-content")]
    public ApiEventResponse OnPortalContent(IBridge bridge, ApiToolEventArguments e)
    {
        string pageId = e["page-id"];

        string portalOwner = PageOwnerName(bridge, pageId);
        if (!String.IsNullOrWhiteSpace(portalOwner))
        {
            string[] sorting =
                JointSorting("page-title,page-description,page-maps",
                bridge.Storage.LoadString(GetPagePathName(bridge, pageId) + ".content-sorting", WebMapping.Core.Api.IO.StorageBlobType.Metadata)).Split(',');

            List<object> contentObjects = new List<object>();
            foreach (string contentId in sorting)
            {
                string content = bridge.Storage.LoadString(GetPagePathName(bridge, pageId) + ".content-" + contentId, WebMapping.Core.Api.IO.StorageBlobType.Data);
                string contentMeta = bridge.Storage.LoadString(GetPagePathName(bridge, pageId) + ".content-" + contentId, WebMapping.Core.Api.IO.StorageBlobType.Metadata);

                if (!String.IsNullOrEmpty(content))
                {
                    contentObjects.Add(new
                    {
                        id = contentId,
                        content = content,
                        meta = String.IsNullOrWhiteSpace(contentMeta) ? null : JSerializer.Deserialize<ContentMeta>(contentMeta)
                    });
                }
            }

            return new ApiRawJsonEventResponse(new
            {
                sorting = sorting,
                contentitems = contentObjects.ToArray()
            });
        }

        return new ApiRawStringEventResponse("{}", "application/json");
    }

    [ServerToolCommand("update-content")]
    public ApiEventResponse OnUpdateContent(IBridge bridge, ApiToolEventArguments e)
    {
        string pageId = e["page-id"];
        string content = bridge.SecurityDecryptString(e["page-content"]);
        string contentId = bridge.SecurityDecryptString(e["page-content-id"]);
        string sorting = bridge.SecurityDecryptString(e["page-content-sorting"]);

        string portalOwner = PageOwnerName(bridge, pageId);

        if (!String.IsNullOrWhiteSpace(portalOwner))
        {
            ApiPortalPageDTO portal = GetPortalPage(bridge, pageId);
            string currentSorting = bridge.Storage.LoadString(GetPagePathName(bridge, pageId) + ".content-sorting", WebMapping.Core.Api.IO.StorageBlobType.Metadata);

            if (portal != null)
            {
                if (UserManagement.IsAllowed(bridge.CurrentUser.Username, portal.ContentAuthors) || UserManagement.IsAllowed(bridge.CurrentUser.UserRoles, portal.ContentAuthors))
                {
                    bridge.Storage.Save(
                        GetPagePathName(bridge, pageId) + ".content-" + contentId,
                        content, WebMapping.Core.Api.IO.StorageBlobType.Data);

                    bridge.Storage.Save(
                        GetPagePathName(bridge, pageId) + ".content-" + contentId,
                        JSerializer.Serialize(new ContentMeta()
                        {
                            Creator = bridge.CurrentUser.Username,
                            LastChange = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString()
                        }),
                        WebMapping.Core.Api.IO.StorageBlobType.Metadata);

                    bridge.Storage.Save(
                        GetPagePathName(bridge, pageId) + ".content-sorting",
                        JointSorting(currentSorting, sorting), WebMapping.Core.Api.IO.StorageBlobType.Metadata);

                    return new ApiRawStringEventResponse("{\"success\":true}", "application/json");
                }
            }
        }

        return new ApiRawStringEventResponse("{\"success\":false}", "application/json");
    }

    [ServerToolCommand("update-content-sorting")]
    public ApiEventResponse OnContentSorting(IBridge bridge, ApiToolEventArguments e)
    {
        string pageId = e["page-id"];
        string content = bridge.SecurityDecryptString(e["page-content"]);
        string contentId = bridge.SecurityDecryptString(e["page-content-id"]);
        string sorting = bridge.SecurityDecryptString(e["page-content-sorting"]);

        string portalOwner = PageOwnerName(bridge, pageId);

        if (!String.IsNullOrWhiteSpace(portalOwner))
        {
            ApiPortalPageDTO portal = GetPortalPage(bridge, pageId);
            string currentSorting = bridge.Storage.LoadString(GetPagePathName(bridge, pageId) + ".content-sorting", WebMapping.Core.Api.IO.StorageBlobType.Metadata);

            if (portal != null)
            {
                if (UserManagement.IsAllowed(bridge.CurrentUser.Username, portal.ContentAuthors) || UserManagement.IsAllowed(bridge.CurrentUser.UserRoles, portal.ContentAuthors))
                {
                    bridge.Storage.Save(
                        GetPagePathName(bridge, pageId) + ".content-sorting",
                        JointSorting(currentSorting, sorting), WebMapping.Core.Api.IO.StorageBlobType.Metadata);

                    return new ApiRawStringEventResponse("{\"success\":true}", "application/json");
                }
            }
        }

        return new ApiRawStringEventResponse("{\"success\":false}", "application/json");
    }

    [ServerToolCommand("upload-content-image")]
    public ApiEventResponse OnUploadContentImage(IBridge bridge, ApiToolEventArguments e)
    {
        string pageId = e["page-id"];
        string contentId = bridge.SecurityDecryptString(e["page-content-id"]);
        byte[] data = Convert.FromBase64String(bridge.SecurityDecryptString(e["page-content-imagedata"]));

        string portalOwner = PageOwnerName(bridge, pageId);

        if (!String.IsNullOrWhiteSpace(portalOwner))
        {
            ApiPortalPageDTO portal = GetPortalPage(bridge, pageId);
            if (portal != null)
            {
                if (UserManagement.IsAllowed(bridge.CurrentUser.Username, portal.ContentAuthors) || UserManagement.IsAllowed(bridge.CurrentUser.UserRoles, portal.ContentAuthors))
                {
                    string imagePath = GetPagePathName(bridge, pageId) + ".content-" + contentId + ".image-" + Guid.NewGuid().ToString("N").ToLower();
                    bridge.Storage.Save(
                        imagePath,
                        data, WebMapping.Core.Api.IO.StorageBlobType.Data);

                    return new ApiRawStringEventResponse(bridge.SecurityEncryptString(pageId + @"/" + imagePath), "text/html");
                }
            }
        }

        return new ApiRawStringEventResponse(String.Empty, "text/html");
    }

    [ServerToolCommand("content-image")]
    public ApiEventResponse OnContentImage(IBridge bridge, ApiToolEventArguments e)
    {
        string id = e["id"];
        string imageId = bridge.SecurityDecryptString(id);

        byte[] data = null;
        if (bridge.Storage.Exists(imageId, WebMapping.Core.Api.IO.StorageBlobType.Data))
        {
            data = bridge.Storage.Load(imageId, WebMapping.Core.Api.IO.StorageBlobType.Data);
        }
        else
        {
            MemoryStream ms = new MemoryStream(Properties.Resources.default_banner);
            data = ms.ToArray();
        }

        return new ApiRawBytesEventResponse(data, "image/jpeg");
    }

    [ServerToolCommand("remove-content")]
    public ApiEventResponse OnRemoveContent(IBridge bridge, ApiToolEventArguments e)
    {
        string pageId = e["page-id"];
        string contentId = bridge.SecurityDecryptString(e["page-content-id"]);

        string portalOwner = PageOwnerName(bridge, pageId);

        if (!String.IsNullOrWhiteSpace(portalOwner))
        {
            ApiPortalPageDTO portal = GetPortalPage(bridge, pageId);
            string currentSorting = bridge.Storage.LoadString(GetPagePathName(bridge, pageId) + ".content-sorting", WebMapping.Core.Api.IO.StorageBlobType.Metadata);

            if (portal != null)
            {
                if (UserManagement.IsAllowed(bridge.CurrentUser.Username, portal.ContentAuthors) || UserManagement.IsAllowed(bridge.CurrentUser.UserRoles, portal.ContentAuthors))
                {
                    if (bridge.Storage.Remove(
                        GetPagePathName(bridge, pageId) + ".content-" + contentId,
                        WebMapping.Core.Api.IO.StorageBlobType.Data, true))
                    {
                        if (!String.IsNullOrWhiteSpace(currentSorting))
                        {
                            var sorting = new List<string>(currentSorting.Split(','));
                            sorting.Remove(contentId);

                            bridge.Storage.Save(
                                GetPagePathName(bridge, pageId) + ".content-sorting",
                                String.Join(",", sorting), WebMapping.Core.Api.IO.StorageBlobType.Metadata);
                        }

                        return new ApiRawStringEventResponse("{\"success\":true}", "application/json");
                    }
                }
            }
        }

        return new ApiRawStringEventResponse("{\"success\":false}", "application/json");
    }

    [ServerToolCommand("getuseraccess")]
    public ApiEventResponse GetUserAccess(IBridge bridge, ApiToolEventArguments e)
    {
        string pageId = e["page-id"].ToLower();
        string name = e["access-name"];

        string portalOwner = PageOwnerName(bridge, pageId);
        if (portalOwner == bridge.CurrentUser.Username)
        {
            UserAccess userAccess = new UserAccess();

            ApiPortalPageDTO portal = GetPortalPage(bridge, pageId);

            userAccess.NodeAccess.Add(pageId, portal.Users);

            StringBuilder path = new StringBuilder();
            foreach (string node in name.Split('/'))
            {
                if (path.Length > 0)
                {
                    path.Append("\\");
                }

                path.Append(node);

                userAccess.NodeAccess.Add(pageId + "/" + path.ToString().Replace("\\", "/"), bridge.Storage.GetUserAccess(path.ToString(), typeof(Publish)));
            }

            string jsonString = JSerializer.Serialize(userAccess);
            return new ApiRawStringEventResponse(jsonString, "application/json");
        }
        else
        {
            throw new Exception("Current user is not portal owner");
        }
    }

    [ServerToolCommand("setuseraccess")]
    public ApiEventResponse SetUserAccess(IBridge bridge, ApiToolEventArguments e)
    {
        string pageId = e["page-id"];
        string name = e["access-name"];
        string ua = e["ua"];

        string portalOwner = PageOwnerName(bridge, pageId);
        if (portalOwner == bridge.CurrentUser.Username)
        {
            bridge.Storage.SetUserAccess(name, ua.Split(','), typeof(Publish));

            return new ApiRawStringEventResponse("{\"success\":true}", "application/json");
        }
        else
        {
            throw new Exception("Current user is not portal owner");
        }
    }

    #region Helpers

    private string PageOwnerName(IBridge bridge, string id)
    {
        string owner = bridge.Storage.GetUniqueIndexItem(IndexIds, id);

        if (!String.IsNullOrWhiteSpace(owner))
        {
            return owner;
        }

        // Nur noch zum Kompatibilität. Der Code sollte in zukunft gar nicht mehr aufgerufen werden!!!
        //Dictionary<string, string[]> names = bridge.Storage.GetAllNames();
        //foreach (var user in names.Keys)
        //{
        //    if (names[user].Contains(id))
        //    {
        //        return user;
        //    }
        //}

        return String.Empty;
    }

    private string GetPagePathName(IBridge bridge, string pageId)
    {
        if (!String.IsNullOrWhiteSpace(bridge.CurrentEventArguments["page-id"]))
        {
            return pageId;
        }

        return pageId + @"/" + pageId;
    }

    private ApiPortalPageDTO GetPortalPage(IBridge bridge, string pageId)
    {
        string portalJson = bridge.Storage.LoadString(GetPagePathName(bridge, pageId));

        if (String.IsNullOrWhiteSpace(portalJson))
        {
            return null;
        }

        return JSerializer.Deserialize<ApiPortalPageDTO>(portalJson);
    }

    /// <summary>
    /// Falls mehrer Anwender gleichzeitig Inhalte einfügen, wird so gewährleistet, dass nicht ein Item verloren geht!
    /// </summary>
    /// <param name="currentSorting"></param>
    /// <param name="sorting"></param>
    /// <returns></returns>
    private string JointSorting(string currentSorting, string sorting)
    {
        if (sorting == null)
        {
            sorting = String.Empty;
        }

        if (String.IsNullOrWhiteSpace(currentSorting))
        {
            return sorting;
        }

        List<string> sortingItems = new List<string>(sorting.Split(','));

        foreach (string currentItem in currentSorting.Split(','))
        {
            if (String.IsNullOrWhiteSpace(currentItem))
            {
                continue;
            }

            if (!sortingItems.Contains(currentItem))
            {
                sortingItems.Add(currentItem);
            }
        }

        while (sortingItems.Contains(""))
        {
            sortingItems.Remove("");
        }

        while (sortingItems.Contains("undefined"))  // kann beim Sortieren am Client vorkommen
        {
            sortingItems.Remove("undefined");
        }

        return String.Join(",", sortingItems.ToArray());
    }


    #endregion

    #region Classes

    public class ContentMeta
    {
        [JsonProperty(PropertyName = "creator")]
        [System.Text.Json.Serialization.JsonPropertyName("creator")]
        public string Creator { get; set; }

        [JsonProperty(PropertyName = "lastchange")]
        [System.Text.Json.Serialization.JsonPropertyName("lastchange")]
        public string LastChange { get; set; }
    }

    public class UserAccess
    {
        public UserAccess()
        {
            this.NodeAccess = new Dictionary<string, string[]>();
        }

        [JsonProperty(PropertyName = "nodeAccess")]
        [System.Text.Json.Serialization.JsonPropertyName("nodeAccess")]
        public Dictionary<string, string[]> NodeAccess { get; set; }
    }

    #endregion

    #endregion Commands
}