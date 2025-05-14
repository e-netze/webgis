using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Models.Abstractions;
using E.Standard.Web.Abstractions;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Api.App.DTOs;

public sealed class FeaturesDTO : VersionDTO, IHtml
{
    public FeaturesDTO()
    {

    }

    public FeaturesDTO(FeatureCollection features,
                    Meta.Tool tool = Meta.Tool.Unknown,
                    bool select = false, FeatureResponseType method = FeatureResponseType.New,
                    string customSelection = null,
                    string[] sortColumns = null)
    {
        if (features != null)
        {
            List<FeatureDTO> featureList = new List<FeatureDTO>();
            this.bounds = this.FeaturesBounds();

            foreach (var feature in features)
            {
                featureList.Add(new FeatureDTO(feature));
            }
            this.features = featureList.ToArray();

            if (bounds == null)
            {
                WebMapping.Core.Geometry.Envelope fBounds = features.ZoomEnvelope;

                if (fBounds != null)
                {
                    bounds = new double[] { fBounds.MinX, fBounds.MinY, fBounds.MaxX, fBounds.MaxY };
                }
            }
        }

        if (tool != Meta.Tool.Unknown || select || features.Links != null || features.TableFieldDefintions != null)
        {
            this.metadata = new Meta()
            {
                ToolType = tool,
                Selected = select,
                CustomSelection = customSelection,
                Links = features.Links,
                LinkTargets = features.LinkTargets,
                TableFields = features.TableFieldDefintions == null ?
                                    null :
                                    features.TableFieldDefintions.Select(f => new TableFieldDefintion()
                                    {
                                        Name = f.Name,
                                        Visible = f.Visible,
                                        SortingAlgorithm = f.SortingAlgorithm,
                                        ImageWidth = f.ImageSize?.width,
                                        ImageHeight = f.ImageSize?.height
                                    }),
                HasAttachments= features.HasAttachments == true ? true : null,
                Warnings = features.Warnings?.ToArray(),
                Informations = features.Informations?.ToArray(),
            };
            this.metadata.HashCode = this.CalcHashCode();
        }

        this.method = method.ToString().ToLower();
        this.SortColumns = sortColumns;
    }

    public string type { get { return "FeatureCollection"; } set { } }
    public FeatureDTO[] features { get; set; }
    public double[] bounds { get; set; }
    public string method { get; set; }

    [JsonProperty("_sortedCols", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("_sortedCols")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] SortColumns { get; set; }

    public double[] FeaturesBounds()
    {
        if (features == null || features.Length == 0)
        {
            return null;
        }

        double[] ret = null;
        foreach (var feature in features)
        {
            if (feature.bounds != null)
            {
                if (ret == null)
                {
                    ret = new double[] { feature.bounds[0], feature.bounds[1], feature.bounds[2], feature.bounds[3] };
                }
                else
                {
                    ret[0] = Math.Min(feature.bounds[0], ret[0]);
                    ret[1] = Math.Min(feature.bounds[1], ret[1]);
                    ret[2] = Math.Max(feature.bounds[2], ret[2]);
                    ret[3] = Math.Max(feature.bounds[3], ret[3]);
                }
            }
        }

        return ret;
    }

    public string title { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Meta metadata { get; set; }

    #region IHtml Member

    public string ToHtmlString()
    {
        return HtmlHelper.ToList(features, "Features (" + (features == null ? 0 : features.Length) + ")");
    }

    #endregion

    //public string ToGeoRSS()
    //{
    //    var rss = new GeoRSS20.rss();
    //    rss.channel = new GeoRSS20.channel();

    //    List<GeoRSS20.item> items = new List<GeoRSS20.item>();

    //    if (this.features != null)
    //    {
    //        foreach (var feature in this.features)
    //        {
    //            var item = new GeoRSS20.item();
    //            item.lat=feature.geometry.coordinates[0]
    //        }
    //    }

    //    return String.Empty;
    //}

    #region Static Memebers

    async static public Task<FeaturesDTO> FromGeoRSS(IHttpService httpService, string geoRssUrl, bool rawAttributes = false)
    {
        //string rssString = GeoRSS20.WebFunctions.DownloadXml(geoRssUrl, new WebProxyHelper(ApiGlobals.AppEtcPath).GetProxy(geoRssUrl), Encoding.UTF8);
        string rssString = await httpService.GetStringAsync(geoRssUrl, encoding: Encoding.UTF8);

        return FromGeoRSS(rssString);
    }

    static public FeaturesDTO FromGeoRSS(string rssString, bool rawAttributes = false)
    {
        var rss = GeoRSS20.Serializer.FromString(rssString, System.Text.Encoding.UTF8);

        FeatureCollection features = new FeatureCollection();
        StringBuilder sbTitle = new StringBuilder();
        if (rss != null && rss.channel != null && rss.channel.item != null)
        {
            #region Title

            string channel_title = GeoRSS20.Formatter.ToString(rss.channel.title);
            string channel_link = GeoRSS20.Formatter.ToString(rss.channel.link);
            string channel_pubDate = rss.channel.pubDate != null ? GeoRSS20.Formatter.ToString(rss.channel.pubDate) : String.Empty;
            string channel_description = GeoRSS20.Formatter.ToString(rss.channel.description);

            if (!String.IsNullOrWhiteSpace(channel_title))
            {
                if (!String.IsNullOrWhiteSpace(channel_link))
                {
                    string img = String.Empty;
                    //if (channel_link.EndsWith(".jpg") || channel_link.EndsWith(".png"))
                    //{
                    //    img = "<img src='" + channel_link + "' style='width:200px' />";
                    //}
                    sbTitle.Append("<a href='" + channel_link + "' target='_blank'><h2>" + channel_title + "</h2>" + img + "</a>");
                }
                else
                {
                    sbTitle.Append("<h2>" + channel_title + "</h2>");
                }
                if (!String.IsNullOrWhiteSpace(channel_description))
                {
                    sbTitle.Append("<p>" + channel_description + "</p>");
                }

                DateTime channel_pubDateTime;
                if (GeoRSS20.Rfc822DateTime.TryParse(channel_pubDate, out channel_pubDateTime))
                {
                    channel_pubDate = channel_pubDateTime.ToString();
                }

                sbTitle.Append("<span style='color:#aaa;font-size:0.8em'>" + channel_pubDate + "</span>");
            }

            #endregion

            foreach (var item in rss.channel.item)
            {
                var feature = new WebMapping.Core.Feature();

                feature.GlobalOid = Guid.NewGuid().ToString("N");

                #region Geometry

                var rssGeometry = GeoRSS20.RssGeometry.FromItem(item);
                if (rssGeometry is GeoRSS20.RssPoint point)
                {
                    feature.Shape = new WebMapping.Core.Geometry.Point(point.Long, point.Lat);
                }
                else if (rssGeometry is GeoRSS20.RssLine line && line.PointCount > 0)
                {
                    var anyPointOnLine = line[line.PointCount / 2];
                    feature.Shape = new WebMapping.Core.Geometry.Point(anyPointOnLine.Long, anyPointOnLine.Lat);
                }

                #endregion

                #region Properties

                //feature.Attributes.Add(new E.WebMapping.Attribute("Title", GeoRSS20.Formatter.ToString(item.title)));
                //feature.Attributes.Add(new E.WebMapping.Attribute("Description", GeoRSS20.Formatter.ToString(item.description)));

                string title = GeoRSS20.Formatter.ToString(item.title);
                string link = GeoRSS20.Formatter.ToString(item.link);
                string pubDate = item.pubDate != null ? GeoRSS20.Formatter.ToString(item.pubDate) : String.Empty;
                string source = GeoRSS20.Formatter.ToString(item.source);
                string author = GeoRSS20.Formatter.ToString(item.author);
                string description = GeoRSS20.Formatter.ToString(item.description);

                if (rawAttributes == true)
                {
                    feature.Attributes.Add(new WebMapping.Core.Attribute("title", title));
                    feature.Attributes.Add(new WebMapping.Core.Attribute("link", link));
                    feature.Attributes.Add(new WebMapping.Core.Attribute($"pubDate", pubDate));
                    feature.Attributes.Add(new WebMapping.Core.Attribute("source", source));
                    feature.Attributes.Add(new WebMapping.Core.Attribute("author", author));
                    feature.Attributes.Add(new WebMapping.Core.Attribute("description", description));
                }
                else
                {
                    StringBuilder sbFullText = new StringBuilder();
                    StringBuilder sbPopupText = new StringBuilder();

                    if (!String.IsNullOrWhiteSpace(link))
                    {
                        string img = String.Empty;
                        //if(link.EndsWith(".jpg") || link.EndsWith(".png"))
                        //{
                        //    img = "<img src='" + link + "' style='width:200px' />";
                        //}
                        sbFullText.Append("<strong>" + title + "</strong>");
                        sbPopupText.Append("<a href='" + link + "' target='_blank'><strong>" + title + "</strong>" + img + "</a>");
                    }
                    else
                    {
                        sbFullText.Append("<strong>" + title + "</strong>");
                    }

                    if (!String.IsNullOrWhiteSpace(description))
                    {
                        sbPopupText.Append("<br/>" + description);
                    }

                    if (!String.IsNullOrWhiteSpace(source))
                    {
                        sbFullText.Append("<br/>Quelle: " + source);
                        sbPopupText.Append("<br/>Quelle: " + source);
                    }
                    if (!String.IsNullOrWhiteSpace(author))
                    {
                        sbPopupText.Append("<br/>Author: " + author);
                    }

                    DateTime pubDateTime;
                    if (GeoRSS20.Rfc822DateTime.TryParse(pubDate, out pubDateTime))
                    {
                        pubDate = pubDateTime.ToString();
                    }

                    sbFullText.Append("<br/><span style='color:#aaa;font-size:0.8em'>" + pubDate + "</span>");
                    sbPopupText.Append("<br/><span style='color:#aaa;font-size:0.8em'>" + pubDate + "</span>");

                    feature.Attributes.Add(new WebMapping.Core.Attribute("_fulltext", sbFullText.ToString()));
                    feature.Attributes.Add(new WebMapping.Core.Attribute("_popuptext", sbPopupText.ToString()));
                }

                #endregion

                features.Add(feature);
            }
        }

        return new FeaturesDTO(features)
        {
            title = sbTitle.ToString()
        };
    }

    #endregion

    #region Classes

    public class Meta
    {
        public enum Tool
        {
            Unknown,
            Query,
            Search,
            Identify,
            PointIdentify,
            LineIdentify,
            PolygonIdentify,
            Buffer,
            Edit
        }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public Tool ToolType { get; set; }

        [JsonProperty(PropertyName = "tool")]
        [System.Text.Json.Serialization.JsonPropertyName("tool")]
        public string ToolTypeString { get { return ToolType.ToString().ToLower(); } }

        [JsonProperty(PropertyName = "selected")]
        [System.Text.Json.Serialization.JsonPropertyName("selected")]
        public bool Selected { get; set; }

        [JsonProperty(PropertyName = "applicable_filters", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("applicable_filters")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string ApplicableVisFilters { get; set; }

        [JsonProperty(PropertyName = "custom_selection", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("custom_selection")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string CustomSelection { get; set; }

        [JsonProperty(PropertyName = "links", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("links")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string> Links { get; set; }

        [JsonProperty(PropertyName = "linktargets", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("linktargets")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string> LinkTargets { get; set; }

        [JsonProperty(PropertyName = "service_id", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("service_id")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string ServiceId { get; set; }
        [JsonProperty(PropertyName = "query_id", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("query_id")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string QueryId { get; set; }

        [JsonProperty("table_fields", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("table_fields")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<TableFieldDefintion> TableFields { get; set; }

        [JsonProperty("has_attachments", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("has_attachments")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public bool? HasAttachments { get; set; }

        [JsonProperty("warnings", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("warnings")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<string> Warnings { get; set; }
        [JsonProperty("infos", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("infos")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<string> Informations { get; set; }

        [JsonProperty("hash", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("hash")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string HashCode { get; set; }
    }

    public class TableFieldDefintion
    {
        [JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("visible")]
        [System.Text.Json.Serialization.JsonPropertyName("visible")]
        public bool Visible { get; set; }

        [JsonProperty("sorting_alg", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("sorting_alg")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string SortingAlgorithm { get; set; }

        [JsonProperty("image_width", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("image_width")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public int? ImageWidth { get; set; }

        [JsonProperty("image_height", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("image_height")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public int? ImageHeight { get; set; }
    }

    #endregion
}