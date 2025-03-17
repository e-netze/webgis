using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Exceptions.Ogc;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Platform;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.Api.Abstractions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using gView.GraphicsEngine;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Api.App.Ogc;

public class WmsHelper
{
    private readonly string _onlineResource;
    private readonly int[] _defaultSupportedCrs;

    public WmsHelper(string onlineResource,
                     int[] defaultSupportedCrs)
    {
        _onlineResource = onlineResource;
        _defaultSupportedCrs = defaultSupportedCrs;
    }

    #region GetCapabilities

    public string WMSGetCapabilities(ServiceInfoDTO[] serviceInfo, NameValueCollection arguments)
    {
        if (arguments["version"] == "1.3.0")
        {
            if (arguments["client"] == "inspire")
            {
                return WMSGetCapabilities_1_3_0_inspire(serviceInfo, arguments);
            }

            return WMSGetCapabilities_1_3_0(serviceInfo, arguments);
        }
        else if (arguments["version"] == "1.1.1")
        {
            return WMSGetCapabilities_1_1_1(serviceInfo, arguments);
        }

        throw new OgcArgumentException("not supported version: " + arguments["version"]);
    }

    private string WMSGetCapabilities_1_1_1(ServiceInfoDTO[] serviceInfos, NameValueCollection arguments)
    {
        OGC.Schema.wms_1_1_1.WMT_MS_Capabilities capabilities = new OGC.Schema.wms_1_1_1.WMT_MS_Capabilities();
        capabilities.version = "1.1.1";

        #region Service
        capabilities.Service = new OGC.Schema.wms_1_1_1.Service();
        capabilities.Service.Name = GetWmsServiceName(serviceInfos);
        capabilities.Service.Title = GetWmsServiceTitle(serviceInfos);
        #endregion

        #region Capabilities
        capabilities.Capability = new OGC.Schema.wms_1_1_1.Capability();
        capabilities.Capability.Request = new OGC.Schema.wms_1_1_1.Request();
        capabilities.Capability.Request.GetMap = new OGC.Schema.wms_1_1_1.GetMap();
        capabilities.Capability.Request.GetCapabilities = new OGC.Schema.wms_1_1_1.GetCapabilities();
        capabilities.Capability.Request.GetFeatureInfo = new OGC.Schema.wms_1_1_1.GetFeatureInfo();

        OGC.Schema.wms_1_1_1.Get getHttp = new OGC.Schema.wms_1_1_1.Get();
        getHttp.OnlineResource = new OGC.Schema.wms_1_1_1.OnlineResource();
        getHttp.OnlineResource.href = OnlineResourcePath() + "?SERVICE=WMS" + TokenParameter(arguments) + "&";
        OGC.Schema.wms_1_1_1.DCPType dcpType = new OGC.Schema.wms_1_1_1.DCPType();
        dcpType.HTTP = new object[] { getHttp };

        capabilities.Capability.Request.GetMap.DCPType = new OGC.Schema.wms_1_1_1.DCPType[] { dcpType };
        capabilities.Capability.Request.GetMap.Format = new string[] { "image/jpg", "image/jpeg", "image/png" };
        capabilities.Capability.Request.GetFeatureInfo.DCPType = new OGC.Schema.wms_1_1_1.DCPType[] { dcpType };
        capabilities.Capability.Request.GetFeatureInfo.Format = new string[] { "text/html" };
        capabilities.Capability.Request.GetCapabilities.DCPType = new OGC.Schema.wms_1_1_1.DCPType[] { dcpType };
        capabilities.Capability.Request.GetCapabilities.Format = new string[] { "application/vnd.ogc.wms_xml" };

        capabilities.Capability.Exception = new string[] { "application/vnd.ogc.se_xml" };

        capabilities.Capability.Layer = new OGC.Schema.wms_1_1_1.Layer();
        capabilities.Capability.Layer.Name = GetWmsServiceName(serviceInfos);
        capabilities.Capability.Layer.Title = GetWmsServiceTitle(serviceInfos);

        #region SRS

        List<string> epsgCodes = new List<string>();
        foreach (int crs in SupportedCrs(serviceInfos))
        {
            epsgCodes.Add("EPSG:" + crs);
        }

        capabilities.Capability.Layer.SRS = epsgCodes.ToArray();


        Envelope initialExtent = InitialExtent(serviceInfos);

        List<OGC.Schema.wms_1_1_1.BoundingBox> bboxes = new List<OGC.Schema.wms_1_1_1.BoundingBox>();
        foreach (int supportedCrs in SupportedCrs(serviceInfos))
        {
            Envelope extent = new Envelope(initialExtent);

            using (var transformer = new WebMapping.Core.Geometry.GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, 4326, supportedCrs))
            {
                transformer.Transform(extent);
            }

            OGC.Schema.wms_1_1_1.BoundingBox wmsBBox = new OGC.Schema.wms_1_1_1.BoundingBox();
            wmsBBox.SRS = "EPSG:" + supportedCrs;
            wmsBBox.minx = extent.MinX.ToPlatformNumberString();
            wmsBBox.miny = extent.MinY.ToPlatformNumberString();
            wmsBBox.maxx = extent.MaxX.ToPlatformNumberString();
            wmsBBox.maxy = extent.MaxY.ToPlatformNumberString();
            bboxes.Add(wmsBBox);
        }
        capabilities.Capability.Layer.BoundingBox = bboxes.ToArray();

        capabilities.Capability.Layer.LatLonBoundingBox = new OGC.Schema.wms_1_1_1.LatLonBoundingBox();
        capabilities.Capability.Layer.LatLonBoundingBox.minx = initialExtent.MinX.ToPlatformNumberString();
        capabilities.Capability.Layer.LatLonBoundingBox.miny = initialExtent.MinY.ToPlatformNumberString();
        capabilities.Capability.Layer.LatLonBoundingBox.maxx = initialExtent.MaxX.ToPlatformNumberString();
        capabilities.Capability.Layer.LatLonBoundingBox.maxy = initialExtent.MaxY.ToPlatformNumberString();

        #endregion

        #region Layers
        List<OGC.Schema.wms_1_1_1.Layer> layers = new List<OGC.Schema.wms_1_1_1.Layer>();
        foreach (var serviceInfo in serviceInfos)
        {
            foreach (var layerInfo in serviceInfo.layers)
            {
                if (layerInfo.locked || !layerInfo.IsTocLayer)
                {
                    continue;
                }

                OGC.Schema.wms_1_1_1.Layer layer = layers.Where(l => l.Name == WmsLayerId(serviceInfos, serviceInfo, layerInfo.OgcId)).FirstOrDefault();

                if (layer != null)
                {
                    continue;
                }

                layer = new OGC.Schema.wms_1_1_1.Layer();
                layer.Name = WmsLayerId(serviceInfos, serviceInfo, layerInfo.OgcId);
                layer.Title = layerInfo.OgcTitle;

                #region Ist Layer Queryable
                bool queryAble = IsQueryableLayer(serviceInfo, layerInfo);
                layer.queryable = (queryAble ? OGC.Schema.wms_1_1_1.LayerQueryable.Item1 : OGC.Schema.wms_1_1_1.LayerQueryable.Item0);
                #endregion

                layers.Add(layer);
            }
        }
        capabilities.Capability.Layer.Layer1 = layers.ToArray();
        #endregion

        #endregion

        OGC.Schema.Serializer<OGC.Schema.wms_1_1_1.WMT_MS_Capabilities> serializer = new OGC.Schema.Serializer<OGC.Schema.wms_1_1_1.WMT_MS_Capabilities>();
        return serializer.Serialize(capabilities).Replace("d8p1", "xlink");
    }

    private string WMSGetCapabilities_1_3_0(ServiceInfoDTO[] serviceInfos, NameValueCollection arguments)
    {
        OGC.Schema.wms_1_3_0.WMS_Capabilities capabilities = new OGC.Schema.wms_1_3_0.WMS_Capabilities();
        capabilities.version = "1.3.0";

        capabilities.NameSpaces = new System.Xml.Serialization.XmlSerializerNamespaces();
        capabilities.NameSpaces.Add("sld", "http://www.opengis.net/sld");
        capabilities.NameSpaces.Add("xlink", "http://www.w3.org/1999/xlink");

        #region Service
        capabilities.Service = new OGC.Schema.wms_1_3_0.Service();
        capabilities.Service.Name = new OGC.Schema.wms_1_3_0.ServiceName();
        capabilities.Service.Title = GetWmsServiceTitle(serviceInfos);
        capabilities.Service.Abstract = arguments["append_to_service_abstract"];

        capabilities.Service.OnlineResource = new OGC.Schema.wms_1_3_0.OnlineResource();
        capabilities.Service.OnlineResource.href = OnlineResourcePath() + "?SERVICE=WMS" + TokenParameter(arguments) + "&";
        if (IgnoreAxes(arguments))
        {
            capabilities.Service.OnlineResource.href += "ignore-axes=true&";
        }
        #endregion

        #region Capabilities
        capabilities.Capability = new OGC.Schema.wms_1_3_0.Capability();
        capabilities.Capability.Request = new OGC.Schema.wms_1_3_0.Request();
        capabilities.Capability.Request.GetMap = new OGC.Schema.wms_1_3_0.OperationType();
        capabilities.Capability.Request.GetCapabilities = new OGC.Schema.wms_1_3_0.OperationType();
        capabilities.Capability.Request.GetFeatureInfo = new OGC.Schema.wms_1_3_0.OperationType();

        OGC.Schema.wms_1_3_0.HTTP getHttp = new OGC.Schema.wms_1_3_0.HTTP();
        getHttp.Get = new OGC.Schema.wms_1_3_0.Get();
        getHttp.Get.OnlineResource = new OGC.Schema.wms_1_3_0.OnlineResource();
        getHttp.Get.OnlineResource.href = OnlineResourcePath() + "?SERVICE=WMS" + TokenParameter(arguments) + "&";
        if (IgnoreAxes(arguments))
        {
            getHttp.Get.OnlineResource.href += "ignore-axes=true&";
        }

        OGC.Schema.wms_1_3_0.DCPType dcpType = new OGC.Schema.wms_1_3_0.DCPType();
        dcpType.HTTP = getHttp;

        capabilities.Capability.Request.GetMap.DCPType = new OGC.Schema.wms_1_3_0.DCPType[] { dcpType };
        capabilities.Capability.Request.GetMap.Format = new string[] { "image/jpg", "image/png" };
        capabilities.Capability.Request.GetFeatureInfo.DCPType = new OGC.Schema.wms_1_3_0.DCPType[] { dcpType };
        capabilities.Capability.Request.GetFeatureInfo.Format = new string[] { "text/html" };
        capabilities.Capability.Request.GetCapabilities.DCPType = new OGC.Schema.wms_1_3_0.DCPType[] { dcpType };
        capabilities.Capability.Request.GetCapabilities.Format = new string[] { "text/xml" };

        capabilities.Capability.Request.GetLegendGraphic = new OGC.Schema.wms_1_3_0.OperationType();
        capabilities.Capability.Request.GetLegendGraphic.DCPType = new OGC.Schema.wms_1_3_0.DCPType[] { dcpType };
        capabilities.Capability.Request.GetLegendGraphic.Format = new string[] { "image/png" };

        capabilities.Capability.Exception = new string[] { "application/vnd.ogc.se_xml" };

        capabilities.Capability.Layer = new OGC.Schema.wms_1_3_0.Layer[] { new OGC.Schema.wms_1_3_0.Layer() };
        capabilities.Capability.Layer[0].Name = GetWmsServiceName(serviceInfos);
        capabilities.Capability.Layer[0].Title = GetWmsServiceTitle(serviceInfos);
        capabilities.Capability.Layer[0].Style = new OGC.Schema.wms_1_3_0.Style[]{
                    new OGC.Schema.wms_1_3_0.Style() {
                        Name="default",
                        Title="default",
                        LegendURL=new OGC.Schema.wms_1_3_0.LegendURL[]{
                            new OGC.Schema.wms_1_3_0.LegendURL(){
                                //width="160",height="50",
                                Format="image/png",
                                OnlineResource = new OGC.Schema.wms_1_3_0.OnlineResource(){
                                   href= OnlineResourcePath() + "?SERVICE=WMS" + TokenParameter(arguments) + "&VERSION=1.3.0&REQUEST=GetLegendGraphic&layer="+GetWmsServiceName(serviceInfos)+"&format=image/png"
                               }
                            }
                        }
                    }
                };

        #region SRS
        List<string> epsgCodes = new List<string>();
        foreach (int crs in SupportedCrs(serviceInfos))
        {
            epsgCodes.Add("EPSG:" + crs);
        }

        capabilities.Capability.Layer[0].CRS = epsgCodes.ToArray();

        Envelope initialExtent = InitialExtent(serviceInfos);

        List<OGC.Schema.wms_1_3_0.BoundingBox> bboxes = new List<OGC.Schema.wms_1_3_0.BoundingBox>();
        foreach (int supportedCrs in SupportedCrs(serviceInfos))
        {
            Envelope extent = new Envelope(initialExtent);

            using (var transformer = new WebMapping.Core.Geometry.GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, 4326, supportedCrs))
            {
                transformer.Transform(extent);
                extent = SwapToAxesDirection(arguments, extent, ApiGlobals.SRefStore.SpatialReferences.ById(supportedCrs));
            }

            OGC.Schema.wms_1_3_0.BoundingBox wmsBBox = new OGC.Schema.wms_1_3_0.BoundingBox();
            wmsBBox.CRS = "EPSG:" + supportedCrs;
            wmsBBox.minx = extent.MinX;
            wmsBBox.miny = extent.MinY;
            wmsBBox.maxx = extent.MaxX;
            wmsBBox.maxy = extent.MaxY;
            bboxes.Add(wmsBBox);
        }
        capabilities.Capability.Layer[0].BoundingBox = bboxes.ToArray();
        capabilities.Capability.Layer[0].EX_GeographicBoundingBox = new OGC.Schema.wms_1_3_0.EX_GeographicBoundingBox()
        {
            westBoundLongitude = initialExtent.MinX,
            eastBoundLongitude = initialExtent.MaxX,
            southBoundLatitude = initialExtent.MinY,
            northBoundLatitude = initialExtent.MaxY
        };

        #endregion

        #region Layers
        List<OGC.Schema.wms_1_3_0.Layer> layers = new List<OGC.Schema.wms_1_3_0.Layer>();
        foreach (var serviceInfo in serviceInfos)
        {
            foreach (var layerInfo in serviceInfo.layers)
            {
                if (layerInfo.locked || !layerInfo.IsTocLayer)
                {
                    continue;
                }

                OGC.Schema.wms_1_3_0.Layer layer = layers.Where(l => l.Name == WmsLayerId(serviceInfos, serviceInfo, layerInfo.OgcId)).FirstOrDefault();

                if (layer != null)
                {
                    layer.MaxScaleDenominator = Math.Max(layer.MaxScaleDenominator, layerInfo.MaxScale);
                    layer.MinScaleDenominator = Math.Min(layer.MinScaleDenominator, layerInfo.MinScale);
                    continue;
                }

                layer = new OGC.Schema.wms_1_3_0.Layer();
                layer.MaxScaleDenominator = layerInfo.MaxScale;
                layer.MinScaleDenominator = layerInfo.MinScale;
                layer.Name = WmsLayerId(serviceInfos, serviceInfo, layerInfo.OgcId);
                layer.Title = layerInfo.OgcTitle;
                if (!String.IsNullOrWhiteSpace(layerInfo.@abstract))
                {
                    layer.Abstract = layerInfo.@abstract;
                }

                if (!String.IsNullOrWhiteSpace(layerInfo.metadataurl))
                {
                    layer.MetadataURL = new OGC.Schema.wms_1_3_0.MetadataURL[]{
                        new OGC.Schema.wms_1_3_0.MetadataURL()
                    };
                    layer.MetadataURL[0].Format = layerInfo.metadataformat;
                    layer.MetadataURL[0].OnlineResource = new OGC.Schema.wms_1_3_0.OnlineResource()
                    {
                        href = layerInfo.metadataurl
                    };
                }

                layer.Style = new OGC.Schema.wms_1_3_0.Style[]{
                    new OGC.Schema.wms_1_3_0.Style() {
                        Name="default",
                        Title="default",
                        LegendURL=new OGC.Schema.wms_1_3_0.LegendURL[]{
                            new OGC.Schema.wms_1_3_0.LegendURL(){
                                //width="160",height="50",
                                Format="image/png",
                                OnlineResource = new OGC.Schema.wms_1_3_0.OnlineResource(){
                                   href= OnlineResourcePath() + "?SERVICE=WMS" + TokenParameter(arguments) + "&VERSION=1.3.0&REQUEST=GetLegendGraphic&layer="+layer.Name+"&format=image/png"
                               }
                            }
                        }
                    }
                };

                layer.CRS = capabilities.Capability.Layer[0].CRS;
                layer.BoundingBox = capabilities.Capability.Layer[0].BoundingBox;
                layer.EX_GeographicBoundingBox = capabilities.Capability.Layer[0].EX_GeographicBoundingBox;

                #region Ist Layer Queryable

                bool queryAble = IsQueryableLayer(serviceInfo, layerInfo);
                layer.queryable = queryAble;

                #endregion

                layers.Add(layer);
            }
        }
        capabilities.Capability.Layer[0].Layer1 = layers.ToArray();

        #endregion

        #endregion

        OGC.Schema.Serializer<OGC.Schema.wms_1_3_0.WMS_Capabilities> serializer = new OGC.Schema.Serializer<OGC.Schema.wms_1_3_0.WMS_Capabilities>();
        return serializer.Serialize(capabilities);
    }

    private string WMSGetCapabilities_1_3_0_inspire(ServiceInfoDTO[] serviceInfos, NameValueCollection arguments)
    {
        OGC.Schema.wms_1_3_0_inspire.WMS_Capabilities capabilities = new OGC.Schema.wms_1_3_0_inspire.WMS_Capabilities();
        capabilities.version = "1.3.0";

        capabilities.NameSpaces = new System.Xml.Serialization.XmlSerializerNamespaces();
        capabilities.NameSpaces.Add("ins_vs", "http://inspire.ec.europa.eu/schemas/inspire_vs/1.0");
        capabilities.NameSpaces.Add("ins_com", "http://inspire.ec.europa.eu/schemas/common/1.0");
        capabilities.NameSpaces.Add("sld", "http://www.opengis.net/sld");
        capabilities.NameSpaces.Add("xlink", "http://www.w3.org/1999/xlink");

        #region Service
        capabilities.Service = new OGC.Schema.wms_1_3_0_inspire.Service();
        capabilities.Service.Name = new OGC.Schema.wms_1_3_0_inspire.ServiceName();
        capabilities.Service.Title = GetWmsServiceTitle(serviceInfos);
        capabilities.Service.OnlineResource = new OGC.Schema.wms_1_3_0_inspire.OnlineResource();
        capabilities.Service.Abstract = arguments["append_to_service_abstract"];

        capabilities.Service.OnlineResource.href = OnlineResourcePath() + "?SERVICE=WMS" + TokenParameter(arguments) + "&";
        if (IgnoreAxes(arguments))
        {
            capabilities.Service.OnlineResource.href += "ignore-axes=true&";
        }

        capabilities.Service.ContactInformation = GetInspireContactInformation(GetWmsServiceName(serviceInfos));
        #endregion

        #region Capabilities
        capabilities.Capability = new OGC.Schema.wms_1_3_0_inspire.Capability();
        capabilities.Capability.Request = new OGC.Schema.wms_1_3_0_inspire.Request();
        capabilities.Capability.Request.GetMap = new OGC.Schema.wms_1_3_0_inspire.OperationType();
        capabilities.Capability.Request.GetCapabilities = new OGC.Schema.wms_1_3_0_inspire.OperationType();
        capabilities.Capability.Request.GetFeatureInfo = new OGC.Schema.wms_1_3_0_inspire.OperationType();

        capabilities.Capability._ExtendedCapabilities = GetInspireExtendedCapabilities(GetWmsServiceName(serviceInfos));

        OGC.Schema.wms_1_3_0_inspire.HTTP getHttp = new OGC.Schema.wms_1_3_0_inspire.HTTP();
        getHttp.Get = new OGC.Schema.wms_1_3_0_inspire.Get();
        getHttp.Get.OnlineResource = new OGC.Schema.wms_1_3_0_inspire.OnlineResource();
        getHttp.Get.OnlineResource.href = OnlineResourcePath() + "?SERVICE=WMS" + TokenParameter(arguments) + "&";
        if (IgnoreAxes(arguments))
        {
            getHttp.Get.OnlineResource.href += "ignore-axes=true&";
        }

        OGC.Schema.wms_1_3_0_inspire.DCPType dcpType = new OGC.Schema.wms_1_3_0_inspire.DCPType();
        dcpType.HTTP = getHttp;

        capabilities.Capability.Request.GetMap.DCPType = new OGC.Schema.wms_1_3_0_inspire.DCPType[] { dcpType };
        capabilities.Capability.Request.GetMap.Format = new string[] { "image/jpg", "image/png" };
        capabilities.Capability.Request.GetFeatureInfo.DCPType = new OGC.Schema.wms_1_3_0_inspire.DCPType[] { dcpType };
        capabilities.Capability.Request.GetFeatureInfo.Format = new string[] { "text/html" };
        capabilities.Capability.Request.GetCapabilities.DCPType = new OGC.Schema.wms_1_3_0_inspire.DCPType[] { dcpType };
        capabilities.Capability.Request.GetCapabilities.Format = new string[] { "text/xml" };

        capabilities.Capability.Request.GetLegendGraphic = new OGC.Schema.wms_1_3_0_inspire.OperationType();
        capabilities.Capability.Request.GetLegendGraphic.DCPType = new OGC.Schema.wms_1_3_0_inspire.DCPType[] { dcpType };
        capabilities.Capability.Request.GetLegendGraphic.Format = new string[] { "image/png" };

        capabilities.Capability.Exception = new string[] { "application/vnd.ogc.se_xml" };

        capabilities.Capability.Layer = new OGC.Schema.wms_1_3_0_inspire.Layer[] { new OGC.Schema.wms_1_3_0_inspire.Layer() };
        capabilities.Capability.Layer[0].Name = GetWmsServiceName(serviceInfos);
        capabilities.Capability.Layer[0].Title = GetWmsServiceTitle(serviceInfos);
        capabilities.Capability.Layer[0].Style = new OGC.Schema.wms_1_3_0_inspire.Style[]{
                    new OGC.Schema.wms_1_3_0_inspire.Style() {
                        Name="default",
                        Title="default",
                        LegendURL=new OGC.Schema.wms_1_3_0_inspire.LegendURL[]{
                            new OGC.Schema.wms_1_3_0_inspire.LegendURL(){
                                //width="160",height="50",
                                Format="image/png",
                                OnlineResource = new OGC.Schema.wms_1_3_0_inspire.OnlineResource(){
                                   href= OnlineResourcePath() + "?SERVICE=WMS" + TokenParameter(arguments) + "&VERSION=1.3.0&REQUEST=GetLegendGraphic&layer="+GetWmsServiceName(serviceInfos)+"&format=image/png"
                               }
                            }
                        }
                    }
                };

        #region SRS
        List<string> epsgCodes = new List<string>();
        foreach (int crs in SupportedCrs(serviceInfos))
        {
            epsgCodes.Add("EPSG:" + crs);
        }

        capabilities.Capability.Layer[0].CRS = epsgCodes.ToArray();

        Envelope initialExtent = InitialExtent(serviceInfos);

        List<OGC.Schema.wms_1_3_0_inspire.BoundingBox> bboxes = new List<OGC.Schema.wms_1_3_0_inspire.BoundingBox>();
        foreach (int supportedCrs in SupportedCrs(serviceInfos))
        {
            Envelope extent = new Envelope(initialExtent);

            using (var transformer = new WebMapping.Core.Geometry.GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, 4326, supportedCrs))
            {
                transformer.Transform(extent);
                extent = SwapToAxesDirection(arguments, extent, ApiGlobals.SRefStore.SpatialReferences.ById(supportedCrs));
            }

            OGC.Schema.wms_1_3_0_inspire.BoundingBox wmsBBox = new OGC.Schema.wms_1_3_0_inspire.BoundingBox();
            wmsBBox.CRS = "EPSG:" + supportedCrs;
            wmsBBox.minx = extent.MinX;
            wmsBBox.miny = extent.MinY;
            wmsBBox.maxx = extent.MaxX;
            wmsBBox.maxy = extent.MaxY;
            bboxes.Add(wmsBBox);
        }
        capabilities.Capability.Layer[0].BoundingBox = bboxes.ToArray();
        capabilities.Capability.Layer[0].EX_GeographicBoundingBox = new OGC.Schema.wms_1_3_0_inspire.EX_GeographicBoundingBox()
        {
            westBoundLongitude = initialExtent.MinX,
            eastBoundLongitude = initialExtent.MaxX,
            southBoundLatitude = initialExtent.MinY,
            northBoundLatitude = initialExtent.MaxY
        };

        #endregion

        #region Layers
        List<OGC.Schema.wms_1_3_0_inspire.Layer> layers = new List<OGC.Schema.wms_1_3_0_inspire.Layer>();
        foreach (var serviceInfo in serviceInfos)
        {
            foreach (var layerInfo in serviceInfo.layers)
            {
                if (layerInfo.locked || !layerInfo.IsTocLayer)
                {
                    continue;
                }

                OGC.Schema.wms_1_3_0_inspire.Layer layer = layers.Where(l => l.Name == WmsLayerId(serviceInfos, serviceInfo, layerInfo.OgcId)).FirstOrDefault();

                if (layer != null)
                {
                    layer.MaxScaleDenominator = Math.Max(layer.MaxScaleDenominator, layerInfo.MaxScale);
                    layer.MinScaleDenominator = Math.Min(layer.MinScaleDenominator, layerInfo.MinScale);
                    continue;
                }

                layer = new OGC.Schema.wms_1_3_0_inspire.Layer();
                layer.MaxScaleDenominator = layerInfo.MaxScale;
                layer.MinScaleDenominator = layerInfo.MinScale;
                layer.Name = WmsLayerId(serviceInfos, serviceInfo, layerInfo.OgcId);
                layer.Title = layerInfo.OgcTitle;
                if (!String.IsNullOrWhiteSpace(layerInfo.@abstract))
                {
                    layer.Abstract = layerInfo.@abstract;
                }

                if (!String.IsNullOrWhiteSpace(layerInfo.metadataurl))
                {
                    layer.MetadataURL = new OGC.Schema.wms_1_3_0_inspire.MetadataURL[]{
                        new OGC.Schema.wms_1_3_0_inspire.MetadataURL()
                    };
                    layer.MetadataURL[0].Format = layerInfo.metadataformat;
                    layer.MetadataURL[0].OnlineResource = new OGC.Schema.wms_1_3_0_inspire.OnlineResource()
                    {
                        href = layerInfo.metadataurl
                    };
                }


                layer.Style = new OGC.Schema.wms_1_3_0_inspire.Style[]{
                    new OGC.Schema.wms_1_3_0_inspire.Style() {
                        Name="default",
                        Title="default",
                        LegendURL=new OGC.Schema.wms_1_3_0_inspire.LegendURL[]{
                            new OGC.Schema.wms_1_3_0_inspire.LegendURL(){
                                //width="160",height="50",
                                Format="image/png",
                                OnlineResource = new OGC.Schema.wms_1_3_0_inspire.OnlineResource(){
                                   href= OnlineResourcePath() + "?SERVICE=WMS" + TokenParameter(arguments) + "&VERSION=1.3.0&REQUEST=GetLegendGraphic&layer="+layer.Name+"&format=image/png"
                                }
                            }
                        }
                    }
                };

                layer.CRS = capabilities.Capability.Layer[0].CRS;
                layer.BoundingBox = capabilities.Capability.Layer[0].BoundingBox;
                layer.EX_GeographicBoundingBox = capabilities.Capability.Layer[0].EX_GeographicBoundingBox;

                #region Ist Layer Queryable
                bool queryAble = IsQueryableLayer(serviceInfo, layerInfo);
                layer.queryable = queryAble ? 1 : 0;
                #endregion

                layers.Add(layer);
            }
        }
        capabilities.Capability.Layer[0].Layer1 = layers.ToArray();
        #endregion

        #endregion

        OGC.Schema.Serializer<OGC.Schema.wms_1_3_0_inspire.WMS_Capabilities> serializer = new OGC.Schema.Serializer<OGC.Schema.wms_1_3_0_inspire.WMS_Capabilities>();
        return serializer.Serialize(capabilities);
    }

    #endregion

    #region GetMap

    async public Task<byte[]> GetMapAsync(IRequestContext requestContext,
                                          HttpRequestContextService httpRequestContext,
                                          IUrlHelperService urlHelper,
                                          CacheService _cache,
                                          IMapService[] services,
                                          ServiceInfoDTO[] serviceInfos,
                                          NameValueCollection arguments,
                                          CmsDocument.UserIdentification ui,
                                          MapRestrictions mapRestrictions,
                                          Dictionary<string, string> transformations)
    {
        #region Validate Arguments

        if (services.Length == 0)
        {
            throw new OgcArgumentException("no service definied");
        }

        if (String.IsNullOrWhiteSpace(arguments["bbox"]))
        {
            throw new OgcArgumentException("Parameter bbox is missing");
        }

        if (String.IsNullOrWhiteSpace(arguments["width"]))
        {
            throw new OgcArgumentException("Parameter width is missing");
        }

        if (String.IsNullOrWhiteSpace(arguments["height"]))
        {
            throw new OgcArgumentException("Parameter height is missing");
        }

        if (String.IsNullOrWhiteSpace(arguments["layers"]))
        {
            throw new OgcArgumentException("Parameter layers is missing");
        }

        if (String.IsNullOrWhiteSpace(arguments["srs"]) && String.IsNullOrWhiteSpace(arguments["crs"]))
        {
            throw new OgcArgumentException("Parameter srs/crs is missing");
        }

        if (String.IsNullOrWhiteSpace(arguments["format"]))
        {
            throw new OgcArgumentException("Parameter format is missing");
        }

        #endregion

        #region Map Initialializaion

        var httpService = requestContext.Http;
        var map = services[0].Map;
        map.Environment.SetUserValue(WebGIS.CMS.webgisConst.OutputPath, urlHelper.OutputPath());
        map.Environment.SetUserValue(WebGIS.CMS.webgisConst.OutputUrl, urlHelper.OutputUrl());
        map.Environment.SetUserValue(WebGIS.CMS.webgisConst.UserIdentification, ui);

        if (map is WebMapping.Core.Map)
        {
            ((WebMapping.Core.Map)map).MapRestrictions = mapRestrictions;
        }

        for (int i = services.Length - 1; i >= 0; i--)
        {
            map.Services.Add(services[i]);
        }

        string watermark = GetWatermarkImage(GetWmsServiceName(services));
        if (!String.IsNullOrWhiteSpace(watermark))
        {
            var watermarkService = new WebMapping.GeoServices.Watermark.WatermarkService(String.Empty, watermark);
            await watermarkService.InitAsync(map, requestContext);
            map.Services.Add(watermarkService);
        }

        if (transformations != null)
        {
            foreach (var key in transformations.Keys)
            {
                map.Environment.SetUserValue(key, transformations[key]);
            }
        }

        #endregion

        #region CRS

        string crsId = !String.IsNullOrWhiteSpace(arguments["srs"]) ? arguments["srs"] : arguments["crs"];
        if (!String.IsNullOrEmpty(crsId))
        {
            if (crsId.ToLower().StartsWith("epsg:"))
            {
                crsId = crsId.Split(':')[1];
            }

            int epsgCode = int.Parse(crsId);
            if (!SupportedCrs(serviceInfos).Contains(epsgCode))
            {
                throw new OgcException("Coordinate system " + crsId + " is not supported");
            }

            map.SpatialReference = ApiGlobals.SRefStore.SpatialReferences.ById(int.Parse(crsId));

            foreach (var service in services)
            {
                if (service is IServiceProjection)
                {
                    ((IServiceProjection)service).ProjectionMethode = ServiceProjectionMethode.Map;
                    ((IServiceProjection)service).RefreshSpatialReference();
                }
            }
        }
        else
        {
            throw new OgcException("unknown coordinate system");
        }

        #endregion

        #region Layers

        bool defaultLayerVisibility = arguments["layers"] == GetWmsServiceName(serviceInfos);
        foreach (var service in services)
        {
            ServiceInfoDTO serviceInfo = (from s in serviceInfos where s.id == service.Url select s).FirstOrDefault();
            if (serviceInfo == null)
            {
                throw new OgcException("general exception: no service-info for service");
            }

            var serviceLayerProperties = _cache.GetServiceLayerProperties(serviceInfo.id);

            string[] layerOgcIds = arguments["layers"].Split(',').Where(m => !String.IsNullOrWhiteSpace(m)).ToArray();
            foreach (var layer in service.Layers)
            {
                var layerProperty = serviceLayerProperties?.Where(l => l.Id == layer.ID).FirstOrDefault();
                if (layerProperty != null && layerProperty.Locked)
                {
                    layer.Visible = layerProperty.Locked;
                    continue;
                }

                var layerInfo = serviceInfo.layers.Where(l => l.id == layer.ID).FirstOrDefault();
                if (layerInfo == null || layerInfo.IsTocLayer == false)
                {
                    layer.Visible = false;
                    continue;
                }

                if (layerInfo.locked == true)
                {
                    layer.Visible = layerInfo.visible;
                }
                else
                {
                    bool visible = (!String.IsNullOrWhiteSpace(layerInfo.OgcGroupId) && layerOgcIds.Contains(layerInfo.OgcGroupId)) || layerOgcIds.Contains(layerInfo.OgcId);
                    layer.Visible = defaultLayerVisibility == true ? layer.Visible : visible;
                }
            }
        }

        #endregion

        #region Image Size & Dpi

        map.ImageWidth = int.Parse(arguments["width"]);
        map.ImageHeight = int.Parse(arguments["height"]);
        if (!String.IsNullOrWhiteSpace(arguments["dpi"]))
        {
            map.Dpi = int.Parse(arguments["dpi"]);
        }
        else if (!String.IsNullOrWhiteSpace(arguments["map_resolution"]))
        {
            map.Dpi = int.Parse(arguments["map_resolution"]);
        }

        #endregion

        #region BBOX

        Envelope extent = null;
        if (arguments["bbox"] != null)
        {
            if (arguments["bbox"] == "default")
            {
            }
            else
            {
                bool ignorAxes = arguments["version"] == "1.3.0" ? IgnoreAxes(arguments) : true;

                string[] bbox = arguments["bbox"].Split(',');
                if (!ignorAxes)
                {
                    if ((map.SpatialReference.AxisX == AxisDirection.North || map.SpatialReference.AxisX == AxisDirection.South) &&
                        (map.SpatialReference.AxisY == AxisDirection.West || map.SpatialReference.AxisY == AxisDirection.East))
                    {
                        extent = new WebMapping.Core.Geometry.Envelope(bbox[1].ToPlatformDouble(),
                                                                       bbox[0].ToPlatformDouble(),
                                                                       bbox[3].ToPlatformDouble(),
                                                                       bbox[2].ToPlatformDouble());
                    }
                    else
                    {
                        extent = new WebMapping.Core.Geometry.Envelope(bbox[0].ToPlatformDouble(),
                                                                       bbox[1].ToPlatformDouble(),
                                                                       bbox[2].ToPlatformDouble(),
                                                                       bbox[3].ToPlatformDouble());
                    }
                }
                else
                {

                    extent = new WebMapping.Core.Geometry.Envelope(bbox[0].ToPlatformDouble(),
                                                                   bbox[1].ToPlatformDouble(),
                                                                   bbox[2].ToPlatformDouble(),
                                                                   bbox[3].ToPlatformDouble());
                }
            }
        }
        map.ZoomTo(extent);

        #endregion

        #region Set Locked Filters

        foreach (var service in services)
        {
            ServiceInfoDTO serviceInfo = (from s in serviceInfos where s.id == service.Url select s).FirstOrDefault();
            if (serviceInfo == null)
            {
                continue;
            }

            serviceInfo.ApplyLockedFilters(service, httpRequestContext.OriginalUrlParameters, ui);
        }

        #endregion

        bool transparent = arguments["transparent"] != null && arguments["transparent"].ToLower().Trim() == "true";  // Default "false" wie beim GeoServer
        ArgbColor? transparentColor = null;
        try
        {
            if (!String.IsNullOrWhiteSpace(arguments["bgcolor"]))
            {
                var cc = new WebGIS.CMS.ColorConverter2();

                if (transparent)
                {
                    transparentColor = ArgbColor.FromHexString(arguments["bgcolor"]);
                }

                map.BackgroundColor = ArgbColor.FromHexString(arguments["bgcolor"]);
            }
        }
        catch { }

        WebMapping.Core.ServiceResponses.ServiceResponse serviceResponse = null;

        if (map.Services.Count == 1)
        {
            switch (map.Services[0].ResponseType)
            {
                case WebMapping.Core.ServiceResponseType.Image:
                    serviceResponse = await map.Services[0].GetMapAsync(requestContext);
                    break;
                case WebMapping.Core.ServiceResponseType.Html:
                    if (map.Services[0] is IPrintableService)
                    {
                        serviceResponse = await ((IPrintableService)map.Services[0]).GetPrintMapAsync(requestContext);
                    }

                    break;
            }
        }
        else
        {
            serviceResponse = await map.GetMapAsync(requestContext, transparent, transparentColor);
        }

        if (serviceResponse == null)
        {
            throw new OgcException("No response");
        }

        if (serviceResponse is WebMapping.Core.ServiceResponses.ImageLocation)
        {
            var imgResponse = (WebMapping.Core.ServiceResponses.ImageLocation)serviceResponse;

            var image = await httpService.GetImageAsync(imgResponse.ImagePath, imgResponse.ImageUrl);

            if (image == null)
            {
                throw new OgcException("map image not available (url=" + imgResponse.ImageUrl + ")");
            }

            //if(transparent==true)
            //{
            //    var bm = image is IBitmap ? (IBitmap)image : null;
            //    bm.MakeTransparent(ArgbColor.White);
            //}

            MemoryStream ms = new MemoryStream();
            switch (arguments["format"].ToLower())
            {
                case "image/jpg":
                case "image/jpeg":
                    image.Save(ms, ImageFormat.Jpeg);
                    break;
                case "image/png":
                    image.Save(ms, ImageFormat.Png);
                    break;
            }

            return ms.ToArray();
        }

        throw new OgcException("Unhandeld response type: " + serviceResponse.GetType());
    }

    #endregion

    #region GetFeatureInfo

    async public Task<string> GetFeatureInfo(IRequestContext requestContext,
                                             IUrlHelperService urlHelper,
                                             WebMapping.Core.Abstraction.IMapService[] services,
                                             ServiceInfoDTO[] serviceInfos,
                                             NameValueCollection arguments,
                                             CmsDocument.UserIdentification ui,
                                             HttpContext httpContext)
    {
        string format = "text/html";

        bool ignorAxes = arguments["version"] == "1.3.0" ? IgnoreAxes(arguments) : true;

        #region Validate Arguments

        if (services.Length == 0)
        {
            throw new OgcArgumentException("no service definied");
        }

        if (String.IsNullOrWhiteSpace(arguments["bbox"]))
        {
            throw new OgcArgumentException("Parameter bbox is missing");
        }

        if (String.IsNullOrWhiteSpace(arguments["width"]))
        {
            throw new OgcArgumentException("Parameter width is missing");
        }

        if (String.IsNullOrWhiteSpace(arguments["height"]))
        {
            throw new OgcArgumentException("Parameter height is missing");
        }

        if (arguments["version"] == "1.1.1")
        {
            if (String.IsNullOrWhiteSpace(arguments["x"]))
            {
                throw new OgcArgumentException("Parameter x is missing");
            }

            if (String.IsNullOrWhiteSpace(arguments["y"]))
            {
                throw new OgcArgumentException("Parameter y is missing");
            }
        }
        else if (arguments["version"] == "1.3.0")
        {
            if (String.IsNullOrWhiteSpace(arguments["i"]))
            {
                throw new OgcArgumentException("Parameter i is missing");
            }

            if (String.IsNullOrWhiteSpace(arguments["j"]))
            {
                throw new OgcArgumentException("Parameter j is missing");
            }
        }
        if (String.IsNullOrWhiteSpace(arguments["srs"]) && String.IsNullOrWhiteSpace(arguments["crs"]))
        {
            throw new OgcArgumentException("Parameter srs/crs is missing");
        }

        if (String.IsNullOrWhiteSpace(arguments["info_format"]) && String.IsNullOrWhiteSpace(arguments["infoformat"]))
        {
            throw new OgcArgumentException("Parameter info_format/infoformat is missing");
        }

        if (String.IsNullOrWhiteSpace(arguments["query_layers"]) && String.IsNullOrWhiteSpace(arguments["infoformat"]))
        {
            throw new OgcArgumentException("Parameter query_layers is missing");
        }

        #endregion

        #region Map Initialializaion

        var map = services[0].Map;
        map.Environment.SetUserValue(WebGIS.CMS.webgisConst.OutputPath, urlHelper.OutputPath());
        map.Environment.SetUserValue(WebGIS.CMS.webgisConst.OutputUrl, urlHelper.OutputUrl());

        for (int i = services.Length - 1; i >= 0; i--)
        {
            map.Services.Add(services[i]);
        }

        #endregion

        #region CRS

        string crsId = !String.IsNullOrWhiteSpace(arguments["srs"]) ? arguments["srs"] : arguments["crs"];
        if (!String.IsNullOrEmpty(crsId))
        {
            if (crsId.ToLower().StartsWith("epsg:"))
            {
                crsId = crsId.Split(':')[1];
            }

            int epsgCode = int.Parse(crsId);
            if (!SupportedCrs(serviceInfos).Contains(epsgCode))
            {
                throw new OgcException("Coordinate system " + crsId + " is not supported");
            }

            map.SpatialReference = ApiGlobals.SRefStore.SpatialReferences.ById(int.Parse(crsId));
            foreach (var service in services)
            {
                if (service is IServiceProjection)
                {
                    ((IServiceProjection)service).ProjectionMethode = ServiceProjectionMethode.Map;
                    ((IServiceProjection)service).RefreshSpatialReference();
                }
            }
        }
        else
        {
            throw new OgcException("unknown coordinate system");
        }

        #endregion

        #region Image Size

        map.ImageWidth = int.Parse(arguments["width"]);
        map.ImageHeight = int.Parse(arguments["height"]);

        #endregion

        #region BBox

        Envelope extent = null;
        string[] bbox = arguments["bbox"].Split(',');
        if (!ignorAxes)
        {
            if ((map.SpatialReference.AxisX == AxisDirection.North || map.SpatialReference.AxisX == AxisDirection.South) &&
                (map.SpatialReference.AxisY == AxisDirection.West || map.SpatialReference.AxisY == AxisDirection.East))
            {
                extent = new WebMapping.Core.Geometry.Envelope(bbox[1].ToPlatformDouble(),
                                                               bbox[0].ToPlatformDouble(),
                                                               bbox[3].ToPlatformDouble(),
                                                               bbox[2].ToPlatformDouble());
            }
            else
            {
                extent = new WebMapping.Core.Geometry.Envelope(bbox[0].ToPlatformDouble(),
                                                               bbox[1].ToPlatformDouble(),
                                                               bbox[2].ToPlatformDouble(),
                                                               bbox[3].ToPlatformDouble());
            }
        }
        else
        {

            extent = new WebMapping.Core.Geometry.Envelope(bbox[0].ToPlatformDouble(),
                                                           bbox[1].ToPlatformDouble(),
                                                           bbox[2].ToPlatformDouble(),
                                                           bbox[3].ToPlatformDouble());
        }
        map.ZoomTo(extent);

        #endregion

        #region Info Format
        if (arguments["info_format"] != null)
        {
            format = arguments["info_format"];
        }
        else if (arguments["infoformat"] != null)
        {
            format = arguments["infoformat"];
        }

        #endregion

        #region X,Y

        Envelope queryEnvelope = null;
        int x = 0, y = 0;
        if (arguments["version"] == "1.3.0")
        {
            x = int.Parse(arguments["i"]);
            y = int.Parse(arguments["j"]);
        }
        else
        {
            x = int.Parse(arguments["x"]);
            y = int.Parse(arguments["y"]);
        }
        Point worldPoint = map.ImageToWorld(new Point(x, y));
        queryEnvelope = worldPoint.ShapeEnvelope;

        double tol = 5.0 * map.MapScale / (map.Dpi / 0.0254);
        if (!map.SpatialReference.IsProjective)
        {
            tol = tol / (2.0 * Math.PI * 6378137.0) * 180.0 / Math.PI;   // tol / Umfange == Bogenmaß 
        }
        if (queryEnvelope.Width < double.Epsilon)
        {
            queryEnvelope.MinX -= tol;
            queryEnvelope.MaxX += tol;
        }
        if (queryEnvelope.Height < double.Epsilon)
        {
            queryEnvelope.MinY -= tol;
            queryEnvelope.MaxY += tol;
        }

        #endregion

        StringBuilder ret = new StringBuilder();

        #region Query Layers

        foreach (var service in services)
        {
            ServiceInfoDTO serviceInfo = (from s in serviceInfos where s.id == service.Url select s).FirstOrDefault();
            if (serviceInfo == null)
            {
                throw new OgcException("general exception: no service-info for service");
            }

            string[] layerOgcIds = arguments["layers"].Split(',');
            foreach (var layer in service.Layers)
            {
                var layerInfo = (from l in serviceInfo.layers where l.id == layer.ID select l).FirstOrDefault();
                if (layerInfo == null)
                {
                    continue;
                }

                if (layerOgcIds.Contains(layerInfo.OgcId))
                {
                    var query = GetLayerQuery(service, serviceInfo, layerInfo);
                    if (query == null)
                    {
                        throw new OgcNotAuthorizedException("Quering for " + layerInfo.name + " is not authorized/available");
                    }

                    WebMapping.Core.Api.Bridge.ApiSpatialFilter filter = new WebMapping.Core.Api.Bridge.ApiSpatialFilter()
                    {
                        QueryShape = queryEnvelope,
                        FilterSpatialReference = map.SpatialReference,
                        FeatureSpatialReference = map.SpatialReference
                    };

                    var features = await query.PerformAsync(requestContext, filter);
                    if (features == null || features.Count == 0)
                    {
                        continue;
                    }

                    if (format == "text/html")
                    {
                        await query.InitFieldRendering(requestContext.Http);

                        ret.Append("<h2>" + layerInfo.name + "</h2>");

                        foreach (var feature in features)
                        {
                            ret.Append("<table style='font-family:verdana;font-size:8.25pt;background-color:#808080;' cellpadding=1 cellspacing=1>");
                            foreach (var field in query.Fields)
                            {
                                string val = field.RenderField(feature, httpContext?.Request?.HeadersCollection());
                                if (String.IsNullOrWhiteSpace(val))
                                {
                                    continue;
                                }

                                ret.Append("<tr>");
                                ret.Append("<td style='background-color:#e0e0e0'>");
                                ret.Append(field.ColumnName);
                                ret.Append("</td>");
                                ret.Append("<td style='background-color:#fff'>");
                                ret.Append(val);
                                ret.Append("</td>");
                                ret.Append("</tr>");
                            }

                            ret.Append("</table><br/><br/>");
                        }

                    }
                }
            }
        }


        #endregion

        return ret.ToString();
    }

    #endregion

    #region GetLegendGraphic

    async public Task<byte[]> GetLegendGraphicAsync(IRequestContext requestContext, IUrlHelperService urlHelper, IMapService[] services, ServiceInfoDTO[] serviceInfos, NameValueCollection arguments, CmsDocument.UserIdentification ui)
    {
        #region Validate Arguments

        if (services.Length == 0)
        {
            throw new OgcArgumentException("no service definied");
        }

        if (String.IsNullOrWhiteSpace(arguments["layer"]))
        {
            throw new OgcArgumentException("Parameter layer is missing");
        }

        if (String.IsNullOrWhiteSpace(arguments["format"]))
        {
            throw new OgcArgumentException("Parameter format is missing");
        }
        #endregion

        #region Map Initialializaion

        var map = services[0].Map;
        map.Environment.SetUserValue(WebGIS.CMS.webgisConst.OutputPath, urlHelper.OutputPath());
        map.Environment.SetUserValue(WebGIS.CMS.webgisConst.OutputUrl, urlHelper.OutputUrl());

        #endregion

        var httpService = requestContext.Http;
        IMapService legendService = null;

        #region Layers

        bool defaultLayerVisibility = arguments["layer"] == GetWmsServiceName(serviceInfos);
        foreach (var service in services)
        {
            ServiceInfoDTO serviceInfo = (from s in serviceInfos where s.id == service.Url select s).FirstOrDefault();
            if (serviceInfo == null)
            {
                throw new OgcException("general exception: no service-info for service");
            }

            string[] layerOgcIds = arguments["layer"].Split(',');
            foreach (var layer in service.Layers)
            {
                var layerInfo = (from l in serviceInfo.layers where l.id == layer.ID select l).FirstOrDefault();
                if (layerInfo == null)
                {
                    continue;
                }

                if ((layer.Visible = (defaultLayerVisibility == true ? layer.Visible : layerOgcIds.Contains(layerInfo.OgcId))) == true)
                {
                    legendService = service;
                }
            }
        }

        #endregion

        ServiceResponse serviceResponse = null;

        if (legendService is IServiceLegend)
        {
            ((IServiceLegend)legendService).LegendVisible = true;
            ((IServiceLegend)legendService).LegendOptMethod = LegendOptimization.None;
            serviceResponse = await ((IServiceLegend)legendService).GetLegendAsync(requestContext);
        }

        if (serviceResponse is ImageLocation)
        {
            var imgResponse = (ImageLocation)serviceResponse;

            var bm = await httpService.GetImageAsync(imgResponse.ImagePath, imgResponse.ImageUrl);
            if (bm == null)
            {
                throw new OgcException("map image not available (url=" + imgResponse.ImageUrl + ")");
            }

            MemoryStream ms = new MemoryStream();
            switch (arguments["format"].ToLower())
            {
                case "image/jpg":
                case "image/jpeg":
                    bm.Save(ms, ImageFormat.Jpeg);
                    break;
                case "image/png":
                    bm.Save(ms, ImageFormat.Png);
                    break;
            }

            return ms.ToArray();
        }

        return new byte[0];
    }

    #endregion

    #region Exception

    public string WmsException(System.Exception ex, NameValueCollection arguments)
    {
        string exString = ex.Message;

        if (arguments != null && (arguments["version"] == "1.3.0" || arguments["VERSION"] == "1.3.0"))
        {
            exString = @"<?xml version='1.0' encoding='UTF-8'?>
<ServiceExceptionReport version='1.3.0'
  xmlns='http://www.opengis.net/ogc'
  xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
  xsi:schemaLocation='http://www.opengis.net/ogc http://schemas.opengis.net/wms/1.3.0/exceptions_1_3_0.xsd'>
<ServiceException>" +
ex.Message +
@"</ServiceException>
</ServiceExceptionReport>";
        }
        else  // Version 1.1.1
        {
            exString = @"<?xml version='1.0' encoding='UTF-8' standalone='no' ?>
<!DOCTYPE ServiceExceptionReport SYSTEM
 'http://www.digitalearth.gov/wmt/xml/exception_1_1_1.dtd'>
<ServiceExceptionReport version='1.1.1'>
  <ServiceException>" +
ex.Message +
@"</ServiceException>
</ServiceExceptionReport>";
        }

        return exString;
    }

    #endregion

    #region Helper

    public string OnlineResourcePath(bool forceHttps = false)
    {
        if (forceHttps && _onlineResource.StartsWith("http://"))
        {
            return $"https{_onlineResource.Substring(4)}";
        }

        return _onlineResource;
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

    #region CRS/Axis/Extent

    private int[] SupportedCrs(ServiceInfoDTO[] serviceInfos)
    {
        //int[] supported = new int[] { 4326, 31254, 31255, 31256, 4258, 3857 };
        int[] supported = _defaultSupportedCrs;

        // ???
        //E.WebMapping.Viewer.Viewer4.GetConfigValue("ogc-default-supported-crs").Split(',');

        List<int> ret = new List<int>();
        foreach (var serviceInfo in serviceInfos)
        {
            if (serviceInfo.supportedCrs != null && serviceInfo.supportedCrs.Length > 0)
            {
                foreach (int supportedCrs in serviceInfo.supportedCrs)
                {
                    if (ret.Contains(supportedCrs))
                    {
                        continue;
                    }

                    foreach (var serviceInfo2 in serviceInfos)
                    {
                        if (serviceInfo2.supportedCrs != null && serviceInfo2.supportedCrs.Length > 0 && serviceInfo2.supportedCrs.Contains(supportedCrs))
                        {
                            ret.Add(supportedCrs);
                        }
                    }
                }
            }
        }

        foreach (var serviceInfo in serviceInfos)
        {
            foreach (int crs in supported)
            {
                if (serviceInfo.supportedCrs != null && serviceInfo.supportedCrs.Length > 0 &&
                    !serviceInfo.supportedCrs.Contains(crs))
                {
                    continue;
                }

                if (!ret.Contains(crs))
                {
                    ret.Add(crs);
                }
            }
        }

        return ret.ToArray();
    }

    private Envelope SwapToAxesDirection(NameValueCollection arguments, Envelope env, SpatialReference sRef)
    {
        if (sRef != null && IgnoreAxes(arguments) == false)
        {
            if ((sRef.AxisX == AxisDirection.North || sRef.AxisX == AxisDirection.South) &&
                 (sRef.AxisY == AxisDirection.West || sRef.AxisY == AxisDirection.East))
            {
                return new Envelope(
                    env.MinY, env.MinX,
                    env.MaxY, env.MaxX
                    );
            }
        }

        return new Envelope(env);
    }

    private bool IgnoreAxes(NameValueCollection parameters)
    {
        return parameters["ignore-axes"] != null && parameters["ignore-axes"].ToLower() == "true";
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

    #endregion

    #region Layers

    private bool IsServiceLayer(IMapService[] services, IMapService service, string wmsLayerId)
    {
        if (services.Length == 1)
        {
            return true;
        }

        return wmsLayerId.EndsWith("@" + service.Url);
    }

    private string ServiceLayerId(IMapService[] services, IMapService service, string wmsLayerId)
    {
        if (!IsServiceLayer(services, service, wmsLayerId))
        {
            return String.Empty;
        }

        return wmsLayerId.Split('@')[0];
    }

    private string WmsLayerId(ServiceInfoDTO[] services, ServiceInfoDTO service, string serviceLayerId)
    {
        if (services.Length == 1)
        {
            return serviceLayerId;
        }

        return serviceLayerId + "@" + service.id;
    }

    private bool IsQueryableLayer(ServiceInfoDTO serviceInfo, ServiceInfoDTO.LayerInfo layerInfo)
    {
        if (serviceInfo.queries == null)
        {
            return false;
        }

        return (from q in serviceInfo.queries
                where q.associatedlayers != null && q.associatedlayers.Length > 0 && q.associatedlayers[0].id == layerInfo.id
                select q).FirstOrDefault() != null;
    }

    private QueryDTO GetLayerQuery(IMapService service, ServiceInfoDTO serviceInfo, ServiceInfoDTO.LayerInfo layerInfo)
    {
        var query = (from q in serviceInfo.queries
                     where q.associatedlayers != null && q.associatedlayers.Length > 0 && q.associatedlayers[0].id == layerInfo.id
                     select q).FirstOrDefault();

        if (query != null)
        {
            query.Service = service;
        }

        return query;
    }

    #endregion

    #region Names

    public string GetWmsServiceName(ServiceInfoDTO[] serviceInfos)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var serviceInfo in serviceInfos)
        {
            if (sb.Length > 0)
            {
                sb.Append(",");
            }

            sb.Append(serviceInfo.id);
        }
        return sb.ToString();
    }

    public string GetWmsServiceName(IMapService[] services)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var service in services)
        {
            if (sb.Length > 0)
            {
                sb.Append(",");
            }

            sb.Append(service.Url);
        }
        return sb.ToString();
    }

    public string GetWmsServiceTitle(ServiceInfoDTO[] serviceInfos)
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

    #endregion

    #region Inspire

    private FileInfo GetInspireConfigFile(string serviceName, string fileTitle)
    {
        FileInfo fi = new FileInfo(ApiGlobals.AppEtcPath + "/ogc/wms/inspire/" + serviceName + @"/" + fileTitle);
        if (fi.Exists)
        {
            return fi;
        }

        fi = new FileInfo(ApiGlobals.AppEtcPath + "/ogc/wms/inspire/default/" + fileTitle);
        if (fi.Exists)
        {
            return fi;
        }

        return null;
    }

    private OGC.Schema.wms_1_3_0_inspire.ContactInformation GetInspireContactInformation(string serviceName)
    {
        FileInfo fi = GetInspireConfigFile(serviceName, "contactInformation.config");
        if (fi == null)
        {
            return null;
        }

        string xml;
        using (StreamReader sr = new StreamReader(fi.FullName))
        {
            xml = sr.ReadToEnd();
            sr.Close();
        }

        OGC.Schema.Serializer<OGC.Schema.wms_1_3_0_inspire.WMS_Capabilities> serializer = new OGC.Schema.Serializer<OGC.Schema.wms_1_3_0_inspire.WMS_Capabilities>();
        OGC.Schema.wms_1_3_0_inspire.WMS_Capabilities capabilities = serializer.FromString(xml, Encoding.UTF8);

        return capabilities.Service.ContactInformation;
    }

    private object[] GetInspireExtendedCapabilities(string mapName)
    {
        FileInfo fi = GetInspireConfigFile(mapName, "extendedCapabilities.config");
        if (fi == null)
        {
            return null;
        }

        string xml;
        using (StreamReader sr = new StreamReader(fi.FullName))
        {
            xml = sr.ReadToEnd();
            sr.Close();
        }

        OGC.Schema.Serializer<OGC.Schema.wms_1_3_0_inspire.WMS_Capabilities> serializer = new OGC.Schema.Serializer<OGC.Schema.wms_1_3_0_inspire.WMS_Capabilities>();
        OGC.Schema.wms_1_3_0_inspire.WMS_Capabilities capabilities = serializer.FromString(xml, Encoding.UTF8);

        return capabilities.Capability._ExtendedCapabilities;
    }

    #endregion

    #region Watermark

    private string GetWatermarkImage(string serviceName)
    {
        foreach (string path in new string[]{
                @"/ogc/wms/watermark/" + serviceName + @".png",
                @"/ogc/wms/watermark/" + serviceName + @".gif",
                @"/ogc/wms/watermark/default.png",
                @"/ogc/wms/watermark/default.gif"
        })
        {
            FileInfo fi = new FileInfo(ApiGlobals.AppEtcPath + path);
            if (fi.Exists)
            {
                return fi.FullName;
            }
        }

        return null;
    }

    #endregion

    #endregion 
}