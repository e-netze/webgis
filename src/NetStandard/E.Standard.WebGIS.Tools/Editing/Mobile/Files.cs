using E.Standard.Drawing.Pro;
using E.Standard.Json;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.IO;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Editing.Mobile;

[Export(typeof(IApiButton))]
[ToolStorageIsolatedUser(false)]
[ToolId("webgis.tools.editing.files")]
public class Files : IApiButton
{
    #region IApiButton

    public string Container
    {
        get
        {
            return String.Empty;
        }
    }

    public bool HasUI
    {
        get
        {
            return false;
        }
    }

    public string Image
    {
        get
        {
            return String.Empty;
        }
    }

    public string Name
    {
        get
        {
            return "Files";
        }
    }

    public string ToolTip
    {
        get
        {
            return String.Empty;
        }
    }

    #endregion

    private readonly int[] _thumbNailSizes = new int[] { 1024, 512, 256, 64 };

    #region Commands

    //[ServerToolCommand("upload")]
    public ApiEventResponse OnUpload(IBridge bridge, ApiToolEventArguments e)
    {
        string themeId = e["edit-themeid"];
        string fieldName = e["edit-field-name"];
        string subDirName = e["subdir-name"];
        string fileName = e["file-name"];
        string contentType = e["file-content-type"];
        byte[] data = Convert.FromBase64String(e["file-data-b64"]);

        Dictionary<int, byte[]> thumbnails = new Dictionary<int, byte[]>();

        ImageMetadata imageMetadata = null;
        if (contentType == "image/jpeg" || contentType == "image/jpg")
        {
            data = ImageOperations.AutoRotate(data, ref imageMetadata);

            var oData = data;
            foreach (var size in _thumbNailSizes)
            {
                thumbnails.Add(size, ImageOperations.Scaledown(oData, size));
                oData = thumbnails[size];
            }
        }

        if (fileName.Contains("/"))
        {
            fileName = fileName.Substring(fileName.LastIndexOf("/"));
        }

        string blobName = themeId + @"\" + fieldName + @"\" + (!String.IsNullOrWhiteSpace(subDirName) ? subDirName + @"\" : String.Empty) + Guid.NewGuid().ToString("N").ToLower();
        bridge.Storage.Save(blobName, data, StorageBlobType.Data);

        foreach (int size in thumbnails.Keys)
        {
            bridge.Storage.Save(blobName + "." + size, thumbnails[size], StorageBlobType.Data);
        }

        bridge.Storage.Save(blobName, JSerializer.Serialize(new FileMeta()
        {
            FileName = fileName,
            ContentType = contentType,
            UserName = bridge.CurrentUser.Username,
            Latitude = imageMetadata != null ? imageMetadata.Latitude : null,
            Longitute = imageMetadata != null ? imageMetadata.Longitute : null,
            DateTimeOriginal = imageMetadata != null ? imageMetadata.DateTimeOriginal : null,
            Thumbnails = thumbnails.Keys.ToArray()
        }), StorageBlobType.Metadata);

        object position = null;
        if (imageMetadata != null && imageMetadata.Latitude != null && imageMetadata.Latitude != null)
        {
            position = new { lat = imageMetadata.Latitude, lng = imageMetadata.Longitute };
        }

        return new ApiRawJsonEventResponse(new
        {
            position = position,
            date = imageMetadata != null ? imageMetadata.DateTimeOriginal : null,
            dateString = imageMetadata != null ? imageMetadata.DateTimeOriginal.ToString() : "",
            contenttype = contentType,
            editfieldname = fieldName,
            url = "/rest/toolmethod/" + this.GetType().ToToolId() + "/load?id=" + bridge.SecurityEncryptString(blobName),
            success = true
        });
    }

    public ApiEventResponse DelteFile(IBridge bridge, ApiToolEventArguments e)
    {
        string url = e["url"];
        int pos = url.IndexOf("?id=");
        if (pos < 0)
        {
            pos = url.IndexOf("&id=");
        }

        if (pos < 0)
        {
            throw new ArgumentException("Can't determine blobname");
        }

        string blobName = bridge.SecurityDecryptString(url.Substring(pos + 4).Split('&')[0]);

        bridge.Storage.Remove(blobName, StorageBlobType.Data, true);
        //bridge.Storage.Remove(blobName, Core.Api.IO.StorageBlobType.Metadata, true);

        return new ApiRawJsonEventResponse(new
        {
            success = true
        });
    }

    [ServerToolCommand("load")]
    public ApiEventResponse OnLoad(IBridge bridge, ApiToolEventArguments e)
    {
        string id = e["id"];
        int size = e.GetInt("size");

        string blobName = bridge.SecurityDecryptString(id);
        string thumbnailSizeExtentsion = String.Empty;

        var fileMetaString = bridge.Storage.LoadString(blobName, StorageBlobType.Metadata);
        if (!String.IsNullOrEmpty(fileMetaString))
        {
            var meta = JSerializer.Deserialize<FileMeta>(fileMetaString);

            int thumbnailSize = 0;
            if (size > 0 && meta.Thumbnails != null && meta.Thumbnails.Length > 0)
            {
                foreach (var s in meta.Thumbnails)
                {
                    if (s >= size)
                    {
                        thumbnailSizeExtentsion = "." + s;
                        thumbnailSize = s;
                    }
                }
            }


            byte[] data = bridge.Storage.Load(blobName + thumbnailSizeExtentsion, StorageBlobType.Data);

            if (size > 0 && thumbnailSize != size)
            {
                data = ImageOperations.Scaledown(data, Math.Max(size, 2));
            }

            string filename = meta.FileName.Replace("\\", "/");
            if (filename.Contains("/"))
            {
                filename = filename.Substring(filename.LastIndexOf("/") + 1);
            }

            NameValueCollection headers = new NameValueCollection();
            headers["Content-Disposition"] = "inline; filename=\"" + filename + "\"";

            return new ApiRawBytesEventResponse(data, meta.ContentType)
            {
                Headers = headers
            };
        }

        throw new FileNotFoundException();
    }

    #endregion

    #region Classes

    private class FileMeta
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string UserName { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double? Longitute { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double? Latitude { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? DateTimeOriginal { get; set; }

        public int[] Thumbnails { get; set; }
    }

    #endregion
}
