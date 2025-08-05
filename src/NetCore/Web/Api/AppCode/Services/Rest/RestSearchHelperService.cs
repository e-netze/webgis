using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using E.Standard.Api.App;
using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Extensions.Compare;
using E.Standard.GeoCoding.GeoCode;
using E.Standard.Platform;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Rest;

public class RestSearchHelperService
{
    private readonly UrlHelperService _urlHelper;
    private readonly RestQueryHelperService _restQueryHelper;
    private readonly CacheService _cache;
    private readonly RestSearchHelperServiceOptions _config;
    private readonly IHttpService _http;

    public RestSearchHelperService(UrlHelperService urlHelper,
                                   RestQueryHelperService restQueryHelper,
                                   CacheService cache,
                                   IOptions<RestSearchHelperServiceOptions> config,
                                   IHttpService http)
    {
        _urlHelper = urlHelper;
        _restQueryHelper = restQueryHelper;
        _cache = cache;
        _config = config.Value;
        _http = http;
    }

    async public Task<IActionResult> PerformSearchServiceAsync(ApiBaseController controller,
                                                               IEnumerable<ISearchService> services,
                                                               NameValueCollection form,
                                                               CmsDocument.UserIdentification ui)
    {
        var watch = new StopWatch(String.Empty);

        var httpRequest = controller.Request;

        int rows = form["rows"] != null ? int.Parse(form["rows"]) : -1;
        string term = form["term"];

        SearchServiceItems items = new SearchServiceItems();
        List<SearchServiceItem> searchItemsList = new List<SearchServiceItem>();

        if (String.IsNullOrWhiteSpace(term))
        {
            #region Metadata

            StringBuilder sb = new StringBuilder();
            string[] categories = !String.IsNullOrEmpty(httpRequest.Query["categories"]) ?
                                                        httpRequest.Query["categories"].ToString().Split(',') :
                                                        null;
            int metaCounter = 0;

            foreach (var service in services)
            {
                if (service is ISearchService2)
                {
                    foreach (var metadataType in await ((ISearchService2)service).GetTypesMetadataAsync(_http))
                    {
                        if (categories != null && !categories.Contains(metadataType.TypeName))
                        {
                            continue;
                        }

                        if (metaCounter < 10)
                        {
                            if (sb.Length > 0)
                            {
                                sb.Append(", ");
                            }

                            sb.Append(metadataType.TypeName);
                        }
                        metaCounter++;
                    }
                }
            }
            if (metaCounter > 10)
            {
                sb.Append($" + {(metaCounter - 10)} weitere Themen...");
            }

            if (!String.IsNullOrWhiteSpace(sb.ToString()))
            {
                searchItemsList.Add(new SearchServiceItem(null)
                {
                    Id = "#metadata",
                    SuggestText = String.Empty,
                    Subtext = sb.ToString(),
                    ThumbnailUrl = $"{_urlHelper.AppRootUrl(HttpSchema.Empty)}/content/api/img/help-24-b.png"
                });
            }

            if (_config.AllowGeoCodesInput)
            {
                StringBuilder sbGeoCodes = new StringBuilder();
                foreach (var geoCode in GeoLocation.Implentations)
                {
                    if (sbGeoCodes.Length > 0)
                    {
                        sbGeoCodes.Append(", ");
                    }

                    sbGeoCodes.Append(geoCode.DisplayName);
                }

                if (!String.IsNullOrWhiteSpace(sbGeoCodes.ToString()))
                {
                    searchItemsList.Add(new SearchServiceItem(null)
                    {
                        Id = "#metadata-geocodes",
                        SuggestText = String.Empty,
                        Subtext = $"GeoCodes: {sbGeoCodes}",
                        ThumbnailUrl = $"{_urlHelper.AppRootUrl(HttpSchema.Empty)}/content/api/img/grid-24-b.png"
                    });
                }
            }

            //if (searchItemsList.Count == 0)
            //{
            //    return null;
            //}

            items.Items = searchItemsList.Take(rows <= 0 ? 5 : rows);

            //("RestController PerformSearch: sleep 500").LogLine();
            await Task.Delay(500);

            #endregion
        }
        else
        {
            #region Serarch 

            foreach (var service in services)
            {
                try
                {
                    if (service is ISearchService3)
                    {
                        string[] categories = String.IsNullOrEmpty(form["categories"]) ? null : form["categories"].Split(',');
                        Envelope queryBBox = String.IsNullOrEmpty(form["bbox"]) ?
                            null :
                            new Envelope(form["bbox"].Split(',').Select(n => n.ToPlatformDouble()).ToArray());

                        var searchItems = await ((ISearchService3)service).Query3Async(_http, term, rows, categories, queryBBox);
                        if (searchItems != null)
                        {
                            searchItemsList.AddRange(searchItems.Items);
                        }
                    }
                    else if (service is ISearchService2)
                    {
                        string[] categories = String.IsNullOrEmpty(form["categories"]) ? null : form["categories"].Split(',');

                        var searchItems = await ((ISearchService2)service).Query2Async(_http, term, rows, categories);
                        if (searchItems != null)
                        {
                            searchItemsList.AddRange(searchItems.Items);
                        }
                    }
                    else
                    {
                        var searchItems = await service.QueryAsync(_http, term, rows);
                        if (searchItems != null)
                        {
                            searchItemsList.AddRange(searchItems.Items);
                        }
                    }
                }
                catch (Exception ex)
                {
                    searchItemsList.Add(new SearchServiceItem(service)
                    {
                        Id = "0",
                        SuggestText = "Serarch Service Exception (" + service.Name + ")",
                        Subtext = ex.Message
                    });
                }
            }

            if (services.Count() > 1)
            {
                Bridge.SortSearchItemByScore.Score(searchItemsList, term);
            }

            if (searchItemsList.Where(i => i.Score != 0D).Count() > 0)
            {
                searchItemsList.Sort(new Bridge.SortSearchItemByScore());
            }

            items.Items = searchItemsList.Take(rows <= 0 ? _config.MaxResultItems : rows).ToArray();
            searchItemsList.Clear();

            #endregion

            #region GeoCodes

            if (_config.AllowGeoCodesInput)
            {

                foreach (var geoCoder in GeoLocation.ValidGeoCoders(term))
                {
                    var geoLocation = geoCoder.Decode(term);
                    if (String.IsNullOrEmpty(geoLocation.ErrorMessage))
                    {
                        searchItemsList.Add(new SearchServiceItem(null)
                        {
                            Id = "",
                            SuggestText = term,
                            Subtext = $"{geoCoder.DisplayName}, {geoLocation.Latitude.ToPlatformNumberString()} {geoLocation.Longitude.ToPlatformNumberString()} ",
                            ThumbnailUrl = $"{_urlHelper.AppRootUrl(HttpSchema.Empty)}/content/api/img/grid-24-b.png",
                            Geometry = new Point(geoLocation.Longitude, geoLocation.Latitude),
                            BBox =
                                geoLocation.NorthEast != null && geoLocation.SouthWest != null ?
                                new double[] { geoLocation.SouthWest.Longitude, geoLocation.SouthWest.Latitude, geoLocation.NorthEast.Longitude, geoLocation.NorthEast.Latitude } :
                                null
                        });
                    }
                }

                if (searchItemsList.Count > 0)
                {
                    items.Items = new List<SearchServiceItem>(items.Items);
                    ((List<SearchServiceItem>)items.Items).AddRange(searchItemsList);
                }

            }

            #endregion
        }

        if (form["f"] == "geojson")
        {
            FeatureCollection features = new FeatureCollection();
            int counter = 1;
            foreach (var item in items.Items)
            {
                var feature = new E.Standard.WebMapping.Core.Feature();

                feature.GlobalOid = /*!String.IsNullOrWhiteSpace(item.Id) ? item.Id :*/ "#service:#default:" + counter++;
                if (item.Geometry != null)
                {
                    feature.Shape = item.Geometry;
                    if (item.BBox != null && item.BBox.Length == 4)
                    {
                        feature.ZoomEnvelope = new Envelope(item.BBox[0], item.BBox[1], item.BBox[2], item.BBox[3]);
                    }
                    else
                    {
                        feature.ZoomEnvelope = feature.Shape.ShapeEnvelope;
                        feature.ZoomEnvelope.Resize(0.0005);
                    }
                }

                feature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("_fulltext", "<table><tr>" +
                    (String.IsNullOrEmpty(item.ThumbnailUrl) ? "" : "<td><img style='max-height:40px' src='" + item.ThumbnailUrl + "' /></td>")
                    + "<td style='vertical-align:top'><span>" + item.SuggestText + "</span><br/><span style='color:#aaa;font-size:0.8em'>" + item.Subtext + "</span></td></tr></table>"));

                feature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("_label", item.SuggestText));

                if (item.Service is ISearchService2 && !String.IsNullOrWhiteSpace(item.Id) && item.Id.Contains("."))
                {
                    feature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("_details", "/rest/search/" + item.Service.Id + "?c=original&render_fields=true&item_id=" + item.Id));
                }

                features.Add(feature);
            }

            // Sorting is better from serarch service scoring.
            //if (rows > 20)
            //{
            //    features = new FeatureCollection(features.OrderBy(f => f.Attributes["_label"]?.Value));
            //}

            return await controller.JsonObject(watch.Apply<FeaturesDTO>(new FeaturesDTO(features, FeaturesDTO.Meta.Tool.Search)));
        }
        else
        {
            List<AutocompleteItemDTO> ret = new List<AutocompleteItemDTO>();
            foreach (var item in items.Items)
            {
                ret.Add(new AutocompleteItemDTO(item));
            }

            return await controller.JsonObject(ret.ToArray());
        }
    }

    async public Task<IActionResult> PerformSearchServiceItemOriginal(ApiBaseController controller,
                                                                      IEnumerable<ISearchService> services,
                                                                      NameValueCollection form,
                                                                      CmsDocument.UserIdentification ui)
    {
        string itemId = form["item_id"];
        if (String.IsNullOrWhiteSpace(itemId) || !itemId.Contains("."))
        {
            return await controller.JsonViewSuccess(false, "invalid item id");
        }

        bool renderFields = form["render_fields"] != "false";
        QueryGeometryType geometryType = form["full_geometry"] == "true" ? QueryGeometryType.Full : QueryGeometryType.Simple;  // full_geometry ... Kompatibilität!!!
        geometryType = form["geometry"].ToQueryGeometryType(geometryType);

        string metaId = itemId.Split('.')[0];
        string featureId = itemId.Split('.')[1];

        SearchTypeMetadata metadata = null;
        foreach (var service in services)
        {
            if (service is ISearchService2)
            {
                metadata = await ((ISearchService2)service).GetTypeMetadataAsync(_http, metaId);
                if (metadata != null)
                {
                    break;
                }
            }
        }

        if (metadata == null)
        {
            return await controller.JsonViewSuccess(false, "no type metadata found");
        }

        if (String.IsNullOrWhiteSpace(metadata.ServiceId) || String.IsNullOrWhiteSpace(metadata.QueryId))
        {
            return await controller.JsonViewSuccess(false, "no service or query in metadata definied");
        }

        NameValueCollection parameters = new NameValueCollection();
        parameters.Add("#oid#", featureId);
        return await _restQueryHelper.PerformQueryAsync(controller,
                                                        metadata.ServiceId,
                                                        metadata.QueryId,
                                                        parameters,
                                                        true,
                                                        geometryType,
                                                        ui,
                                                        renderFields,
                                                        srs: form["srs"].OrTake<int>(_config.DefaultQuerySrefId));
    }

    async public Task<IActionResult> PerformSearchServiceMetaAsync(ApiBaseController controller,
                                                                   IEnumerable<ISearchService> services,
                                                                   CmsDocument.UserIdentification ui)
    {
        List<AutocomplteItemMetadata> metadataList = new List<AutocomplteItemMetadata>();
        var httpRequest = controller.Request;

        try
        {
            string[] categories = !String.IsNullOrEmpty(httpRequest.Query["categories"]) ?
                                                        httpRequest.Query["categories"].ToString().Split(',') :
                                                        null;

            foreach (var service in services)
            {
                if (service is ISearchService2)
                {
                    foreach (var metadata in await ((ISearchService2)service).GetTypesMetadataAsync(_http))
                    {
                        if (categories != null && !categories.Contains(metadata.TypeName))
                        {
                            continue;
                        }

                        var metadataItem = new AutocomplteItemMetadata()
                        {
                            Id = metadata.Id,
                            TypeName = metadata.TypeName,
                            Sample = metadata.Sample,
                            Description = metadata.Description
                        };

                        metadataList.Add(metadataItem);
                    }
                }

                if (!String.IsNullOrEmpty(service.CopyrightId))
                {
                    var copyrightInfo = _cache.CopyrightInfo(service.CopyrightId, ui);
                    if (copyrightInfo != null)
                    {
                        var metadataItem = new AutocomplteItemMetadata()
                        {
                            Id = "__copyright",
                            TypeName = "Copyright",
                            CopyrightInfo = copyrightInfo
                        };

                        metadataList.Add(metadataItem);
                    }
                }
            }
        }
        catch { }

        return await controller.JsonObject(metadataList.ToArray());
    }

    async public Task<IActionResult> PerformSearchServiceMetaGeoCodesAsync(ApiBaseController controller,
                                                                     IEnumerable<ISearchService> services,
                                                                     CmsDocument.UserIdentification ui)
    {
        List<AutocomplteItemMetadata> metadataList = new List<AutocomplteItemMetadata>();
        try
        {
            foreach (var geoCode in GeoLocation.Implentations)
            {
                metadataList.Add(new AutocomplteItemMetadata()
                {
                    Id = Guid.NewGuid().ToString(),
                    TypeName = geoCode.DisplayName,
                    Sample = String.Join(";", geoCode.Examples),
                    SampleSeparator = ";",
                    Link = geoCode.Links.FirstOrDefault(),
                    Description = geoCode.Description("de")
                });
            }
        }
        catch { }

        return await controller.JsonObject(metadataList.ToArray());
    }

    async public Task<IActionResult> PerformSearchServiceItemMetaAsync(ApiBaseController controller,
                                                                       IEnumerable<ISearchService> services,
                                                                       NameValueCollection form,
                                                                       CmsDocument.UserIdentification ui)
    {
        string metaId = form["id"].Split('.')[0];

        foreach (var service in services)
        {
            if (service is ISearchService2)
            {
                SearchTypeMetadata metadata = await ((ISearchService2)service).GetTypeMetadataAsync(_http, metaId);
                if (metadata != null)
                {
                    return await controller.JsonObject(new SearchItemMetadataDTO(metadata));
                }
            }
        }

        return await controller.JsonViewSuccess(false, "No metadata found");
    }
}

