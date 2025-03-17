using System;
using System.Collections.Generic;
using System.Globalization;

namespace E.Standard.OGC.Schema.wfs;

public class CapabilitiesHelper
{
    public static NumberFormatInfo Nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

    private object _caps = null;

    private string _onlineResource;
    private string _getGFTOnlineResource, _postGFTOnlineResource;
    private string _getDFTOnlineResource, _postDFTOnlineResource;
    private List<FeatureType> _feaureTypes = new List<FeatureType>();

    public CapabilitiesHelper(object capabilities)
    {
        _caps = capabilities;

        if (_caps is wfs_1_0_0.WFS_CapabilitiesType)
        {
            wfs_1_0_0.WFS_CapabilitiesType caps = (wfs_1_0_0.WFS_CapabilitiesType)_caps;

            #region OnlineResources

            if (caps.Capability?.Request?.Items == null || caps.Capability.Request.Items.Length == 0)
            {
                throw new Exception("WFS Capabilities: No Items defined in Capability.Request.Items");
            }

            if (caps.Service.OnlineResource != null)
            {
                _onlineResource = caps.Service.OnlineResource.ToString();
            }

            foreach (object item in caps.Capability.Request.Items)
            {
                if (item is wfs_1_0_0.GetCapabilitiesType)
                {
                    wfs_1_0_0.GetCapabilitiesType gct = (wfs_1_0_0.GetCapabilitiesType)item;
                }
                else if (item is wfs_1_0_0.DescribeFeatureTypeType)
                {
                    wfs_1_0_0.DescribeFeatureTypeType dft = (wfs_1_0_0.DescribeFeatureTypeType)item;
                }
                else if (item is wfs_1_0_0.GetFeatureTypeType)
                {
                    wfs_1_0_0.GetFeatureTypeType gft = (wfs_1_0_0.GetFeatureTypeType)item;
                    foreach (wfs_1_0_0.DCPTypeType dtt in gft.DCPType)
                    {
                        foreach (object http in dtt.HTTP)
                        {
                            if (http is wfs_1_0_0.GetType)
                            {
                                _getGFTOnlineResource = _getGFTOnlineResource = ((wfs_1_0_0.GetType)http).onlineResource;
                            }

                            if (http is wfs_1_0_0.PostType)
                            {
                                _postDFTOnlineResource = _postGFTOnlineResource = ((wfs_1_0_0.PostType)http).onlineResource;
                            }
                        }
                    }
                }
            }

            foreach (object item in caps.Filter_Capabilities.Spatial_Capabilities.Spatial_Operators.Items)
            {

            }

            #endregion

            #region FeatureTypes

            wfs_1_0_0.FeatureTypeListType ftl = caps.FeatureTypeList;

            if (ftl?.FeatureType == null || ftl.FeatureType.Length == 0)
            {
                throw new Exception("WFS Capabilities: No FeatureTypes defined in FeatureTypeList");
            }

            foreach (wfs_1_0_0.FeatureTypeType ft in ftl.FeatureType)
            {
                FeatureType featureType = new FeatureType(ft.Name, ft.Title, ft.SRS);
                if (ft.LatLongBoundingBox != null && ft.LatLongBoundingBox.Length > 0)
                {
                    featureType.LatLongBBox[0] = double.Parse(ft.LatLongBoundingBox[0].minx, Nhi);
                    featureType.LatLongBBox[1] = double.Parse(ft.LatLongBoundingBox[0].miny, Nhi);
                    featureType.LatLongBBox[2] = double.Parse(ft.LatLongBoundingBox[0].maxx, Nhi);
                    featureType.LatLongBBox[3] = double.Parse(ft.LatLongBoundingBox[0].maxy, Nhi);
                }
                _feaureTypes.Add(featureType);
            }

            #endregion
        }
        else if (_caps is wfs_1_1_0.WFS_CapabilitiesType)
        {
            wfs_1_1_0.WFS_CapabilitiesType caps = (wfs_1_1_0.WFS_CapabilitiesType)_caps;

            #region OnlineResources

            if (caps.OperationsMetadata?.Operation == null || caps.OperationsMetadata?.Operation.Length == 0)
            {
                throw new Exception("WFS Capabilities: No Operations defined in OperationsMetadata");
            }

            foreach (wfs_1_1_0.Operation operation in caps.OperationsMetadata.Operation)
            {
                if (operation.DCP == null || operation.DCP.Length == 0)
                {
                    continue;
                }

                switch (operation.name.ToLower())
                {
                    case "getcapabilities":
                        foreach (wfs_1_1_0.DCP dcp in operation.DCP)
                        {
                            wfs_1_1_0.HTTP http = dcp.Item;
                            foreach (object item in http.Items)
                            {
                                if (item is wfs_1_1_0.OnlineResourceType)
                                {
                                    // Get oder Post kann man irgendwie nicht richtig unterscheiden :-(
                                    wfs_1_1_0.OnlineResourceType or = (wfs_1_1_0.OnlineResourceType)item;
                                    _onlineResource = or.href;
                                    break;
                                }
                            }
                        }
                        break;
                    case "describefeaturetype":
                        foreach (wfs_1_1_0.DCP dcp in operation.DCP)
                        {
                            wfs_1_1_0.HTTP http = dcp.Item;
                            for (int i = 0; i < http.Items.Length; i++)
                            {
                                if (http.Items[i] is wfs_1_1_0.OnlineResourceType)
                                {
                                    wfs_1_1_0.OnlineResourceType or = http.Items[i];
                                    if (http.ItemsElementName[i].ToString().ToLower() == "get")
                                    {
                                        _getDFTOnlineResource = or.href;
                                    }
                                    else if (http.ItemsElementName[i].ToString().ToLower() == "post")
                                    {
                                        _postDFTOnlineResource = or.href;
                                    }
                                }
                            }
                        }
                        break;
                    case "getfeature":
                        foreach (wfs_1_1_0.DCP dcp in operation.DCP)
                        {
                            wfs_1_1_0.HTTP http = dcp.Item;
                            for (int i = 0; i < http.Items.Length; i++)
                            {
                                if (http.Items[i] is wfs_1_1_0.OnlineResourceType)
                                {
                                    wfs_1_1_0.OnlineResourceType or = http.Items[i];
                                    if (http.ItemsElementName[i].ToString().ToLower() == "get")
                                    {
                                        _getGFTOnlineResource = or.href;
                                    }
                                    else if (http.ItemsElementName[i].ToString().ToLower() == "post")
                                    {
                                        _postGFTOnlineResource = or.href;
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            #endregion

            #region FeatureTypes

            wfs_1_1_0.FeatureTypeListType ftl = caps.FeatureTypeList;

            if (ftl?.FeatureType == null || ftl.FeatureType.Length == 0)
            {
                throw new Exception("WFS Capabilities: No FeatureTypes defined in FeatureTypeList");
            }

            foreach (wfs_1_1_0.FeatureTypeType ft in ftl.FeatureType)
            {
                string srs = String.Empty;
                for (int i = 0; i < ft.Items.Length; i++)
                {
                    if (ft.ItemsElementName[i].ToString() == "DefaultSRS")
                    {
                        srs = ft.Items[i].ToString();
                        break;
                    }
                }

                FeatureType featureType = new FeatureType(ft.Name.Name, ft.Title, srs, ft.OutputFormats != null ? ft.OutputFormats.Format : null);

                if (ft.WGS84BoundingBox != null && ft.WGS84BoundingBox.Length > 0)
                {
                    featureType.LatLongBBox[0] = double.Parse(ft.WGS84BoundingBox[0].LowerCorner.Split(' ')[0], Nhi);
                    featureType.LatLongBBox[1] = double.Parse(ft.WGS84BoundingBox[0].LowerCorner.Split(' ')[1], Nhi);
                    featureType.LatLongBBox[2] = double.Parse(ft.WGS84BoundingBox[0].UpperCorner.Split(' ')[0], Nhi);
                    featureType.LatLongBBox[3] = double.Parse(ft.WGS84BoundingBox[0].UpperCorner.Split(' ')[1], Nhi);
                }

                _feaureTypes.Add(featureType);
            }

            #endregion
        }
    }

    #region Properties
    public string OnlineResource
    {
        get { return _onlineResource; }
    }

    public string GetFeatureTypeOnlineResourceHttpPost
    {
        get { return _postGFTOnlineResource; }
    }

    public string GetFeatureTypeOnlineResourceHttpGet
    {
        get { return _getGFTOnlineResource; }
    }

    public string GetDescribeFeatureTypeOnlineResourceHttpPost
    {
        get { return _postDFTOnlineResource; }
    }

    public string GetDescribeFeatureTypeOnlineResourceHttpGet
    {
        get { return _getDFTOnlineResource; }
    }

    public FeatureType[] FeatureTypeList
    {
        get { return _feaureTypes.ToArray(); }
    }
    #endregion

    #region Helper Classes
    public class FeatureType
    {
        public string Name, Title, SRS;
        public double[] LatLongBBox = new double[4];
        public string[] OutputFormats;

        public FeatureType(string name, string title, string srs)
        {
            Name = name;
            Title = title;
            SRS = srs;
        }
        public FeatureType(string name, string title, string srs, string[] outputFormats)
        {
            Name = name;
            Title = title;
            SRS = srs;
            OutputFormats = outputFormats;
        }
    }
    #endregion
}
