using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace E.Standard.OGC.Schema.wms;

public class CapabilitiesHelper
{
    public static NumberFormatInfo Nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

    private readonly WMS_Vendor _vendor;
    private readonly object _caps = null;
    private readonly WMSLayers _layers = new WMSLayers();
    private readonly string[] _imageFormats = null, _getFeatureInfoFormats = null;
    private readonly string _getMapOnlineResource = String.Empty, _getFeatureInfoOnlineResource = String.Empty, _getLegendGraphicOnlineResource = String.Empty;

    public CapabilitiesHelper(object capabilities, WMS_Vendor vendor = WMS_Vendor.Unknown)
    {
        _caps = capabilities;
        _vendor = vendor;

        if (_caps is wms_1_1_1.WMT_MS_Capabilities)
        {
            wms_1_1_1.WMT_MS_Capabilities caps = (wms_1_1_1.WMT_MS_Capabilities)_caps;

            #region GetMap
            if (caps.Capability.Request.GetMap != null &&
                caps.Capability.Request.GetMap.DCPType != null &&
                caps.Capability.Request.GetMap.DCPType.Length > 0)
            {
                foreach (object http in caps.Capability.Request.GetMap.DCPType[0].HTTP)
                {
                    if (http is OGC.Schema.wms_1_1_1.Get)
                    {
                        _getMapOnlineResource = ((wms_1_1_1.Get)http).OnlineResource.href;
                    }
                }
            }
            if (caps.Capability.Request.GetMap != null)
            {
                _imageFormats = caps.Capability.Request.GetMap.Format;
            }
            #endregion

            #region GetFeatureInfo
            if (caps.Capability.Request.GetFeatureInfo != null &&
                caps.Capability.Request.GetFeatureInfo.DCPType != null &&
                caps.Capability.Request.GetFeatureInfo.DCPType.Length > 0)
            {
                foreach (object http in caps.Capability.Request.GetFeatureInfo.DCPType[0].HTTP)
                {
                    if (http is OGC.Schema.wms_1_1_1.Get)
                    {
                        _getFeatureInfoOnlineResource = ((wms_1_1_1.Get)http).OnlineResource.href;
                    }
                }
            }
            if (caps.Capability.Request.GetFeatureInfo != null)
            {
                _getFeatureInfoFormats = caps.Capability.Request.GetFeatureInfo.Format;
            }
            #endregion

            #region GetLegendGraphic
            if (caps.Capability.Request.GetLegendGraphic != null &&
                caps.Capability.Request.GetLegendGraphic.DCPType != null &&
                caps.Capability.Request.GetLegendGraphic.DCPType.Length > 0)
            {
                foreach (object http in caps.Capability.Request.GetLegendGraphic.DCPType[0].HTTP)
                {
                    if (http is OGC.Schema.wms_1_1_1.Get)
                    {
                        _getLegendGraphicOnlineResource = ((wms_1_1_1.Get)http).OnlineResource.href;
                    }
                }
            }
            #endregion

            #region Layers

            if (caps.Capability.Layer.Layer1 != null)
            {
                for (int i = 0; i < caps.Capability.Layer.Layer1.Length; i++)
                {
                    AddCascadingLayers(caps.Capability.Layer.Layer1[i], null, String.Empty);
                }
            }
            else
            {
                AddCascadingLayers(caps.Capability.Layer, null, String.Empty);
            }

            #endregion
        }
        else if (_caps is wms_1_3_0.WMS_Capabilities)
        {
            wms_1_3_0.WMS_Capabilities caps = (wms_1_3_0.WMS_Capabilities)_caps;

            #region GetMap

            if (caps.Capability.Request.GetMap.DCPType.Length > 0)
            {
                _getMapOnlineResource = caps.Capability.Request.GetMap.DCPType[0].HTTP.Get.OnlineResource.href;
            }

            _imageFormats = caps.Capability.Request.GetMap.Format;

            #endregion

            #region GetFeatureInfo

            if (caps.Capability.Request.GetFeatureInfo != null)
            {
                if (caps.Capability.Request.GetFeatureInfo.DCPType.Length > 0)
                {
                    _getFeatureInfoOnlineResource = caps.Capability.Request.GetFeatureInfo.DCPType[0].HTTP.Get.OnlineResource.href;
                }

                _getFeatureInfoFormats = caps.Capability.Request.GetFeatureInfo.Format;
            }

            #endregion

            #region Layers

            for (int l = 0; l < caps.Capability.Layer.Length; l++)
            {
                AddCascadingLayers(caps.Capability.Layer[l], null, String.Empty);
            }

            #endregion
        }
        //else if (_caps is wms_1_3_0.WMS_Capabilities)
        //{
        //    wms_1_3_0.WMS_Capabilities caps = (wms_1_3_0.WMS_Capabilities)_caps;

        //    #region GetMap
        //    if (caps.Capability.Request.GetMap.DCPType.Length > 0)
        //        _getMapOnlineResource = caps.Capability.Request.GetMap.DCPType[0].HTTP.Get.OnlineResource.href;

        //    _imageFormats = caps.Capability.Request.GetMap.Format;
        //    #endregion

        //    #region GetFeatureInfo
        //    if (caps.Capability.Request.GetMap.DCPType.Length > 0)
        //        _getFeatureInfoOnlineResource = caps.Capability.Request.GetFeatureInfo.DCPType[0].HTTP.Get.OnlineResource.href;

        //    _getFeatureInfoFormats = caps.Capability.Request.GetFeatureInfo.Format;
        //    #endregion

        //    #region Layers
        //    for (int l = 0; l < caps.Capability.Layer.Length; l++)
        //    {
        //        AddCascadingLayers(caps.Capability.Layer[l], null, String.Empty);
        //    }
        //    #endregion
        //}
    }

    //private void AddCascadingLayers(wms_1_3_0_inspire.Layer layer, WMSLayer parentLayer, string parentName)
    //{
    //    if (layer == null)
    //    {
    //        return;
    //    }

    //    layer.Name = layer.Name?.Trim();
    //    layer.Title = layer.Title?.Trim();

    //    if (layer.Layer1 != null)
    //    {
    //        parentName = String.IsNullOrEmpty(parentName) ? layer.Title : parentName + "/" + layer.Title;

    //        #region Add Group Layer

    //        WMSLayer wmslayer = parentLayer;
    //        if (!String.IsNullOrWhiteSpace(layer.Name))
    //        {
    //            wmslayer = CreateWMSLayer(layer, parentLayer, parentName);
    //            _layers.Add(wmslayer);
    //        }

    //        #endregion

    //        for (int i = 0; i < layer.Layer1.Length; i++)
    //        {
    //            AddCascadingLayers(layer.Layer1[i], wmslayer, parentName);
    //        }
    //    }
    //    else
    //    {
    //        string title = String.IsNullOrEmpty(parentName) ? layer.Title : parentName + "/" + layer.Title;
    //        WMSLayer wmslayer = CreateWMSLayer(layer, parentLayer, title);
    //        _layers.Add(wmslayer);
    //    }
    //}

    private WMSLayer CreateWMSLayer(wms_1_3_0_inspire.Layer layer, WMSLayer parentLayer, string title)
    {
        string name = String.IsNullOrEmpty(layer.Name) ? layer.Title : layer.Name;

        WMSLayer wmslayer = new WMSLayer(name, title);
        if (layer.Style != null && layer.Style.Length > 0)
        {
            for (int s = 0; s < layer.Style.Length; s++)
            {
                string styleName = layer.Style[s].Name;
                if (styleName.Contains(":"))  // Namespace aus Style wegmachen!! da kann bei Inspire Diensten "inspire_common:DEFAULT" stehen!!!
                {
                    int pos = styleName.IndexOf(":");
                    styleName = styleName.Substring(pos + 1, styleName.Length - pos - 1);
                }
                wmslayer.Styles.Add(new WMSStyle(styleName, layer.Style[s].Title));
            }
        }
        if (layer.MinScaleDenominator > 0.0)
        {
            wmslayer.MinScale = ScaleDenominator2Scale(layer.MinScaleDenominator);
        }

        if (layer.MaxScaleDenominator > 0.0)
        {
            wmslayer.MaxScale = ScaleDenominator2Scale(layer.MaxScaleDenominator);
        }

        wmslayer.ParentLayer = parentLayer;

        return wmslayer;
    }

    private void AddCascadingLayers(wms_1_3_0.Layer layer, WMSLayer parentLayer, string parentName)
    {
        if (layer == null)
        {
            return;
        }

        layer.Name = TrimLayerNameOrTitle(layer.Name);
        layer.Title = TrimLayerNameOrTitle(layer.Title);

        if (layer.Layer1 != null)
        {
            parentName = String.IsNullOrEmpty(parentName) ? layer.Title : parentName + "/" + layer.Title;

            #region Add Group Layer

            WMSLayer wmslayer = parentLayer;
            if (!String.IsNullOrEmpty(layer.Name)) // nur Layer mit Namen sind auch Layer, die man Abrufen kann.
            {
                wmslayer = CreateWMSLayer(layer, parentLayer, parentName);
                _layers.Add(wmslayer);
            }

            #endregion

            for (int i = 0; i < layer.Layer1.Length; i++)
            {
                AddCascadingLayers(layer.Layer1[i], wmslayer, parentName);
            }
        }
        else
        {
            string title = String.IsNullOrEmpty(parentName) ? layer.Title : parentName + "/" + layer.Title;
            WMSLayer wmslayer = CreateWMSLayer(layer, parentLayer, title);
            _layers.Add(wmslayer);
        }
    }

    private WMSLayer CreateWMSLayer(wms_1_3_0.Layer layer, WMSLayer parentLayer, string title)
    {
        string name = String.IsNullOrEmpty(layer.Name) ? layer.Title : layer.Name;

        WMSLayer wmslayer = new WMSLayer(name, title);
        if (layer.Style != null && layer.Style.Length > 0)
        {
            for (int s = 0; s < layer.Style.Length; s++)
            {
                layer.Style[s].Name = TrimLayerNameOrTitle(layer.Style[s].Name);
                layer.Style[s].Title = TrimLayerNameOrTitle(layer.Style[s].Title);

                string styleName = layer.Style[s].Name;
                if (styleName.Contains(":"))  // Namespace aus Style wegmachen!! da kann bei Inspire Diensten "inspire_common:DEFAULT" stehen!!!
                {
                    int pos = styleName.IndexOf(":");
                    styleName = styleName.Substring(pos + 1, styleName.Length - pos - 1);
                }
                wmslayer.Styles.Add(new WMSStyle(styleName, layer.Style[s].Title));
            }
        }
        if (layer.MinScaleDenominator > 0.0)
        {
            wmslayer.MinScale = ScaleDenominator2Scale(layer.MinScaleDenominator);
        }

        if (layer.MaxScaleDenominator > 0.0)
        {
            wmslayer.MaxScale = ScaleDenominator2Scale(layer.MaxScaleDenominator);
        }

        wmslayer.Queryable = layer.queryable;
        wmslayer.ParentLayer = parentLayer;

        return wmslayer;
    }

    private void AddCascadingLayers(wms_1_1_1.Layer layer, WMSLayer parentLayer, string parentName)
    {
        if (layer == null)
        {
            return;
        }

        layer.Name = TrimLayerNameOrTitle(layer.Name);
        layer.Title = TrimLayerNameOrTitle(layer.Title);

        if (layer.Layer1 != null)
        {
            parentName = String.IsNullOrEmpty(parentName) ? layer.Title : parentName + "/" + layer.Title;

            #region Add Group Layer

            WMSLayer wmslayer = parentLayer;
            if (!String.IsNullOrWhiteSpace(layer.Name))
            {
                wmslayer = CreateWMSLayer(layer, parentLayer, parentName);
                _layers.Add(wmslayer);
            }

            #endregion

            for (int i = 0; i < layer.Layer1.Length; i++)
            {
                AddCascadingLayers(layer.Layer1[i], wmslayer, parentName);
            }
        }
        else
        {
            string title = String.IsNullOrEmpty(parentName) ? layer.Title : parentName + "/" + layer.Title;
            WMSLayer wmslayer = CreateWMSLayer(layer, parentLayer, title);
            _layers.Add(wmslayer);
        }
    }

    private WMSLayer CreateWMSLayer(wms_1_1_1.Layer layer, WMSLayer parentLayer, string title)
    {
        string name = String.IsNullOrEmpty(layer.Name) ? layer.Title : layer.Name;

        WMSLayer wmslayer = new WMSLayer(name, title);
        if (layer.Style != null && layer.Style.Length > 0)
        {
            for (int s = 0; s < layer.Style.Length; s++)
            {
                layer.Style[s].Name = TrimLayerNameOrTitle(layer.Style[s].Name);
                layer.Style[s].Title = TrimLayerNameOrTitle(layer.Style[s].Title);

                wmslayer.Styles.Add(new WMSStyle(layer.Style[s].Name, layer.Style[s].Title));
            }
        }

        if (layer.ScaleHint != null)
        {
            try { wmslayer.MinScale = ScaleHint2Scale(double.Parse(layer.ScaleHint.min.Replace(",", "."), Nhi)); }
            catch { }
            try { wmslayer.MaxScale = ScaleHint2Scale(double.Parse(layer.ScaleHint.max.Replace(",", "."), Nhi)); }
            catch { }
        }

        wmslayer.ParentLayer = parentLayer;

        return wmslayer;
    }

    private double ScaleHint2Scale(double scaleHint)
    {
        switch (_vendor)
        {
            case WMS_Vendor.GeoServer2_x:
                return scaleHint;
            default:
                // http://wiki.deegree.org/deegreeWiki/HowToUseScaleHintAndScaleDenominator
                // Scale = (1:)25000
                // pixelwidth = 25000 * 0.00028 = 7
                // ScaleHint = squareroot(pixelwidth2 * 2)
                //
                //              ScaleHint 
                // -> Scele = ---------------
                //            0.00028*Sqrt(2)

                // return scaleHint * 2004.4;
                return scaleHint / 0.00028 / Math.Sqrt(2D);
        }
    }

    private double ScaleDenominator2Scale(double scaleDenominator)
    {
        // 0.28mm Pixelgröße
        //
        // -> wms_dpi= 25.4 / 0.28  =  90.7...
        //
        //
        //              scaleDenominator * 96 (dpi) * 0.28
        //  scale = ------------------------------------------
        //                              25,4

        // return scaleDenominator * 96D * 0.28 / 25.4;

        return scaleDenominator;
    }

    public WMSLayers Layers
    {
        get { return _layers; }
    }

    public WMSLayers LayersWithStyle
    {
        get
        {
            WMSLayers layers = new WMSLayers();

            foreach (WMSLayer layer in _layers)
            {
                if (layer.Styles.Count == 0)
                {
                    layers.Add(layer);
                }
                else
                {
                    foreach (WMSStyle style in layer.Styles.Distinct(new WMSStyleComparer()))
                    {
                        layers.Add(new WMSLayer(layer.Name + "|" + style.Name, layer.Title + " (" + style.Title + ")")
                        {
                            MinScale = layer.MinScale,
                            MaxScale = layer.MaxScale
                        });
                    }
                }
            }

            return layers;
        }
    }

    public WMSLayer LayerByName(string name)
    {
        string oName = name.Split('|')[0];

        foreach (WMSLayer layer in _layers)
        {
            if (layer.Name == oName)
            {
                return layer;
            }
        }
        return null;
    }

    public WMSStyle LayerStyleByName(string name)
    {
        string[] p = name.Split('|');
        if (p.Length != 2)
        {
            return null;
        }

        WMSLayer layer = LayerByName(name);
        if (layer == null)
        {
            return null;
        }

        foreach (WMSStyle style in layer.Styles)
        {
            if (style.Name == p[1])
            {
                return style;
            }
        }

        return null;
    }

    public string[] ImageFormats
    {
        get { return _imageFormats; }
    }
    public string GetMapOnlineResouce
    {
        get { return _getMapOnlineResource; }
    }
    public string[] GetFeatureInfoFormats
    {
        get { return _getFeatureInfoFormats; }
    }
    public string GetFeatureInfoOnlineResouce
    {
        get { return _getFeatureInfoOnlineResource; }
    }
    public string GetLegendGraphicOnlineResource
    {
        get { return _getLegendGraphicOnlineResource; }
    }

    public string GetServiceDescription()
    {
        var description = new StringBuilder();

        try
        {
            if (_caps is wms_1_1_1.WMT_MS_Capabilities)
            {
                var caps = (wms_1_1_1.WMT_MS_Capabilities)_caps;

                if (!String.IsNullOrWhiteSpace(caps.Service?.Abstract) && caps.Service.Abstract.ToLower() != "wms" && caps.Service.Abstract.ToLower() != "ogc:wms")
                {

                    description.Append($"{caps.Service.Abstract}\n");
                }
                if (caps.Service?.ContactInformation != null)
                {
                    AppendStringAttributes(caps.Service.ContactInformation.ContactPersonPrimary, description);
                    AppendStringAttributes(caps.Service.ContactInformation, description);
                }
            }
            else if (_caps is wms_1_3_0.WMS_Capabilities)
            {
                var caps = (wms_1_3_0.WMS_Capabilities)_caps;

                if (!String.IsNullOrWhiteSpace(caps.Service?.Abstract) && caps.Service.Abstract.ToLower() != "wms" && caps.Service.Abstract.ToLower() != "ogc:wms")
                {
                    description.Append($"{caps.Service.Abstract}\n");
                }
                if (caps.Service?.ContactInformation != null)
                {
                    AppendStringAttributes(caps.Service.ContactInformation.ContactPersonPrimary, description);
                    AppendStringAttributes(caps.Service.ContactInformation, description);
                }
            }
        }
        catch { }

        return description.ToString()
            .Replace("&quot;", "\"")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">");
    }

    private void AppendStringAttributes(object instance, StringBuilder sb)
    {
        if (instance == null)
        {
            return;
        }

        foreach (var propertyInfo in instance.GetType().GetProperties())
        {
            if (propertyInfo.PropertyType == typeof(string) && !String.IsNullOrWhiteSpace((string)propertyInfo.GetValue(instance)))
            {
                sb.Append($"{propertyInfo.Name}: {propertyInfo.GetValue(instance)}\n");
            }
        }
    }

    private string TrimLayerNameOrTitle(string name)
    {
        if (String.IsNullOrEmpty(name))
        {
            return name;
        }

        return name.Trim().Replace("\n", "").Replace("\r", "").Replace("\t", "");
    }

    #region Classes
    public class WMSLayers : List<WMSLayer>
    {

    }

    public class WMSLayer
    {
        public string Name = String.Empty;
        public string Title = String.Empty;
        public List<WMSStyle> Styles = new List<WMSStyle>();
        public double MinScale = 0.0;
        public double MaxScale = 0.0;
        public bool Queryable = true;

        public WMSLayer ParentLayer { get; set; }

        public WMSLayer(string name, string title)
        {
            Name = name;
            Title = title;
        }
    }

    public class WMSStyle
    {
        public string Name = String.Empty;
        public string Title = String.Empty;

        public WMSStyle(string name, string title)
        {
            Name = name;
            Title = !String.IsNullOrEmpty(title) ? title : name;
        }
    }

    private class WMSStyleComparer : IEqualityComparer<WMSStyle>
    {
        public bool Equals(WMSStyle x, WMSStyle y)
        {
            return x.Name == y.Name &&
                   x.Title == y.Title;
        }

        public int GetHashCode(WMSStyle obj)
        {
            return 0;
        }
    }

    #endregion
}
