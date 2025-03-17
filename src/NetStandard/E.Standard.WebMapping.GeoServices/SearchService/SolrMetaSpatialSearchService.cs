using E.Standard.CMS.Core;
using E.Standard.Web.Abstractions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.SearchService;

public class SolrMetaSpatialSearchService : SolrMetaSearchService, ISearchService3
{
    public SolrMetaSpatialSearchService(IMap map, CmsNode node)
        : base(map, node)
    {
    }

    async public Task<SearchServiceItems> Query3Async(IHttpService httpService, string term, int rows, IEnumerable<string> categories, Envelope queryBBox, int targetProjId = 4326)
    {
        string url = String.Format(_serviceUrl, term).Replace("{term}", term).Replace("[term]", term);
        url += "&rows=" + (rows > 0 ? rows : _rows);

        if (queryBBox != null)
        {
            url += $"&fq=geowgs:[{queryBBox.MinY},{queryBBox.MinX} TO {queryBBox.MaxY},{queryBBox.MaxX}]";
        }

        //string json = await dotNETConnector.DownloadXmlAsync(url, _connector, null);
        string json = await httpService.GetStringAsync(url, encoding: Encoding.UTF8);
        var lucType = JsonConvert.DeserializeObject<LucType>(json);

        List<SearchServiceItem> items = new List<SearchServiceItem>();

        if (categories != null && categories.Count() > 0)
        {
            if ((await this.TypesAsync(httpService)).Select(t => t.Key).Intersect(categories).Count() == 0)
            {
                return new SearchServiceItems()
                {
                    Items = items.ToArray()
                };
            }
        }

        SpatialReference targetSRef = _sRef4326;
        if (targetProjId > 0 && targetProjId != 4326 && _sRefStore != null)
        {
            targetSRef = _sRefStore.SpatialReferences.ById(targetProjId);
        }

        if (lucType.response != null &&
            lucType.response.docs is Newtonsoft.Json.Linq.JArray &&
            ((Newtonsoft.Json.Linq.JArray)lucType.response.docs).Count > 0)
        {
            using (var transformer = new GeometricTransformer())
            {
                if (_sRef != null && targetSRef != null && _sRef.Id != targetSRef.Id)
                {
                    transformer.FromSpatialReference(_sRef.Proj4, !_sRef.IsProjective);
                    transformer.ToSpatialReference(targetSRef.Proj4, !targetSRef.IsProjective);
                }
                foreach (Newtonsoft.Json.Linq.JObject jObject in (Newtonsoft.Json.Linq.JArray)lucType.response.docs)
                {
                    var item = new SearchServiceItem(this)
                    {
                        Id = GetJsonValue(jObject, "id"),
                        SuggestText = GetJsonValue(jObject, _suggestedTextField),
                        ThumbnailUrl = GetJsonValue(jObject, _thumbnailField),
                        Subtext = GetJsonValue(jObject, _subtextField),
                        Link = GetJsonValue(jObject, _link),
                        Score = 0D
                    };

                    string geo = GetJsonValue(jObject, _geometryField);
                    Shape shape = GetGeometry(geo);

                    if (shape != null && transformer.CanTransform)
                    {
                        transformer.Transform2D(shape);
                    }

                    item.Geometry = shape;
                    item.BBox = shape?.ShapeEnvelope?.ToArray();

                    #region BBox

                    double? minx = GetJsonDouble(jObject, "minx"),
                            miny = GetJsonDouble(jObject, "miny"),
                            maxx = GetJsonDouble(jObject, "maxx"),
                            maxy = GetJsonDouble(jObject, "maxy");

                    if (minx != null && miny != null && maxx != null && maxy != null)
                    {
                        double[] x = new double[] { (double)minx, (double)maxx };
                        double[] y = new double[] { (double)miny, (double)maxy };

                        if (transformer.CanTransform)
                        {
                            transformer.Transform2D(x, y);
                        }

                        item.BBox = new double[] { x[0], y[0], x[1], y[1] };
                    }

                    #endregion

                    items.Add(item);
                }
            }
        }

        return new SearchServiceItems()
        {
            Items = items.ToArray()
        };
    }
}
