using E.Standard.Platform;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.AXL;

class AxlHelper
{
    #region CoordSys
    static public void AXLaddFeatureCoordsys(ref XmlTextWriter xWriter, string attr)
    {
        if (attr == null || attr == "")
        {
            return;
        }

        if (attr.StartsWith("<FEATURECOORDSYS"))
        {
            xWriter.WriteRaw(attr);
        }
        else
        {
            xWriter.WriteRaw("<FEATURECOORDSYS " + attr + " />");
        }
    }
    static public void AXLaddFilterCoordsys(ref XmlTextWriter xWriter, string attr)
    {
        if (attr == null || attr == "")
        {
            return;
        }

        if (attr.StartsWith("<FILTERCOORDSYS"))
        {
            xWriter.WriteRaw(attr);
        }
        else
        {
            xWriter.WriteRaw("<FILTERCOORDSYS " + attr + " />");
        }
    }
    static public void AXLaddCoordsys(ref XmlTextWriter xWriter, string attr)
    {
        if (attr == null || attr == "")
        {
            return;
        }

        attr = attr.Replace("FILTERCOORDSYS", "COORDSYS").Replace("FEATURECOORDSYS", "COORDSYS");

        if (attr.StartsWith("<COORDSYS"))
        {
            xWriter.WriteRaw(attr);
        }
        else
        {
            xWriter.WriteRaw("<COORDSYS " + attr + " />");
        }
    }
    static public string Coordsys(string tagName, SpatialReference sRef)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<" + tagName + " id='" + sRef.Id + "' />");
        return sb.ToString();
    }
    #endregion

    #region Renderer
    static public string addLabels2Renderer(FeatureLayer layer)
    {
        if (!layer.UseLabelRenderer || layer.LabelRenderer == null)
        {
            return layer.Renderer;
        }

        string renderer = layer.Renderer;
        try
        {
            XmlNode label = layer.LabelRenderer;
            if (label == null)
            {
                return "";
            }

            XmlTextReader render = new XmlTextReader(renderer, XmlNodeType.Element, null);
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(render);

            XmlNodeList group = xmldoc.SelectNodes("GROUPRENDERER");
            if (group.Count == 1)
            {
                renderer = "<GROUPRENDERER>" + group[0].InnerXml + label.OuterXml + "</GROUPRENDERER>";
            }
            else if (group.Count == 0)
            {
                renderer = "<GROUPRENDERER>" + xmldoc.OuterXml + label.OuterXml + "</GROUPRENDERER>";
            }
            else
            {
                renderer = "";
            }
        }
        catch
        {
            return "";
        }
        return renderer;
    }
    #endregion

    #region Features
    static public bool AppendFeatures(ILayer layer, FeatureCollection features, string axl, NumberFormatInfo nfi)
    {
        try
        {
            List<string> dateTimeFields = new List<string>();
            if (layer != null && layer.Fields != null)
            {
                foreach (IField f in layer.Fields)
                {
                    if (f == null)
                    {
                        continue;
                    }

                    if (f.Type == FieldType.Date)
                    {
                        dateTimeFields.Add(Core.Attribute.ShortName(f.Name));
                    }
                }
            }
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(axl);

            foreach (XmlNode featureNode in doc.SelectNodes("//FEATURE"))
            {
                Feature feature = new Feature();

                #region Attributes
                XmlNode fields = featureNode.SelectSingleNode("FIELDS");
                if (fields != null)
                {
                    if (fields.Attributes.Count > 0)
                    {
                        foreach (XmlAttribute attribute in fields.Attributes)
                        {
                            feature.Attributes.Add(
                                new Core.Attribute(attribute.Name, attribute.Value));
                        }
                    }
                    else
                    {
                        foreach (XmlNode field in fields.SelectNodes("FIELD[@name and @value]"))
                        {
                            string name = field.Attributes["name"].Value;
                            string val = field.Attributes["value"].Value;

                            if (dateTimeFields.Contains(Core.Attribute.ShortName(name)))
                            {
                                try
                                {
                                    DateTime td = new DateTime(1970, 1, 1);
                                    td = td.AddMilliseconds(val.ToPlatformDouble());
                                    if (td.TimeOfDay.TotalSeconds == 0.0)
                                    {
                                        val = td.ToShortDateString();
                                    }
                                    else
                                    {
                                        val = td.ToString();
                                    }
                                }
                                catch { }
                            }
                            feature.Attributes.Add(new Core.Attribute(name, val));
                        }
                    }
                }
                #endregion

                #region Geometry/Envelope
                foreach (XmlNode child in featureNode.ChildNodes)
                {
                    Shape shape = Shape.FromArcXML(child, nfi);
                    if (shape == null)
                    {
                        continue;
                    }

                    feature.Shape = shape;
                    if (!(shape is Envelope))
                    {
                        break;
                    }
                }
                #endregion

                string oid = feature[layer.IdFieldName];
                if (!String.IsNullOrEmpty(oid))
                {
                    try
                    {
                        feature.Oid = int.Parse(oid);
                    }
                    catch { }
                }

                #region Network

                if (featureNode.Attributes["type"] != null)
                {
                    try
                    {
                        switch (featureNode.Attributes["type"].Value)
                        {
                            case "network_edge":
                                feature.Oid = int.Parse(featureNode.Attributes["id"].Value);
                                break;
                            case "network_node":
                                feature.Oid = int.Parse(featureNode.Attributes["id"].Value);
                                break;
                        }
                    }
                    catch { }
                }

                #endregion

                features.Add(feature);
            }

            XmlNode featureCount = doc.SelectSingleNode("//FEATURES/FEATURECOUNT[@hasmore]");
            if (featureCount != null)
            {
                features.HasMore = (featureCount.Attributes["hasmore"].Value == "true");
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
    static public bool AppendRasterBands(FeatureCollection features, string axl)
    {
        try
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(axl);

            Feature feature = new Feature();
            foreach (XmlNode bandNode in doc.SelectNodes("//RASTER_INFO/BANDS/BAND[@number and @value]"))
            {
                feature.Attributes.Add(new Core.Attribute("band" + bandNode.Attributes["number"].Value,
                                                      bandNode.Attributes["value"].Value));


            }
            features.Add(feature);
            features.HasMore = false;

            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion

    static public string ColorToString(ArgbColor color)
    {
        return color.R + "," + color.G + "," + color.B;
    }
}
