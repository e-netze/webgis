using E.Standard.Json;
using E.Standard.WebGIS.Core.Extensions;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Models;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image.Extensions;

static public class BridgeExtensions
{
    static public IEnumerable<GeorefImageMetadata> GetGeorefImageMetatas(this IBridge bridge)
    {
        List<GeorefImageMetadata> result = new List<GeorefImageMetadata>();

        foreach (string name in bridge.Storage.GetNames(includeDirectories: false))
        {
            if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(JSerializer.Deserialize<GeorefImageMetadata>(bridge.Storage.LoadString(name)));
            }
        }

        return result;
    }

    static public string GenerateGeorefImageId(this IBridge bridge)
    {
        return $"{typeof(OutStream).ToToolId()}:{bridge.SecurityEncryptString(bridge.CurrentUser.Username + ":" + Guid.NewGuid().ToString("N").ToLower())}";
    }

    static public GeorefImageMetadata GetGeorefImageMetata(this IBridge bridge, string id)
    {
        var metaJsonString = bridge.Storage.LoadString($"{id.GeorefImageIdToStorageName()}.meta");
        if (!String.IsNullOrEmpty(metaJsonString))
        {
            return JSerializer.Deserialize<GeorefImageMetadata>(metaJsonString);
        }

        return null;
    }

    static public GeorefImageMetadata GetGeorefImageMetata(this IBridge bridge, string username, string id)
    {
        var metaJsonString = bridge.Storage.LoadString($"{username.Username2StorageDirectory()}/_georefimages/{id.GeorefImageIdToStorageName()}.meta");
        if (!String.IsNullOrEmpty(metaJsonString))
        {
            return JSerializer.Deserialize<GeorefImageMetadata>(metaJsonString);
        }

        return null;
    }

    static public string GetGeorefImageOwner(this IBridge bridge, string id)
    {
        if (!string.IsNullOrEmpty(id) && id.Contains(":"))
        {
            var usernameTitle = bridge.SecurityDecryptString(id.Split(':')[1]);
            if (usernameTitle.Contains(":"))
            {
                return usernameTitle.Substring(0, usernameTitle.LastIndexOf(":"));
            }
        }

        throw new Exception("Invalid georefimage-id");
    }

    static public byte[] GetGeorefImageData(this IBridge bridge, GeorefImageMetadata georefImageMetadata)
    {
        return bridge.Storage.Load($"{georefImageMetadata.Id.GeorefImageIdToStorageName()}.{georefImageMetadata.ImageExtension}", WebMapping.Core.Api.IO.StorageBlobType.Data);
    }

    static public byte[] GetGeorefImageData(this IBridge bridge, string owner, GeorefImageMetadata georefImageMetadata)
    {
        return bridge.Storage.Load($"{owner.Username2StorageDirectory()}/_georefimages/{georefImageMetadata.Id.GeorefImageIdToStorageName()}.{georefImageMetadata.ImageExtension}", WebMapping.Core.Api.IO.StorageBlobType.Data);
    }

    static public string GeorefImageUrl(this IBridge bridge, GeorefImageMetadata georefImageMetadata)
    {
        return bridge.GeorefImageUrl(bridge.CurrentUser.Username, georefImageMetadata);
    }

    static public string GeorefImageUrl(this IBridge bridge, string owner, GeorefImageMetadata georefImageMetadata)
    {
        if (bridge == null || georefImageMetadata == null)
        {
            return String.Empty;
        }

        string filePath = $"{owner.Username2StorageDirectory()}/_georefimages/{georefImageMetadata.Id.GeorefImageIdToStorageName()}.{georefImageMetadata.ImageExtension}";

        return $"{bridge.AppRootUrl}/rest/tooldata/{typeof(OutStream).ToToolId()}/get?id={bridge.SecurityEncryptString(filePath)}";
    }

    static public string GeorefImageUrl(this IBridge bridge, string filename)
    {
        if (bridge == null || String.IsNullOrEmpty(filename))
        {
            return String.Empty;
        }

        string filePath = $"{bridge.CurrentUser.Username.Username2StorageDirectory()}/_georefimages/{filename}";

        return $"{bridge.AppRootUrl}/rest/tooldata/{typeof(OutStream).ToToolId()}/get?id={bridge.SecurityEncryptString(filePath)}";
    }

    static public string GeorefImageUrlToStorageName(this IBridge bridge, string url)
    {
        if (bridge == null || String.IsNullOrEmpty(url) || !url.Contains("?id="))
        {
            return String.Empty;
        }

        var filePathEncrypted = url.Substring(url.IndexOf("?id=") + 4);

        return bridge.SecurityDecryptString(filePathEncrypted).Replace("\\", "/").Split('/').Last();
    }

    static public string GeorefImageIdToStorageName(this string georefImageId)
    {
        return $"georefimage-{georefImageId.Hash256String()}";
    }

    static private string Hash256String(this string inputString)
    {
        using (HashAlgorithm algorithm = SHA256.Create())
        {
            var data = algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }
    }

    static public string ToTempFilename(this Guid guid) => $"tempfile-{guid.ToString().ToLower()}.tmp";
    static public bool IsTemFilename(this string filename) => filename.StartsWith("tempfile-");
}
