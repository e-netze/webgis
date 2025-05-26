using E.Standard.CMS.Core;
using E.Standard.Extensions.Text;
using E.Standard.Platform;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.WebGIS.CMS;

public class webgisConst
{
    static public string OutputPath { get { return "OutputPath"; } }
    static public string OutputUrl { get { return "OutputUrl"; } }
    static public string EtcPath { get { return "EtcPath"; } }

    static public string BufferColor { get { return "BufferColor"; } }

    static public string UserName { get { return "username"; } }

    static public string UserIdentification { get { return "UserIdentification"; } }

    static public string SessionId { get { return "SessionID"; } }

    static public string AppConfigPath { get { return "AppConfigPath"; } }

    static public string Transformation { get { return "Tranformation"; } }

    static public string ShowWarningInPrintLayout { get { return "show_warnings_in_print_output"; } }
}

public class Globals
{
    public enum EditAttributeMode { FloatingDialog, ModalDialog }
    static public EditAttributeMode EditMode = EditAttributeMode.FloatingDialog;

    public enum EMailUIModes { Normal, Useable }
    static public EMailUIModes EMailUIMode = EMailUIModes.Normal;

    //internal static NumberFormatInfo Nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

    static public WebMapping.Core.Geometry.SpatialReferenceCollection SpatialReferences = new WebMapping.Core.Geometry.SpatialReferenceCollection();

    public static string EncUmlaute(string val, bool umlaute2wildcard)
    {
        val = val.Replace("ä", (umlaute2wildcard) ? "%" : "&#228;");
        val = val.Replace("ö", (umlaute2wildcard) ? "%" : "&#246;");
        val = val.Replace("ü", (umlaute2wildcard) ? "%" : "&#252;");
        val = val.Replace("Ä", (umlaute2wildcard) ? "%" : "&#196;");
        val = val.Replace("Ö", (umlaute2wildcard) ? "%" : "&#214;");
        val = val.Replace("Ü", (umlaute2wildcard) ? "%" : "&#220;");
        val = val.Replace("ß", (umlaute2wildcard) ? "%" : "&#223;");
        return val;
    }

    public static string EncodeXmlString(string str)
    {
        return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }


    static public string ShortName(string fieldname)
    {
        int pos = 0;
        string[] fieldnames = fieldname.Split(';');
        fieldname = "";
        for (int i = 0; i < fieldnames.Length; i++)
        {
            while ((pos = fieldnames[i].IndexOf(".")) != -1)
            {
                fieldnames[i] = fieldnames[i].Substring(pos + 1, fieldnames[i].Length - pos - 1);
            }
            if (fieldname != "")
            {
                fieldname += ";";
            }

            fieldname += fieldnames[i];
        }

        return fieldname;
    }

    public static string SolveExpression(Feature feature, string expression)
    {
        string[] keys = Helper.GetKeyParameters(expression);
        if (keys == null || feature == null)
        {
            return expression;
        }

        foreach (string key in keys)
        {
            int srsId = key.ToLower().StartsWith("spatial::") ? GetSRefIdFromSpatialParameter(key) : 0;
            if ((key.Equals("BBOX") || key.ToLower().StartsWith("spatial::bbox")) && feature.Shape != null)
            {
                expression = expression.Replace($"[{key}]", feature.Shape.ShapeEnvelope.ToBBox(Globals.SpatialReferences, srsId));
            }
            else if (key.ToLower().StartsWith("spatial::point"))
            {
                var point = feature.Shape?.DeterminePointsOnShape(Globals.SpatialReferences, srsId).FirstOrDefault();
                if (point != null)
                {
                    expression = expression.Replace($"[{key}]", point.X.ToPlatformNumberString() + "," + point.Y.ToPlatformNumberString());
                }
            }
            else if (key.ToLower().Equals("spatial::latlng"))
            {
                var point = feature.Shape?.DeterminePointsOnShape(Globals.SpatialReferences, 4326).FirstOrDefault();
                if (point != null)
                {
                    expression = expression.Replace($"[{key}]", point.Y.ToPlatformNumberString() + "," + point.X.ToPlatformNumberString());
                }
            }
            else if (key.ToLower().Equals("spatial::lnglat"))
            {
                var point = feature.Shape?.DeterminePointsOnShape(Globals.SpatialReferences, 4326).FirstOrDefault();
                if (point != null)
                {
                    expression = expression.Replace($"[{key}]", point.X.ToPlatformNumberString() + "," + point.Y.ToPlatformNumberString());
                }
            }
            else if (key.ToLower().Equals("spatial::lng"))
            {
                var point = feature.Shape?.DeterminePointsOnShape(Globals.SpatialReferences, 4326).FirstOrDefault();
                if (point != null)
                {
                    expression = expression.Replace($"[{key}]", point.X.ToPlatformNumberString());
                }
            }
            else if (key.ToLower().Equals("spatial::lat"))
            {
                var point = feature.Shape?.DeterminePointsOnShape(Globals.SpatialReferences, 4326).FirstOrDefault();
                if (point != null)
                {
                    expression = expression.Replace($"[{key}]", point.Y.ToPlatformNumberString());
                }
            }
            else
            {
                if (key.StartsWith("url-encode:"))
                {
                    expression = expression.Replace($"[{key}]", Uri.EscapeDataString(feature[key.RemovePrefixIfPresent("url-encode:")]));
                }
                else if (key.StartsWith("url-encode-latin1:"))
                {
                    expression = expression.Replace($"[{key}]", feature[key.RemovePrefixIfPresent("url-encode-latin1:")].ToLatin1UrlEncoded());
                }
                else if (key.Contains(":"))
                {
                    int pos = key.IndexOf(":");
                    string format = key.Substring(pos + 1, key.Length - pos - 1);
                    string key2 = key.Substring(0, pos);
                    double res;
                    if (feature[key2].TryToPlatformDouble(out res))
                    {
                        expression = expression.Replace($"[{key}]", String.Format("{0:" + format + "}", res));
                    }
                    else
                    {
                        expression = expression.Replace($"[{key}]", String.Format("{0:" + format + "}", feature[key2]));
                    }
                }
                //else if (key.StartsWith("~") && hotlink != null && hotlink.One2N && !String.IsNullOrEmpty(hotlink.HotlinkId))
                //{
                //    string fKey = key.Substring(1, key.Length - 1);
                //    string val = feature[fKey];
                //    if (val.Length < 100)
                //    {
                //        expression = expression.Replace($"[{key}]", val);
                //    }
                //    else
                //    {
                //        string valId = hotlink.HotlinkId + "~" + fKey;
                //        expression = expression.Replace($"[{key}]", "~" + valId);
                //        hotlink[fKey] = val;
                //    }
                //}
                else if (key.StartsWith("~")/* && hotlink == null*/)
                {
                    string fKey = key.Substring(1, key.Length - 1);
                    expression = expression.Replace($"[{key}]", feature[fKey]);
                }
                else if (key.StartsWith("!")) // Zwingender Parameter
                {
                    string fKey = key.Substring(1, key.Length - 1);
                    if (String.IsNullOrEmpty(feature[fKey]))
                    {
                        return String.Empty;
                    }

                    expression = expression.Replace($"[{key}]", feature[fKey]);
                }
                else
                {
                    expression = expression.Replace($"[{key}]", feature[key]);
                }
            }
        }
        return expression;
    }

    static private int GetSRefIdFromSpatialParameter(string parameter)
    {
        parameter = parameter.ToLower();

        if (parameter.StartsWith("spatial::"))
        {
            int pos = parameter.LastIndexOf("::");
            if (pos > 7)
            {
                if (int.TryParse(parameter.Substring(pos + 2), out int srsId))
                {
                    return srsId;
                }
            }
        }

        return 0;
    }

    public static string ExtractValue(string Params, string Param)
    {
        Param = Param.Trim();

        foreach (string a in Params.Split(';'))
        {
            string aa = a.Trim();
            if (aa.ToLower().IndexOf(Param.ToLower() + "=") == 0)
            {
                if (aa.Length == Param.Length + 1)
                {
                    return "";
                }

                return aa.Substring(Param.Length + 1, aa.Length - Param.Length - 1);
            }
        }
        return String.Empty;
    }

    static public string[] KeyParameters(string commandLine, string startingBracket = "[", string endingBracket = "]")
    {
        int pos1 = 0, pos2;
        pos1 = commandLine.IndexOf(startingBracket);
        string parameters = "";

        while (pos1 != -1)
        {
            pos2 = commandLine.IndexOf(endingBracket, pos1);
            if (pos2 == -1)
            {
                break;
            }

            if (parameters != "")
            {
                parameters += ";";
            }

            parameters += commandLine.Substring(pos1 + startingBracket.Length, pos2 - pos1 - endingBracket.Length);
            pos1 = commandLine.IndexOf(startingBracket, pos2);
        }
        if (parameters != "")
        {
            return parameters.Split(';');
        }
        else
        {
            return null;
        }
    }

    static public string ListToString(string[] list)
    {
        if (list == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        foreach (string item in list)
        {
            if (sb.Length > 0)
            {
                sb.Append(";");
            }

            sb.Append(item);
        }
        return sb.ToString();
    }

    static public string[] StringToList(string str)
    {
        if (String.IsNullOrEmpty(str))
        {
            return new string[] { };
        }

        List<string> list = new List<string>();
        foreach (string item in str.Split(';'))
        {
            list.Add(item);
        }

        return list.ToArray();
    }

    static public string GdiXPath(string cmsNamePlusXPath)
    {
        if (cmsNamePlusXPath.Contains(":"))
        {
            int pos = cmsNamePlusXPath.IndexOf(":");
            return cmsNamePlusXPath.Substring(pos + 1, cmsNamePlusXPath.Length - pos - 1);
        }
        return cmsNamePlusXPath;
    }

    static public string FormEnc(string txt)
    {
        return txt.Replace("&", "&amp;").Replace("<", "&lt;").Replace("&gt;", ">");
    }


    static public string NameToUrl(string name)
    {
        if (name == null)
        {
            return String.Empty;
        }

        name = name.ToLower();
        name = name.Replace(" ", "_");
        name = name.Replace("ä", "ae");
        name = name.Replace("ö", "oe");
        name = name.Replace("ü", "ue");
        name = name.Replace("ß", "ss");

        return name;
    }

    static public IServiceCreator ServiceCreator = null;

    static public bool VisibleInServiceMapScale(IMap map, ILayer layer)
    {
        int mins = (int)layer.MinScale,
            maxs = (int)layer.MaxScale;
        if ((mins > 0) && (mins > Math.Round(map.ServiceMapScale + 0.5, 0))) { return false; }
        if ((maxs > 0) && (maxs < Math.Round(map.ServiceMapScale - 0.5, 0))) { return false; }

        return true;
    }

    static public string GetContainerName(CmsDocument doc, string containerUrl)
    {
        if (doc != null)
        {
            CmsNode container = doc.SelectSingleNode(null, "etc/containers/*", "url", containerUrl);
            if (container != null)
            {
                return container.Name;
            }
        }
        return containerUrl;
    }
}
