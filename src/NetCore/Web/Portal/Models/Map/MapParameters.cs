using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.WebGIS.Core.Models;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Geometry;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Portal.Core.AppCode.Exceptions;
using Portal.Core.AppCode.Services.WebgisApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using E.Standard.Extensions.Formatting;

namespace Portal.Core.Models.Map;

public class MapParameters
{
    private static readonly string[] KnownParametes = new string[]
    {
        "bbox",
        "scale",
        "center",
        "marker",
        "markers",
        "filter",
        "query", "abfragethema", "query2", "abfragethema2",
        "presentation", "darstellungsvariante",
        "showlayers", "sichtbar",
        "snapshot",
        "hidelayers", "unsichtbar", "basemap",
        "append-services","gdiservices"
    };

    private MapParameters()
    {

    }

    async static public Task<MapParameters> CreateAsync(HttpContext context, WebgisApiService api)
    {
        var request = context.Request;

        var mapParameters = new MapParameters();

        #region Parse Parameters

        #region BBox

        mapParameters.BBox = null;

        if (!String.IsNullOrWhiteSpace(request.Query["bbox"]))
        {
            string[] bboxParts = request.Query["bbox"].ToString().Replace(";", ",").Split(',');
            if (bboxParts.Length == 4)
            {
                mapParameters.BBox = new double[]
                {
                        bboxParts[0].ToPlatformDouble(),
                        bboxParts[1].ToPlatformDouble(),
                        bboxParts[2].ToPlatformDouble(),
                        bboxParts[3].ToPlatformDouble()
                };

                if (!String.IsNullOrWhiteSpace(request.Query["srs"]))
                {
                    var projResult = await api.Project(context, new ProjectionServiceArgumentDTO(int.Parse(request.Query["srs"]), 4326,
                        new Envelope(mapParameters.BBox[0],
                                     mapParameters.BBox[1],
                                     mapParameters.BBox[2],
                                     mapParameters.BBox[3])));

                    if (projResult != null)
                    {
                        mapParameters.BBox[0] = projResult.Envelope.MinX;
                        mapParameters.BBox[1] = projResult.Envelope.MinY;
                        mapParameters.BBox[2] = projResult.Envelope.MaxX;
                        mapParameters.BBox[3] = projResult.Envelope.MaxY;
                    }
                }
            }
        }

        #endregion

        #region Scale

        mapParameters.Scale = 0;
        if (!String.IsNullOrWhiteSpace(request.Query["scale"]))
        {
            mapParameters.Scale = request.Query["scale"].ToPlatformDouble();
        }

        #endregion

        #region Center

        mapParameters.Center = null;

        if (!String.IsNullOrWhiteSpace(request.Query["center"]))
        {
            string[] centerParts = request.Query["center"].ToString().Replace(";", ",").Split(',');
            if (centerParts.Length == 2)
            {
                mapParameters.Center = new double[]
                {
                        centerParts[0].ToPlatformDouble(),
                        centerParts[1].ToPlatformDouble()
                };

                if (!String.IsNullOrWhiteSpace(request.Query["srs"]))
                {
                    var projResult = await api.Project(context, new ProjectionServiceArgumentDTO(int.Parse(request.Query["srs"]), 4326,
                        new Point(mapParameters.Center[0],
                                  mapParameters.Center[1])));

                    if (projResult?.Point is null)
                    {
                        throw new Exception($"Internal Error: Can't project coordinates to {request.Query["srs"]}");
                    }

                    mapParameters.Center[0] = projResult.Point.X;
                    mapParameters.Center[1] = projResult.Point.Y;
                }
            }
        }

        #endregion

        #region Marker 

        mapParameters.Marker = null;
        if (!String.IsNullOrWhiteSpace(request.Query["marker"].ToStringOrEmptyIfMalware()))
        {
            var markerJson = request.Query["marker"].ToString().FixToStrictJson();
            var mapMarker = JSerializer.Deserialize<MapMarkerDTO>(markerJson);

            await PrepareMapMarkers(new MapMarkerDTO[] { mapMarker }, context, api);

            mapParameters.Marker = mapMarker;
        }

        #endregion

        #region Markers

        if (!String.IsNullOrWhiteSpace(request.Query["markers"].ToStringOrEmptyIfMalware()))
        {
            var markersJson = request.Query["markers"].ToString().FixToStrictJson();
            var mapMarkers = JSerializer.Deserialize<MapMarkerDTO[]>(markersJson);

            await PrepareMapMarkers(mapMarkers, context, api);

            mapParameters.Markers = mapMarkers;
            mapParameters.MarkersName = request.Query["markers_name"].ToStringOrEmptyIfMalware();
        }

        #endregion

        #region Snapshot

        if (!String.IsNullOrEmpty(request.Query["snapshot"]))
        {
            mapParameters.Snapshot = request.Query["snapshot"].ToStringOrEmptyIfMalware();
        }

        #endregion

        #region Filters

        List<VisFilterDefinitionDTO> visFilters = new List<VisFilterDefinitionDTO>();

        if (!String.IsNullOrWhiteSpace(request.Query["filter"].ToStringOrEmptyIfMalware()))
        {
            var filterArguments = new List<VisFilterDefinitionDTO.VisFilterDefinitionArgument>();

            string filterId = request.Query["filter"];
            if (!filterId.Contains("~") && !String.IsNullOrWhiteSpace(request.Query["filterservice"]))
            {
                filterId = $"{request.Query["filterservice"]}~{filterId}";
            }

            foreach (var key in request.Query.Keys)
            {
                if (key.StartsWith("filterarg_"))
                {
                    filterArguments.Add(new VisFilterDefinitionDTO.VisFilterDefinitionArgument()
                    {
                        Name = key.Substring(10),
                        Value = request.Query[key]
                    });
                }
            }

            visFilters.Add(new VisFilterDefinitionDTO()
            {
                Id = filterId,
                Arguments = filterArguments.ToArray()
            });
        }

        mapParameters.Filters = visFilters.Count == 0 ? null : visFilters.ToArray();

        #endregion

        #region Query

        mapParameters.Query = request.Query["query"].ToStringOrEmptyIfMalware()
                            .OrTake(request.Query["abfragethema"].ToStringOrEmptyIfMalware());

        mapParameters.QueryThemeId = request.Query["query2"].ToStringOrEmptyIfMalware()
                            .OrTake(request.Query["abfragethema2"].ToStringOrEmptyIfMalware())
                            .OrTake(request.Query["querythemeid"].ToStringOrEmptyIfMalware());

        #endregion

        #region Edit Theme

        mapParameters.EditThemeId = request.Query["editthemeid"].ToStringOrEmptyIfMalware();

        #endregion

        #region Tools

        mapParameters.ToolId = request.Query["tool"].ToStringOrEmptyIfMalware();

        #endregion

        #region EditFieldValues 

        foreach (string key in request.Query.Keys)
        {
            if (key.StartsWith("ed_") && !String.IsNullOrEmpty(key.ToStringOrEmptyIfMalware()))
            {
                if (mapParameters.EditFieldValues == null)
                {
                    mapParameters.EditFieldValues = new Dictionary<string, string>();
                }

                mapParameters.EditFieldValues[key.Substring(3)] = request.Query[key];
            }
        }

        #endregion

        #region Presentations

        string prenstationsParameter = !String.IsNullOrEmpty(request.Query["presentation"]) ?
            request.Query["presentation"].ToStringOrEmptyIfMalware() :
            request.Query["darstellungsvariante"].ToStringOrEmptyIfMalware();
        mapParameters.Presentations = String.IsNullOrWhiteSpace(prenstationsParameter) ? null : prenstationsParameter.Split(',')
                                                                                                                     .Select(p => p.Trim()).ToArray();

        #endregion

        #region Layers

        string showLayersParameter = request.Query["showlayers"].ToStringOrEmptyIfMalware().OrTake(request.Query["sichtbar"].ToStringOrEmptyIfMalware());
        mapParameters.ShowLayers = String.IsNullOrEmpty(showLayersParameter) ? null : showLayersParameter.Split(',')
                                                                                                         .Select(p => p.Trim()
                                                                                                                       //.Replace("/", "\\\\")
                                                                                                                       .Replace(@"\", @"\\")
                                                                                                                       ).ToArray();

        string hideLayersParameter = request.Query["hidelayers"].ToStringOrEmptyIfMalware().OrTake(request.Query["unsichtbar"].ToStringOrEmptyIfMalware());
        mapParameters.HideLayers = String.IsNullOrEmpty(hideLayersParameter) ? null : hideLayersParameter.Split(',')
                                                                                                         .Select(p => p.Trim()
                                                                                                                         //.Replace("/", "\\\\")
                                                                                                                         .Replace(@"\", @"\\")
                                                                                                                         ).ToArray();

        #endregion

        #region Basemap

        mapParameters.Basemap = request.Query["basemap"].ToStringOrEmptyIfMalware().OrTake<string>(null);

        #endregion

        #region (GDI) Services

        string appendServices = request.Query["append-services"].ToStringOrEmptyIfMalware()
                                       .OrTake(request.Query["gdiservices"].ToStringOrEmptyIfMalware());
        mapParameters.AppendServices = appendServices?.OrTake<string>(null);

        #endregion

        #region (GDI) Serarch Services

        string appendSearchServices = request.Query["append-search-services"].ToStringOrEmptyIfMalware()
                                             .OrTake(request.Query["gdisearchservices"].ToStringOrEmptyIfMalware());
        mapParameters.AppendSearchServices = appendSearchServices?.OrTake<string>(null);

        #endregion

        #region Others

        mapParameters.OrigianParameters = new Dictionary<string, string>();
        foreach (string key in request.Query.Keys)
        {
            if (string.IsNullOrEmpty(key.ToStringOrEmptyIfMalware()))
            {
                continue;
            }

            if (KnownParametes.Where(p => p.ToLower() == key.ToLower()).Count() > 0)
            {
                continue;
            }

            mapParameters.OrigianParameters[key] = request.Query[key].ToStringOrEmptyIfMalware();
        }

        #endregion

        #endregion

        return mapParameters;
    }

    [JsonProperty(PropertyName = "bbox")]
    [System.Text.Json.Serialization.JsonPropertyName("bbox")]
    public double[] BBox { get; set; }

    [JsonProperty(PropertyName = "center")]
    [System.Text.Json.Serialization.JsonPropertyName("center")]
    public double[] Center { get; set; }

    [JsonProperty(PropertyName = "scale")]
    [System.Text.Json.Serialization.JsonPropertyName("scale")]
    public double Scale { get; set; }

    [JsonProperty(PropertyName = "filters", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("filters")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<VisFilterDefinitionDTO> Filters { get; set; }

    [JsonProperty(PropertyName = "marker")]
    [System.Text.Json.Serialization.JsonPropertyName("marker")]
    public MapMarkerDTO Marker { get; set; }

    [JsonProperty(PropertyName = "markers", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("markers")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<MapMarkerDTO> Markers { get; set; }

    [JsonProperty(PropertyName = "markers_name", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("markers_name")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string MarkersName { get; set; }

    [JsonProperty(PropertyName = "query", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("query")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Query { get; set; }

    [JsonProperty(PropertyName = "querythemeid", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("querythemeid")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string QueryThemeId { get; set; }

    [JsonProperty(PropertyName = "editthemeid")]
    [System.Text.Json.Serialization.JsonPropertyName("editthemeid")]
    public string EditThemeId { get; set; }

    [JsonProperty(PropertyName = "toolid")]
    [System.Text.Json.Serialization.JsonPropertyName("toolid")]
    public string ToolId { get; set; }

    [JsonProperty(PropertyName = "editfieldvalues")]
    [System.Text.Json.Serialization.JsonPropertyName("editfieldvalues")]
    public Dictionary<string, string> EditFieldValues { get; private set; }

    [JsonProperty(PropertyName = "presentations", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("presentations")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] Presentations { get; set; }

    [JsonProperty(PropertyName = "showlayers", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("showlayers")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] ShowLayers { get; set; }
    [JsonProperty(PropertyName = "hidelayers", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("hidelayers")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] HideLayers { get; set; }

    [JsonProperty(PropertyName = "snapshot_name", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("snapshot_name")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Snapshot { get; set; }

    [JsonProperty(PropertyName = "basemap", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("basemap")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Basemap { get; set; }

    [JsonProperty(PropertyName = "appendservices", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("appendservices")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string AppendServices { get; set; }

    [JsonProperty(PropertyName = "appendsearchservices", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("appendsearchservices")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string AppendSearchServices { get; set; }

    [JsonProperty(PropertyName = "original")]
    [System.Text.Json.Serialization.JsonPropertyName("original")]
    public Dictionary<string, string> OrigianParameters { get; set; }

    async static private Task PrepareMapMarkers(IEnumerable<MapMarkerDTO> mapMarkers, HttpContext context, WebgisApiService api)
    {
        foreach (var mapMarker in mapMarkers)
        {
            if (mapMarker.Srs > 0 && mapMarker.Srs != 4326)
            {
                var projResult = await api.Project(context,
                                                   new ProjectionServiceArgumentDTO(mapMarker.Srs, 4326,
                                                   new Point(mapMarker.X, mapMarker.Y)));

                if (projResult?.Point is null)
                {
                    throw new Exception($"Internal Error: Can't project coordinates to {mapMarker.Srs}");
                }

                mapMarker.Lng = projResult.Point.X;
                mapMarker.Lat = projResult.Point.Y;
            }

            if (!String.IsNullOrWhiteSpace(mapMarker.Text))
            {
                string validUrlPattern = @"((([A-Za-z]{3,9}:(?:\/\/)?)(?:[-;:&=\+\$,\w]+@)?[A-Za-z0-9.-]+|(?:www.|[-;:&=\+\$,\w]+@)[A-Za-z0-9.-]+)((?:\/[\+~%\/.\w-_]*)?\??(?:[-\+=&;%@.\w_]*)#?(?:[\w]*))?)";
                string imgPattern = "img:" + validUrlPattern;

                mapMarker.Text = Regex.Replace(mapMarker.Text, imgPattern, match =>
                {
                    return "<br/><img src='" + match.Value.Substring(4) + "' style='width:240px' /><br/>";
                });
            }
        }
    }
}
