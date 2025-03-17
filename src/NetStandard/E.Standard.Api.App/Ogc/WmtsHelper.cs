using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Exceptions.Ogc;
using E.Standard.CMS.Core;
using E.Standard.Platform;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Tiling;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Api.App.Ogc;

public class WmtsHelper
{
    private readonly string _onlineResource;

    public WmtsHelper(string onlineResource)
    {
        _onlineResource = onlineResource;
    }

    #region GetCapabilities

    public string WMTSGetCapabilities(ServiceInfoDTO[] serviceInfo, NameValueCollection arguments)
    {
        if (arguments["version"] == "1.0.0")
        {
            return WMTSGetCapabilities_1_0_0(serviceInfo, arguments);
        }

        throw new OgcArgumentException("not supported version: " + arguments["version"]);
    }

    private string WMTSGetCapabilities_1_0_0(ServiceInfoDTO[] serviceInfos, NameValueCollection arguments)
    {
        OGC.Schema.wmts_1_0_0.Capabilities capabilities = new OGC.Schema.wmts_1_0_0.Capabilities()
        {
            version = "1.0.0",
        };

        bool ignoreAxes = IgnoreAxes(arguments);  // ArcMap spinnt!!

        capabilities.NameSpaces = new System.Xml.Serialization.XmlSerializerNamespaces();
        capabilities.NameSpaces.Add("ows", "http://www.opengis.net/ows/1.1");
        capabilities.NameSpaces.Add("xlink", "http://www.w3.org/1999/xlink");
        capabilities.NameSpaces.Add("gml", "http://www.opengis.net/gml");

        #region ServiceIndentification

        capabilities.ServiceIdentification = new OGC.Schema.wmts_1_0_0.ServiceIdentification();
        capabilities.ServiceIdentification.Title = new OGC.Schema.wmts_1_0_0.LanguageStringType[] { new OGC.Schema.wmts_1_0_0.LanguageStringType() {
            Value= GetWmtsServiceTitle(serviceInfos)
        } };
        capabilities.ServiceIdentification.ServiceType = new OGC.Schema.wmts_1_0_0.CodeType() { Value = "OGC WMTS" };
        capabilities.ServiceIdentification.ServiceTypeVersion = new string[] { "1.0.0" };

        #endregion

        #region OperationsMetadata

        capabilities.OperationsMetadata = new OGC.Schema.wmts_1_0_0.OperationsMetadata();

        var getCapOperation = new OGC.Schema.wmts_1_0_0.Operation() { name = "GetCapabilities" };
        getCapOperation.DCP = new OGC.Schema.wmts_1_0_0.DCP[] { new OGC.Schema.wmts_1_0_0.DCP() };
        getCapOperation.DCP[0].Item = new OGC.Schema.wmts_1_0_0.HTTP();
        getCapOperation.DCP[0].Item.Items = new OGC.Schema.wmts_1_0_0.RequestMethodType[] { new OGC.Schema.wmts_1_0_0.RequestMethodType() };

        getCapOperation.DCP[0].Item.Items[0].href = OnlineResourcePath() + "?SERVICE=WMTS&VERSION=1.0.0" + TokenParameter(arguments) + "&";
        getCapOperation.DCP[0].Item.Items[0].Constraint = new OGC.Schema.wmts_1_0_0.DomainType[] { new OGC.Schema.wmts_1_0_0.DomainType() };
        getCapOperation.DCP[0].Item.Items[0].Constraint[0].name = "GetEncoding";
        getCapOperation.DCP[0].Item.Items[0].Constraint[0].AllowedValues = new object[] { new OGC.Schema.wmts_1_0_0.ValueType() { Value = "KVP" /*"RESTful"*/ } };
        getCapOperation.DCP[0].Item.ItemsElementName = new OGC.Schema.wmts_1_0_0.ItemsChoiceType[] { OGC.Schema.wmts_1_0_0.ItemsChoiceType.Get };


        var getTileOperation = new OGC.Schema.wmts_1_0_0.Operation() { name = "GetTile" };
        getTileOperation.DCP = new OGC.Schema.wmts_1_0_0.DCP[] { new OGC.Schema.wmts_1_0_0.DCP() };
        getTileOperation.DCP[0].Item = new OGC.Schema.wmts_1_0_0.HTTP();
        getTileOperation.DCP[0].Item.Items = new OGC.Schema.wmts_1_0_0.RequestMethodType[] { new OGC.Schema.wmts_1_0_0.RequestMethodType() };

        getTileOperation.DCP[0].Item.Items[0].href = OnlineResourcePath() + "?SERVICE=WMTS&VERSION=1.0.0" + TokenParameter(arguments) + "&";
        getTileOperation.DCP[0].Item.Items[0].Constraint = new OGC.Schema.wmts_1_0_0.DomainType[] { new OGC.Schema.wmts_1_0_0.DomainType() };
        getTileOperation.DCP[0].Item.Items[0].Constraint[0].name = "GetEncoding";
        getTileOperation.DCP[0].Item.Items[0].Constraint[0].AllowedValues = new object[] { new OGC.Schema.wmts_1_0_0.ValueType() { Value = "KVP" /*"RESTful"*/ } };
        getTileOperation.DCP[0].Item.ItemsElementName = new OGC.Schema.wmts_1_0_0.ItemsChoiceType[] { OGC.Schema.wmts_1_0_0.ItemsChoiceType.Get };

        capabilities.OperationsMetadata.Operation = new OGC.Schema.wmts_1_0_0.Operation[]
        {
            getCapOperation, getTileOperation
        };

        #endregion

        #region Contents

        capabilities.Contents = new OGC.Schema.wmts_1_0_0.ContentsType();

        List<OGC.Schema.wmts_1_0_0.LayerType> layers = new List<OGC.Schema.wmts_1_0_0.LayerType>();
        List<OGC.Schema.wmts_1_0_0.TileMatrixSet> matrixSets = new List<OGC.Schema.wmts_1_0_0.TileMatrixSet>();

        foreach (var serviceInfo in serviceInfos)
        {
            if (!(serviceInfo.properties is ServiceInfoDTO.TileProperties) || serviceInfo.extent == null)
            {
                continue;
            }

            var srs = serviceInfo.supportedCrs != null && serviceInfo.supportedCrs.Length > 0 ? serviceInfo.supportedCrs[0] : 0;
            if (srs == 0)
            {
                continue;
            }

            var sRef = ApiGlobals.SRefStore.SpatialReferences.ById(srs);
            if (sRef == null)
            {
                continue;
            }

            var tileProperties = serviceInfo.properties as ServiceInfoDTO.TileProperties;

            #region Layer

            var layer = new OGC.Schema.wmts_1_0_0.LayerType();

            layer.Title = new OGC.Schema.wmts_1_0_0.LanguageStringType[] { new OGC.Schema.wmts_1_0_0.LanguageStringType() { Value = GetWmtsServiceTitle(new ServiceInfoDTO[] { serviceInfo }) } };
            layer.Identifier = new OGC.Schema.wmts_1_0_0.CodeType() { Value = serviceInfo.id };

            layer.Style = new OGC.Schema.wmts_1_0_0.Style[]
            {
                new OGC.Schema.wmts_1_0_0.Style()
                {
                    Title=new OGC.Schema.wmts_1_0_0.LanguageStringType[] {new OGC.Schema.wmts_1_0_0.LanguageStringType() { Value="Default Style"} },
                    Identifier=new OGC.Schema.wmts_1_0_0.CodeType() {Value="default" }
                }
            };

            #region BoundingBox

            var extent = new Envelope(serviceInfo.extent[0], serviceInfo.extent[1], serviceInfo.extent[2], serviceInfo.extent[3]);
            using (var transformer = new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, 4326, srs))
            {
                transformer.Transform(extent);
            }

            layer.BoundingBox = new OGC.Schema.wmts_1_0_0.BoundingBoxType[]
            {
                new OGC.Schema.wmts_1_0_0.BoundingBoxType()
                {
                    crs="urn:ogc:def:crs:EPSG::"+srs,
                    LowerCorner=PointToString(extent.LowerLeft, sRef, ignoreAxes),
                    UpperCorner=PointToString(extent.UpperRight, sRef, ignoreAxes)
                }
            };
            layer.WGS84BoundingBox = new OGC.Schema.wmts_1_0_0.WGS84BoundingBoxType[]
            {
                new OGC.Schema.wmts_1_0_0.WGS84BoundingBoxType()
                {
                    crs="urn:ogc:def:crs:OGC:2:84",  // urn:ogc:def:crs:OGC:2:84
                    LowerCorner=PointToString(new Point( serviceInfo.extent[0],serviceInfo.extent[1]),null),
                    UpperCorner=PointToString(new Point( serviceInfo.extent[2],serviceInfo.extent[3]),null)
                }
            };

            #endregion

            layer.TileMatrixSetLink = new OGC.Schema.wmts_1_0_0.TileMatrixSetLink[]
            {
                new OGC.Schema.wmts_1_0_0.TileMatrixSetLink()
                {
                    TileMatrixSet=serviceInfo.id+"_default_matrixset"
                }
            };

            layer.Format = new string[] { tileProperties.tileurl.EndsWith(".png") ? "image/png" : "image/jpg" };

            layers.Add(layer);

            #endregion

            #region Matrix Set

            double matrixSetWidth = extent.MaxX - tileProperties.origin[0];
            double matrixSetHeight = tileProperties.origin[1] - extent.MinY;

            var matrixSet = new OGC.Schema.wmts_1_0_0.TileMatrixSet();

            matrixSet.Title = new OGC.Schema.wmts_1_0_0.LanguageStringType[] { new OGC.Schema.wmts_1_0_0.LanguageStringType() { Value = serviceInfo.name + " Default Matrix Set" } };
            matrixSet.Identifier = new OGC.Schema.wmts_1_0_0.CodeType() { Value = serviceInfo.id + "_default_matrixset" };
            matrixSet.SupportedCRS = "urn:ogc:def:crs:EPSG::" + srs;

            matrixSet.TileMatrix = new OGC.Schema.wmts_1_0_0.TileMatrix[tileProperties.resolutions.Length];

            #region DPI
            double dpi = 25.4D / 0.28D;   // wmts 0.28mm -> 1 Pixel;
            double inchMeter = 0.0254; // 0.0254000508001016; 
            double dpm = dpi / inchMeter;
            #endregion

            for (int r = 0, to = tileProperties.resolutions.Length; r < to; r++)
            {
                matrixSet.TileMatrix[r] = new OGC.Schema.wmts_1_0_0.TileMatrix();
                matrixSet.TileMatrix[r].Identifier = new OGC.Schema.wmts_1_0_0.CodeType() { Value = r.ToString() };
                matrixSet.TileMatrix[r].TopLeftCorner = PointToString(new Point(tileProperties.origin[0], tileProperties.origin[1]), sRef, ignoreAxes);
                matrixSet.TileMatrix[r].TileWidth = tileProperties.tilesize.ToString();
                matrixSet.TileMatrix[r].TileHeight = tileProperties.tilesize.ToString();

                double tileWidthHeight = tileProperties.tilesize * tileProperties.resolutions[r];
                int matrixWidth = (int)Math.Round(matrixSetWidth / tileWidthHeight + 0.5);
                int matrixHeight = (int)Math.Round(matrixSetHeight / tileWidthHeight + 0.5);

                matrixSet.TileMatrix[r].MatrixWidth = matrixWidth.ToString();
                matrixSet.TileMatrix[r].MatrixHeight = matrixHeight.ToString();
                matrixSet.TileMatrix[r].ScaleDenominator = Math.Round(tileProperties.resolutions[r] * dpm, 8);
            }

            matrixSets.Add(matrixSet);

            #endregion
        }

        capabilities.Contents.DatasetDescriptionSummary = layers.ToArray();
        capabilities.Contents.TileMatrixSet = matrixSets.ToArray();

        #endregion

        OGC.Schema.Serializer<OGC.Schema.wmts_1_0_0.Capabilities> serializer = new OGC.Schema.Serializer<OGC.Schema.wmts_1_0_0.Capabilities>();
        string xml = serializer.Serialize(capabilities);

        xml = xml.Replace(@"<ows:DatasetDescriptionSummary xsi:type=""LayerType"">", "<Layer>");
        xml = xml.Replace(@"</ows:DatasetDescriptionSummary>", "</Layer>");

        return xml;
    }

    #endregion

    #region GetTile

    async public Task<byte[]> GetTile(IRequestContext requestContext, IMapService[] services, ServiceInfoDTO[] serviceInfos, NameValueCollection arguments, CmsDocument.UserIdentification ui, MapRestrictions mapRestrictions)
    {
        string layerName = arguments["layer"];
        string matrixSetName = arguments["tilematrixset"];
        int tileLevel = int.Parse(arguments["tilematrix"]);
        int tileRow = int.Parse(arguments["tilerow"]);
        int tileCol = int.Parse(arguments["tilecol"]);

        var serviceInfo = serviceInfos.Where(m => m.id == layerName).FirstOrDefault();
        if (!(serviceInfo?.properties is ServiceInfoDTO.TileProperties))
        {
            return null;
        }

        //var tileProperties = serviceInfo.properties as ServiceInfoDTO.TileProperties;

        var service = services.Where(m => m.Url == layerName).FirstOrDefault();
        if (!(service is TileService))
        {
            return null;
        }

        ServiceRestirctions serviceRestrictions = mapRestrictions != null && mapRestrictions.ContainsKey(service.ID) ? mapRestrictions[service.ID] : null;
        var url = ((TileService)service).TileUrl(requestContext, tileLevel, tileRow, tileCol, serviceRestrictions);

        if (String.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        byte[] data = null;
        //if (ApiGlobals.UseAsyncService)
        {
            //data = await WebHelper.DownloadDataAsync(url, null, false);
            data = await requestContext.Http.GetDataAsync(url);
        }
        //else
        //{
        //    NameValueCollection headers;
        //    data = WebHelper.DownloadData(url, null, out headers, false);
        //}

        return data;
    }

    #endregion

    #region Helper

    public string GetWmtsServiceTitle(ServiceInfoDTO[] serviceInfos)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var serviceInfo in serviceInfos)
        {
            if (sb.Length > 0)
            {
                sb.Append(",");
            }

            sb.Append(serviceInfo.name);
        }
        return sb.ToString();
    }


    private string TokenParameter(NameValueCollection arguments)
    {
        if (arguments == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();

        foreach (string key in arguments.Keys)
        {
            if (key == "ogc_ticket")
            {
                continue;
            }

            if (key.StartsWith("ogc_"))
            {
                sb.Append("&" + key + "=" + arguments[key]);
            }
        }

        string token = arguments["ogc_ticket"];
        if (!String.IsNullOrEmpty(token))
        {
            sb.Append("&ogc_ticket=" + token);
        }

        return sb.ToString();
    }

    public string OnlineResourcePath()
    {
        return _onlineResource;
    }

    private Envelope InitialExtent(ServiceInfoDTO[] serviceInfos)
    {
        Envelope initialExtent = null;
        foreach (var serviceInfo in serviceInfos)
        {
            if (serviceInfo.extent == null)
            {
                throw new OgcException("service '" + serviceInfo.id + "' has no extent");
            }

            if (initialExtent == null)
            {
                initialExtent = new Envelope(serviceInfo.extent[0], serviceInfo.extent[1], serviceInfo.extent[2], serviceInfo.extent[3]);
            }
            else
            {
                initialExtent.Union(new Envelope(serviceInfo.extent[0], serviceInfo.extent[1], serviceInfo.extent[2], serviceInfo.extent[3]));
            }
        }
        return initialExtent;
    }

    public string PointToString(Point p, SpatialReference sRef, bool ignoreAxes = false)
    {
        if (ignoreAxes == false &&
            sRef != null &&
            (sRef.AxisX == AxisDirection.North || sRef.AxisX == AxisDirection.South) &&
            (sRef.AxisY == AxisDirection.West || sRef.AxisY == AxisDirection.East))
        {
            return p.Y.ToPlatformNumberString() + " " + p.X.ToPlatformNumberString();
        }
        else
        {
            return p.X.ToPlatformNumberString() + " " + p.Y.ToPlatformNumberString();
        }
    }

    private bool IgnoreAxes(NameValueCollection parameters)
    {
        return parameters["ignore-axes"] != null && parameters["ignore-axes"].ToLower() == "true";
    }

    #endregion
}
