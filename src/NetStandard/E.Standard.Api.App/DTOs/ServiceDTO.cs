using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Models;
using E.Standard.Api.App.Models.Abstractions;
using E.Standard.Api.App.Services.Cache;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs;

public sealed class ServiceDTO : VersionDTO, IHtml3
{
    public string id { get; set; }
    public string name { get; set; }
    public string type { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string container { get; set; }

    public int[] supportedCrs { get; set; }

    public bool isbasemap { get; set; }

    public double opacity { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public QueryDTO[] queries { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] childServices { get; set; }

    #region Static Members

    public static ServiceType GetServiceType(IMapService service)
    {
        if (service == null)
        {
            return ServiceType.Unknown;
        }

        ServiceType type = ServiceType.Unknown;
        if (service is WebMapping.GeoServices.Tiling.TileService)
        {
            type = ServiceType.Tile;
        }
        else if (service is WebMapping.GeoServices.Tiling.GeneralVectorTileService)
        {
            type = ServiceType.Vtc;
        }
        else
        {
            switch (service.ResponseType)
            {
                case ServiceResponseType.Image:
                    type = ServiceType.Image;
                    break;
                case ServiceResponseType.Collection:
                    type = ServiceType.Collection;
                    break;
                case ServiceResponseType.StaticOverlay:
                    type = ServiceType.Static_Overlay;
                    break;
                case ServiceResponseType.VectorService:
                    type = ServiceType.Vector;
                    break;
            }
        }

        return type;
    }

    #endregion

    #region IHtml Member

    public string ToHtmlString()
    {
        return ToHtmlString(null);
    }

    public string ToHtmlString(CacheService cache)
    {
        var service = cache?.GetOriginalServiceIfInitialized(this.id, null);

        string color = "", background = "";
        if (service != null)
        {
            if (service.HasInitialzationErrors())
            {
                background = "#ffaaaa";
                color = "#fff";
            }
        }
        else
        {
            background = "#b5dbad4a";
        }

        return HtmlHelper.ToNextLevelLink(this.id, this.name, background: background, color: color);
    }

    #endregion
}