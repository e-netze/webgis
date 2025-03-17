using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.OGC.Extensions;
using E.Standard.WebMapping.GeoServices.OGC.WFS.Helper;
using System;
using System.Xml;


namespace E.Standard.WebMapping.GeoServices.OGC.GML;

public class FeatureCursor2 : IDisposable
{
    private XmlTextReader _reader = null;
    private XmlNamespaceManager _ns;
    private QueryFilter _filter;
    private ILayer _layer;
    private GmlVersion _gmlVersion;
    private bool _first = true;
    private bool _interpretSrsAxix = true;

    public FeatureCursor2(ILayer layer, XmlTextReader reader, XmlNamespaceManager ns, QueryFilter filter, GmlVersion gmlVersion)
    {
        _ns = ns;
        _filter = filter;
        _layer = layer;
        _gmlVersion = gmlVersion;

        if (reader == null || ns == null || layer == null)
        {
            return;
        }

        try
        {
            _reader = reader;
        }
        catch
        {
            _reader = null;
        }
    }
    public FeatureCursor2(ILayer layer, XmlTextReader reader, XmlNamespaceManager ns, QueryFilter filter, GmlVersion gmlVersion, Filter_Capabilities filterCapabilities, bool interpretSrsAxis)
        : this(layer, reader, ns, filter, gmlVersion)
    {
        _interpretSrsAxix = interpretSrsAxis;
        //
        // wenn Filter schon geometry operation implementiert
        // ist es hier nicht noch einmal zu vergleichen...
        //
        //if (filterCapabilities != null &&
        //    _filter is ISpatialFilter &&
        //    filterCapabilities.SupportsSpatialOperator(((ISpatialFilter)_filter).SpatialRelation))
        //{
        //    _checkGeometryRelation = false;
        //}
    }

    #region IFeatureCursor Member

    public Feature NextFeature
    {
        get
        {
            while (true)
            {
                if (_reader == null)
                {
                    return null;
                }

                if (_first)
                {
                    _first = false;
                    if (!_reader.ReadToFollowing(_layer.ID, _ns.LookupNamespace("myns")))
                    {
                        return null;
                    }
                }
                else
                {
                    //if(!_reader.Read())
                    //    return null;
                    string nodeName = _reader.Name;
                    if (String.IsNullOrEmpty(nodeName))
                    {
                        return null;
                    }

                    if (nodeName.Contains(":"))
                    {
                        nodeName = nodeName.Split(':')[1];
                    }

                    if (_reader.NodeType == XmlNodeType.Element && nodeName == _layer.ID && _reader.NamespaceURI == _ns.LookupNamespace("myns"))
                    {
                        // passt
                    }
                    else
                    {
                        if (!_reader.ReadToFollowing(_layer.ID, _ns.LookupNamespace("myns")))
                        {
                            return null;
                        }
                    }
                }
                string featureString = _reader.ReadOuterXml();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(featureString);
                XmlNode featureNode = doc.ChildNodes[0];

                string srsName = String.Empty;

                Feature feature = new Feature();
                if (featureNode.Attributes["fid"] != null)
                {
                    feature.Oid = IntergerFeatureID(featureNode.Attributes["fid"].Value);
                }
                else if (featureNode.Attributes["gml:id"] != null)
                {
                    feature.Oid = IntergerFeatureID(featureNode.Attributes["gml:id"].Value);
                }
                foreach (XmlNode fieldNode in featureNode.SelectNodes("myns:*", _ns))
                {
                    string fieldName = fieldNode.Name.Split(':')[1];

                    if (fieldName == _layer.ShapeFieldName.Replace("#", ""))
                    {
                        feature.Shape = GeometryTranslator.GML2Geometry(fieldNode.InnerXml, _gmlVersion, out srsName, _interpretSrsAxix);
                    }
                    else
                    {
                        feature.Attributes.Add(new Core.Attribute(fieldName, fieldNode.InnerText));

                        try
                        {
                            if (fieldName == _layer.IdFieldName)
                            {
                                feature.Oid = Convert.ToInt32(fieldNode.InnerText);
                            }
                        }
                        catch { }
                    }
                }

                if (feature.Shape == null)
                {
                    foreach (XmlNode gmlNode in featureNode.SelectNodes("GML:*", _ns))
                    {
                        feature.Shape = GeometryTranslator.GML2Geometry(gmlNode.OuterXml, _gmlVersion, out srsName, _interpretSrsAxix);
                        if (feature.Shape != null)
                        {
                            break;
                        }
                    }
                }

                if (_filter.FeatureSpatialReference != null && _filter.FeatureSpatialReference.EPSG.IsEqualEPSG(srsName) == false)
                {
                    SpatialReference fromSref = GeometryTranslator.FromSrsName(srsName);
                    if (fromSref != null)
                    {
                        GeometricTransformer.Transform2D(feature.Shape,
                            fromSref.Proj4, !fromSref.IsProjective, _filter.FeatureSpatialReference.Proj4, !_filter.FeatureSpatialReference.IsProjective);
                    }
                }

                return feature;
            }
        }
    }

    #endregion

    #region IDisposable Member

    public void Dispose()
    {
        if (_reader != null)
        {
            _reader.Close();
        }

        _reader = null;
    }

    #endregion

    #region Helper

    private int IntergerFeatureID(string id)
    {
        if (id.Contains("."))
        {
            id = id.Substring(id.LastIndexOf(".") + 1, id.Length - id.LastIndexOf(".") - 1);
        }
        int ID;
        if (!int.TryParse(id, out ID))
        {
            ID = -1;
        }

        return ID;
    }

    #endregion
}
