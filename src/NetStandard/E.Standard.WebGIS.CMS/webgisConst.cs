using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using E.Standard.CMS.Core;
using E.Standard.Extensions.Text;
using E.Standard.Parsing.SimpleExpressions;
using E.Standard.Platform;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebGIS.CMS;

public class WebGISConst
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
        => SolveExpression(feature, expression, Helper.GetKeyParameters(expression));

    public static string SolveExpression(
        Feature feature,
        string expression,
        IReadOnlyList<string> keys)
    {
        if (feature == null)
        {
            return expression;
        }

        return SimpleExpression.Solve(expression, keys, new FeatureExpressionValueResolver(feature));
    }

    private readonly struct FeatureExpressionValueResolver : ISimpleExpressionValueResolver
    {
        private const string SpatialPrefix = "spatial::";

        private readonly Feature _feature;

        public FeatureExpressionValueResolver(Feature feature) => _feature = feature;

        public string GetValue(string fieldName) => _feature[fieldName];

        public bool TryParseNumber(string value, out double number) => value.TryToPlatformDouble(out number);

        public string UrlEncodeLatin1(string value) => value.ToLatin1UrlEncoded();

        public bool TryResolveSpecialKey(string key, out string replacement)
        {
            replacement = null;

            bool isBBox = key.Equals("BBOX");
            if (!isBBox && !key.StartsWith(SpatialPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;  // fast path for plain field keys (no allocation)
            }

            string lowerKey = key.ToLowerInvariant();

            if ((isBBox || lowerKey.StartsWith("spatial::bbox")) && _feature.Shape != null)
            {
                replacement = _feature.Shape.ShapeEnvelope.ToBBox(Globals.SpatialReferences, GetSRefIdFromSpatialParameter(key));
                return true;
            }

            if (lowerKey.StartsWith("spatial::point"))
            {
                var point = FirstPointOnShape(GetSRefIdFromSpatialParameter(key));
                replacement = point == null ? null : $"{point.X.ToPlatformNumberString()},{point.Y.ToPlatformNumberString()}";
                return true;
            }

            if (lowerKey is "spatial::latlng" or "spatial::lnglat" or "spatial::lng" or "spatial::lat")
            {
                var point = FirstPointOnShape(4326);
                if (point != null)
                {
                    string lng = point.X.ToPlatformNumberString(), lat = point.Y.ToPlatformNumberString();
                    replacement = lowerKey switch
                    {
                        "spatial::latlng" => $"{lat},{lng}",
                        "spatial::lnglat" => $"{lng},{lat}",
                        "spatial::lng" => lng,
                        _ => lat
                    };
                }
                return true;
            }

            return false;
        }

        private WebMapping.Core.Geometry.Point FirstPointOnShape(int srsId)
            => _feature.Shape?.DeterminePointsOnShape(Globals.SpatialReferences, srsId).FirstOrDefault();
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
