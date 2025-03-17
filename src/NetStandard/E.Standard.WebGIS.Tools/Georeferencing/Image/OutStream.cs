using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Extensions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Reflection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image;

[Export(typeof(IApiButton))]
[ToolStorageId("WebGIS.Tools.Serialization")]
[ToolStorageIsolatedUser(false)]
public class OutStream : IApiButton, IApiCustomToolServiceProvider
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
            return "ImageStream";
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

    #region Commands

    [ServerToolCommand("get")]
    public ApiEventResponse OnGet(IBridge bridge, ApiToolEventArguments e)
    {
        string id = e["id"];

        string blobName = bridge.SecurityDecryptString(id);
        string contentType = $"image/{blobName.Split('.').Last().ToLower()}";

        byte[] data = bridge.Storage.Load(blobName, WebMapping.Core.Api.IO.StorageBlobType.Data);

        var response = new ApiRawBytesEventResponse(data, contentType);
        response.AddEtag(DateTime.UtcNow.AddDays(7));

        return response;
    }

    #endregion

    #region IApiCustomToolServiceProvider

    async public Task<IMapService> CreateCustomToolService(IBridge bridge, IMap map, string serviceId)
    {
        string owner = bridge.GetGeorefImageOwner(serviceId);
        var georefImageMetadata = bridge.GetGeorefImageMetata(owner, serviceId);

        if (georefImageMetadata != null)
        {
            var service = new PrintImageService(bridge, owner, georefImageMetadata);
            await service.InitAsync(map, null);

            return service;
        }

        return null;
    }

    #endregion
}
