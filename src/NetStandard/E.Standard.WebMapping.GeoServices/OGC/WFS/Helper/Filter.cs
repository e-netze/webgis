using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Miscellaneous;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.OGC.WFS.Helper;

public class FilterHelper
{
    private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

    #region SubClasses
    private abstract class FilterObject
    {
        abstract public void WriteTo(StreamWriter sw);

        abstract public void BuildFromNode(XmlNode node);
        abstract public bool Check(Feature feature, SpatialReference featureSRef);
    }

    private abstract class LogicalOperator : FilterObject
    {
        private List<FilterObject> _filterObjects = new List<FilterObject>();

        public LogicalOperator()
        {
        }

        public List<FilterObject> FilterObjects
        {
            get { return _filterObjects; }
        }

        public override void BuildFromNode(XmlNode node)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "And":
                        AndOperator newAnd = new AndOperator();
                        newAnd.BuildFromNode(childNode);
                        _filterObjects.Add(newAnd);
                        break;
                    case "Or":
                        OrOperator newOr = new OrOperator();
                        newOr.BuildFromNode(childNode);
                        _filterObjects.Add(newOr);
                        break;
                    case "PropertyIsEqualTo":
                    case "PropertyIsNotEqualTo":
                    case "PropertyIsLessThan":
                    case "PropertyIsGreaterThan":
                    case "PropertyIsLessThanEqualTo":
                    case "PropertyIsGreaterThanEqualTo":
                    case "PropertyIsLessThanOrEqualTo":
                    case "PropertyIsGreaterThanOrEqualTo":
                    case "PropertyIsLike":
                    case "PropertyIsBetween":
                    case "PropertyIsNullCheck":
                        ComparisonOperator newComp = new ComparisonOperator();
                        newComp.BuildFromNode(childNode);
                        _filterObjects.Add(newComp);
                        break;
                }
            }
        }
    }

    private class AndOperator : LogicalOperator
    {
        public override void WriteTo(StreamWriter sw)
        {
            sw.WriteLine("<And>");
            foreach (FilterObject fObject in this.FilterObjects)
            {
                fObject.WriteTo(sw);
            }

            sw.WriteLine("</And>");
        }

        public override bool Check(Feature feature, SpatialReference featureSRef)
        {
            foreach (FilterObject fObject in this.FilterObjects)
            {
                if (!fObject.Check(feature, featureSRef))
                {
                    return false;
                }
            }

            return true;
        }

    }
    private class OrOperator : LogicalOperator
    {
        public override void WriteTo(StreamWriter sw)
        {
            sw.WriteLine("<Or>");
            foreach (FilterObject fObject in this.FilterObjects)
            {
                fObject.WriteTo(sw);
            }

            sw.WriteLine("</Or>");
        }

        public override bool Check(Feature feature, SpatialReference featureSRef)
        {
            foreach (FilterObject fObject in this.FilterObjects)
            {
                if (fObject.Check(feature, featureSRef))
                {
                    return true;
                }
            }

            return false;
        }
    }
    private class ComparisonOperator : FilterObject
    {
        public enum CompOperator
        {
            EqualTo = 0,
            NotEqualTo = 1,
            LessThan = 2,
            GreaterThan = 3,
            LessThanOrEqualTo = 4,
            GreaterThanOrEqualTo = 5,
            Like = 6,
            Between = 7,
            NullCheck = 8
        }

        public String PropertyName = String.Empty;
        public object Literal = String.Empty;
        public CompOperator Operator;
        private WildcardEx _wildcard = null;

        public ComparisonOperator() { }
        public ComparisonOperator(CompOperator op, string propertyName, string literal)
        {
            Operator = op;
            PropertyName = propertyName;
            Literal = literal;

            if (Operator == CompOperator.Like)
            {
                _wildcard = new WildcardEx(Literal.ToString().Replace("%", "*"),
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
        }

        private bool NumericCompare(double val)
        {
            double lit = Convert.ToDouble(Literal.ToString().Replace(",", "."), _nhi);
            switch (Operator)
            {
                case CompOperator.EqualTo:
                    return val == lit;
                case CompOperator.NotEqualTo:
                    return val != lit;
                case CompOperator.GreaterThan:
                    return val > lit;
                case CompOperator.GreaterThanOrEqualTo:
                    return val >= lit;
                case CompOperator.LessThan:
                    return val < lit;
                case CompOperator.LessThanOrEqualTo:
                    return val <= lit;
            }

            return false;
        }
        private bool StringCompare(string val)
        {
            string lit = Literal.ToString();

            switch (Operator)
            {
                case CompOperator.EqualTo:
                    return lit.Equals(val, StringComparison.OrdinalIgnoreCase);
                //case CompOperator.GreaterThan:
                //    return lit > val;
                //case CompOperator.GreaterThanEqualTo:
                //    return lit >=val;
                //case CompOperator.LessThan:
                //    return lit < val;
                //case CompOperator.LessThanEqualTo:
                //    return lit<=val;
                case CompOperator.Like:
                    if (_wildcard != null)
                    {
                        return _wildcard.IsMatch(val);
                    }
                    break;
                case CompOperator.NotEqualTo:
                    return !lit.Equals(val, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
        private bool DateTimeCompare(DateTime val)
        {
            DateTime lit;
            if (!DateTime.TryParse(Literal.ToString(), out lit))
            {
                return false;
            }

            switch (Operator)
            {
                case CompOperator.EqualTo:
                    return lit.Equals(val);
                case CompOperator.GreaterThan:
                    return val > lit;
                case CompOperator.GreaterThanOrEqualTo:
                    return val >= lit;
                case CompOperator.LessThan:
                    return val < lit;
                case CompOperator.LessThanOrEqualTo:
                    return val <= lit;
                case CompOperator.NotEqualTo:
                    return !lit.Equals(val);
            }

            return false;
        }
        private bool Compare(object val)
        {
            Type t = val.GetType();

            if (t == typeof(double) ||
                t == typeof(float) ||
                t == typeof(decimal) ||
                t == typeof(int) ||
                t == typeof(short) ||
                t == typeof(byte) ||
                t == typeof(long))
            {
                return NumericCompare(Convert.ToDouble(val));
            }
            else if (t == typeof(string))
            {
                return StringCompare(val.ToString());
            }
            else if (t == typeof(DateTime))
            {
                return DateTimeCompare((DateTime)val);
            }

            return false;
        }
        public override bool Check(Feature feature, SpatialReference featureSRef)
        {
            if (feature == null)
            {
                return false;
            }

            object val = feature[PropertyName];
            if (val == null || val == DBNull.Value)
            {
                if (Operator == CompOperator.NullCheck)
                {
                    return true;
                }

                return false;
            }

            return Compare(val);
        }

        public override void WriteTo(StreamWriter sw)
        {
            sw.WriteLine("<PropertyIs" + Operator.ToString() + ">");
            sw.WriteLine("<PropertyName>" + PropertyName + "</PropertyName>");
            sw.WriteLine("<Literal>" + Literal + "</Literal>");
            sw.WriteLine("</PropertyIs" + Operator.ToString() + ">");
        }

        public override void BuildFromNode(XmlNode node)
        {
            XmlNode nodePropertyName = node.SelectSingleNode("PropertyName");
            XmlNode nodeLiteral = node.SelectSingleNode("Literal");

            if (nodePropertyName != null)
            {
                PropertyName = nodePropertyName.InnerText;
            }

            if (nodeLiteral != null)
            {
                Literal = nodeLiteral.InnerText;
            }

            switch (node.Name)
            {
                case "PropertyIsEqualTo":
                    Operator = CompOperator.EqualTo;
                    break;
                case "PropertyIsNotEqualTo":
                    Operator = CompOperator.NotEqualTo;
                    break;
                case "PropertyIsLessThan":
                    Operator = CompOperator.LessThan;
                    break;
                case "PropertyIsGreaterThan":
                    Operator = CompOperator.GreaterThan;
                    break;
                case "PropertyIsLessThanEqualTo":
                case "PropertyIsLessThanOrEqualTo":
                    Operator = CompOperator.LessThanOrEqualTo;
                    break;
                case "PropertyIsGreaterThanEqualTo":
                case "PropertyIsGreaterThanOrEqualTo":
                    Operator = CompOperator.GreaterThanOrEqualTo;
                    break;
                case "PropertyIsLike":
                    Operator = CompOperator.Like;
                    _wildcard = new WildcardEx(Literal.ToString().Replace("%", "*"),
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    break;
                case "PropertyIsBetween":
                    Operator = CompOperator.Between;
                    break;
                case "PropertyIsNullCheck":
                    Operator = CompOperator.NullCheck;
                    break;
            }
        }
    }

    /*
    private class SpatialComparisonOperator : FilterObject
    {
        public enum SpatialCompOperator
        {
            BBOX = 0,
            Intersect = 1,
            Intersects = 2,
            Within = 3,
            Contains = 4
        }
        private SpatialFilter sFilter = null;
        private SpatialCompOperator Operator = SpatialCompOperator.BBOX;
        public string PropertyName = String.Empty;
        //public string srsName = String.Empty;

        public SpatialComparisonOperator() { }
        public SpatialComparisonOperator(SpatialCompOperator op, Shape geometry)
        {
            
            //sFilter = new SpatialFilter();
            //sFilter.Geometry = geometry;

            //Operator = op;
            //switch (op)
            //{
            //    case SpatialCompOperator.BBOX:
            //        sFilter.SpatialRelation = spatialRelation.SpatialRelationEnvelopeIntersects;
            //        break;
            //    case SpatialCompOperator.Intersect:
            //    case SpatialCompOperator.Intersects:
            //        sFilter.SpatialRelation = spatialRelation.SpatialRelationIntersects;
            //        break;
            //    case SpatialCompOperator.Contains:
            //        sFilter.SpatialRelation = spatialRelation.SpatialRelationContains;
            //        break;
            //    case SpatialCompOperator.Within:
            //        sFilter.SpatialRelation = spatialRelation.SpatialRelationWithin;
            //        break;
            //}
             
        }

        public string srsName
        {
            get
            {
                if (sFilter != null && sFilter.FilterSpatialReference != null)
                    return sFilter.FilterSpatialReference.Name;

                return String.Empty;
            }
            set
            {
                if (sFilter != null)
                    sFilter.FilterSpatialReference = Viewer4(value);
            }
        }

        public override bool Check(Feature feature, SpatialReference featureSRef)
        {
            if (feature == null || feature.Shape == null ||
                sFilter == null || sFilter.Geometry == null) return false;

            Shape pGeometry = GeometricTransformer.Transform2D(
                sFilter.QueryShape,
                sFilter.FilterSpatialReference,
                featureSRef);
            SpatialFilter filter = new SpatialFilter(null);
            filter.QueryShape = pGeometry;
            //filter.SpatialRelation = sFilter.SpatialRelation;

            return true; // SpatialRelation.Check(filter, feature.Shape);
        }
        public override void WriteTo(StreamWriter sw)
        {
            if (sFilter == null) return;

            //Operator = SpatialCompOperator.BBOX;
            sw.WriteLine("<" + Operator.ToString() + ">");
            sw.WriteLine("<PropertyName>" + PropertyName + "</PropertyName>");
            sw.WriteLine(gView.OGC.GML.GeometryTranslator.Geometry2GML(
                sFilter.Geometry,
                srsName
                ).Replace("<gml:", "<").Replace("</gml:", "</")
                );
            sw.WriteLine("</" + Operator.ToString() + ">");
        }
        public override void BuildFromNode(XmlNode node)
        {
            if (node == null) return;

            switch (node.Name.ToLower())
            {
                case "bbox":
                    Operator = SpatialCompOperator.BBOX;
                    break;
                case "intersect":
                    Operator = SpatialCompOperator.Intersect;
                    break;
                case "intersects":
                    Operator = SpatialCompOperator.Intersects;
                    break;
                case "contains":
                    Operator = SpatialCompOperator.Contains;
                    break;
                case "widthin":
                    Operator = SpatialCompOperator.Within;
                    break;
            }

            sFilter = new SpatialFilter();
            switch (Operator)
            {
                case SpatialCompOperator.BBOX:
                    sFilter.SpatialRelation = spatialRelation.SpatialRelationEnvelopeIntersects;
                    break;
                case SpatialCompOperator.Intersect:
                case SpatialCompOperator.Intersects:
                    sFilter.SpatialRelation = spatialRelation.SpatialRelationIntersects;
                    break;
                case SpatialCompOperator.Contains:
                    sFilter.SpatialRelation = spatialRelation.SpatialRelationContains;
                    break;
                case SpatialCompOperator.Within:
                    sFilter.SpatialRelation = spatialRelation.SpatialRelationWithin;
                    break;
            }
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.Name == "PropertyName")
                {
                    PropertyName = child.InnerText;
                    continue;
                }
                sFilter.Geometry = gView.OGC.GML.GeometryTranslator.GML2Geometry(child.OuterXml);
                if (sFilter.Geometry != null)
                {
                    if (child.Attributes["srsName"] != null)
                        srsName = child.Attributes["srsName"].Value;
                    break;
                }
            }
        }
    }
     * */
    #endregion

    #region Static Members
    static public string ToWFS(ILayer layer, QueryFilter filter, Filter_Capabilities filterCaps, OGC.GML.GmlVersion gmlVersion, bool ignoreAxisDirection = false)
    {
        if (String.IsNullOrEmpty(filter.Where) && !(filter is SpatialFilter))
        {
            return String.Empty;
        }

        if (filterCaps == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();

        sb.Append("<ogc:Filter>");
        if (filter is SpatialFilter && ((SpatialFilter)filter).QueryShape != null)
        {
            SpatialFilter sFilter = filter as SpatialFilter;
            string srsName = (sFilter.FilterSpatialReference != null) ?
                "EPSG:" + sFilter.FilterSpatialReference.Id : String.Empty;

            if (sFilter.Where != String.Empty)
            {
                sb.Append("<ogc:And>");
            }

            string op = filterCaps.SpatialOperator(/*sFilter.SpatialRelation*/);
            sb.Append("  <ogc:" + op + ">");
            sb.Append("    <ogc:PropertyName>" + layer.ShapeFieldName + "</ogc:PropertyName>");

            /*
            switch (sFilter.SpatialRelation)
            {
                case spatialRelation.SpatialRelationMapEnvelopeIntersects:
                case spatialRelation.SpatialRelationEnvelopeIntersects:
                    sw.WriteLine(GML.GeometryTranslator.Geometry2GML(sFilter.Geometry.Envelope, srsName));
                    break;
                case spatialRelation.SpatialRelationWithin:
                case spatialRelation.SpatialRelationIntersects:
                case spatialRelation.SpatialRelationContains:
                    sw.WriteLine(GML.GeometryTranslator.Geometry2GML(
                        filterCaps.SupportsSpatialOperator(sFilter.SpatialRelation) ? sFilter.Geometry : sFilter.Geometry.Envelope,
                        srsName));
                    break;
            }*/
            // Im webGIS immer Intersects:
            sb.Append(GML.GeometryTranslator.Geometry2GML(sFilter.QueryShape, srsName, gmlVersion, ignoreAxisDirection));

            sb.Append("  </ogc:" + op + ">");
            if (sFilter.Where != String.Empty)
            {
                if (filter.Where.ToLower().Contains(" in ("))
                {
                    ParseInWhereClause(filter.Where, sb, gmlVersion);
                }
                else
                {
                    ParseWhereClause(filter.Where, sb, gmlVersion);
                }
                sb.Append("</ogc:And>");
            }
        }
        /*
    else if (filter is FidFilter)
    {
        if (layer.IdFieldName == String.Empty)
        {
            CreateRowIDFilter(layer, filter as IRowIDFilter, sw);
        }
        else
        {
            ParseInWhereClause(((FidFilter)filter).w, sw);
        }
    }
         * */
        else if (filter is QueryFilter)
        {
            if (filter.Where.ToLower().Contains(" in ("))
            {
                ParseInWhereClause(filter.Where, sb, gmlVersion);
            }
            else
            {
                ParseWhereClause(filter.Where, sb, gmlVersion);
            }
        }
        sb.Append("</ogc:Filter>");

        return sb.ToString();
    }

    /*
    static public IQueryFilter FromWFS(XmlNode filter)
    {
        return FromWFS(null, filter);
    }
    static public IQueryFilter FromWFS(IFeatureClass fc, XmlNode filter)
    {
        if (filter == null) {
            QueryFilter empty = new QueryFilter();
            return empty;
        }

        XmlNamespaceManager ns = null;
        if (filter.NamespaceURI != String.Empty)
        {
            ns = new XmlNamespaceManager(filter.OwnerDocument.NameTable);
            ns.AddNamespace("OGC", filter.NamespaceURI);
            ns.AddNamespace("GML", "http://www.opengis.net/gml");
        }
        StringBuilder whereClause = new StringBuilder();
        IQueryFilter newFilter = null;
        ParseWFSFilter(fc, filter, "", ns, whereClause, ref newFilter);

        if (newFilter == null) newFilter = new QueryFilter();
        newFilter.WhereClause = RemoveOuterBrackets(whereClause.ToString());

        return newFilter;
    }
     * */
    static public void AppendSortBy(QueryFilter filter, XmlNode SortBy, GML.GmlVersion gmlVersion)
    {
        if (filter == null || SortBy == null)
        {
            return;
        }

        XmlNamespaceManager ns = null;
        if (SortBy.NamespaceURI != String.Empty)
        {
            ns = new XmlNamespaceManager(SortBy.OwnerDocument.NameTable);
            ns.AddNamespace("OGC", SortBy.NamespaceURI);
        }

        XmlNode sortProperty = SortBy.SelectSingleNode("SortProperty", ns);
        if (sortProperty == null)
        {
            sortProperty = SortBy.SelectSingleNode("OGC:SortProperty", ns);
        }

        if (sortProperty == null)
        {
            return;
        }

        string prop = String.Empty, order = String.Empty;
        foreach (XmlNode child in sortProperty.ChildNodes)
        {
            switch (child.Name)
            {
                case "ogc:PropertyName":
                case "OGC:PropertyName":
                case "PropertyName":
                    prop = child.InnerText;
                    break;
                case "ogc:SortOrder":
                case "OGC:SortOrder":
                case "SortOrder":
                    order = child.InnerText;
                    break;
            }
        }

        /*
        if (!String.IsNullOrEmpty(prop))
        {
            filter.OrderBy = prop;
            if (!String.IsNullOrEmpty(order))
                filter.OrderBy += " " + order;
        }
         * */
    }
    #region Helper

    #region ToWFS
    /*
    private static void CreateRowIDFilter(ILayer fc, IRowIDFilter filter, StreamWriter sw)
    {
        if (fc == null || filter == null || sw == null) return;

        string fcName = XML.Globals.TypeWithoutPrefix(fc.Name);
        foreach (int id in filter.IDs)
        {
            //sw.WriteLine("<ogc:GmlObjectId gml:id=\"" + fcName + "." + id.ToString() + "\" />");
            sw.WriteLine("<ogc:FeatureId fid=\"" + fcName + "." + id.ToString() + "\" />");
        }
    }
    */

    private static void ParseWhereClause(string whereClause, StringBuilder sw, OGC.GML.GmlVersion gmlVersion)
    {
        if (whereClause.StartsWith(OgcWfsLayer.DefaultIdFieldName + "="))
        {
            sw.Append("<ogc:GmlObjectId gml:id=\"" + whereClause.Substring(OgcWfsLayer.DefaultIdFieldName.Length + 1) + "\"/>");
        }
        else
        {
            Tokenizer tokenizer = new Tokenizer(whereClause);
            tokenizer.OnTransformToken += new Tokenizer.TransformTokenEventHandler(tokenizer_OnTransformToken);

            sw.Append(tokenizer.TransformXML((int)gmlVersion));
        }
    }

    static void tokenizer_OnTransformToken(ref string text, int gmlVersion)
    {
        if (text == String.Empty)
        {
            return;
        }

        if (text.LastIndexOf("[") == 0 &&
            text.IndexOf("]") == text.Length - 1)
        {
            return;
        }

        text = ParseFlatWhereClause(text, (OGC.GML.GmlVersion)gmlVersion);
    }

    private static string ParseFlatWhereClause(string whereClause, OGC.GML.GmlVersion gmlVersion)
    {
        StringBuilder sb = new StringBuilder();
        if (whereClause == String.Empty)
        {
            return String.Empty;
        }

        if (whereClause.Contains("(") || whereClause.Contains(")"))
        {
            throw (new Exception("Whereclause to complex: don't use brackets"));
        }
        //Tokenizer tokenizer = new Tokenizer(whereClause);
        //Token rootToken = tokenizer.Token;

        bool useAND = false, useOR = false, useNOT = false;
        useAND = containsLogicalOperator(whereClause, "and");
        useOR = containsLogicalOperator(whereClause, "or");
        useNOT = containsLogicalOperator(whereClause, "not");

        if (useAND == true && useOR == true ||
            useAND == true && useNOT == true ||
            useOR == true && useNOT == true)
        {
            throw (new Exception("Whereclause to complex: Only use one kind of logical operations (and, or, not)"));
        }

        string op = String.Empty;
        if (useAND)
        {
            op = "And";
        }

        if (useOR)
        {
            op = "Or";
        }

        if (useNOT)
        {
            op = "Not";
        }

        string[] properties = SplitByString(whereClause, " " + op + " ");

        if (op != String.Empty)
        {
            sb.Append("<ogc:" + op + ">");
        }

        string matchCase = String.Empty;
        if (gmlVersion == OGC.GML.GmlVersion.v3)
        {
            matchCase = @" matchCase=""false""";
        }

        foreach (string property in properties)
        {
            if (property == String.Empty)
            {
                continue;
            }

            if (property[0] == '[' && property[property.Length - 1] == ']')
            {
                sb.Append(property);
                continue;
            }
            string[] parts = SplitPropertyClause(property);
            string operatorName = "";
            switch (parts[1].ToLower())
            {
                case "like":
                    operatorName = "ogc:PropertyIsLike";
                    sb.Append(@"<ogc:PropertyIsLike wildCard=""%"" singleChar=""?"" escape=""ESC""" + matchCase + ">");
                    break;
                case "=":
                    operatorName = "ogc:PropertyIsEqualTo";
                    sb.Append(@"<ogc:PropertyIsEqualTo" + matchCase + ">");
                    //operatorName = "ogc:PropertyIsLike";
                    //sb.Append(@"<ogc:PropertyIsLike wildCard=""%"" singleChar=""?"" escape=""ESC"">");
                    break;
                case ">":
                    operatorName = "ogc:PropertyIsGreaterThan";
                    sb.Append(@"<ogc:PropertyIsGreaterThan>");
                    break;
                case ">=":
                    operatorName = "ogc:PropertyIsGreaterThanOrEqualTo";
                    sb.Append(@"<ogc:PropertyIsGreaterThanOrEqualTo>");
                    break;
                case "<":
                    operatorName = "ogc:PropertyIsLessThan";
                    sb.Append(@"<ogc:PropertyIsLessThan>");
                    break;
                case "<=":
                    operatorName = "ogc:PropertyIsLessThanOrEqualTo";
                    sb.Append(@"<ogc:PropertyIsLessThanOrEqualTo>");
                    break;
                case "<>":
                    operatorName = "ogc:PropertyIsNotEqualTo";
                    sb.Append(@"<ogc:PropertyIsNotEqualTo>");
                    break;
            }
            sb.Append("<ogc:PropertyName>" + parts[0] + "</ogc:PropertyName>");
            sb.Append("<ogc:Literal>" + parts[2].Replace("\"", "").Replace("'", "") + "</ogc:Literal>");
            sb.Append(@"</" + operatorName + ">");
        }
        if (op != String.Empty)
        {
            sb.Append("</ogc:" + op + ">");
        }

        return sb.ToString();
    }

    private static void ParseInWhereClause(string whereClause, StringBuilder sw, OGC.GML.GmlVersion gmlVersion)
    {
        while (whereClause.IndexOf(" ,") != -1)
        {
            whereClause = whereClause.Replace(" ,", ",");
        }

        while (whereClause.IndexOf(", ") != -1)
        {
            whereClause = whereClause.Replace(", ", ",");
        }

        string[] parts = whereClause.Trim().Split(' ');
        if (parts.Length != 3)
        {
            throw (new Exception("Whereclause to complex..."));
        }

        string field = parts[0];
        string[] ids = parts[2].Substring(1, parts[2].Length - 2).Split(',');

        StringBuilder sb = new StringBuilder();
        foreach (string id in ids)
        {
            if (sb.Length > 0)
            {
                sb.Append(" OR ");
            }

            sb.Append(field + "=" + id);
        }

        ParseWhereClause(sb.ToString(), sw, gmlVersion);
    }
    private static bool containsLogicalOperator(string clause, string op)
    {
        clause = TrimClause(clause).ToLower();
        op = op.ToLower();

        string[] parts = clause.Split(' ');

        foreach (string part in parts)
        {
            if (part == op)
            {
                return true;
            }
        }
        return false;
    }

    private static string TrimClause(string clause)
    {
        clause = clause.Trim();
        while (clause.IndexOf("  ") != -1)
        {
            clause = clause.Replace("  ", " ");
        }

        return clause;
    }

    private static string[] SplitByString(string testString, string split)
    {
        if (split == string.Empty)
        {
            string[] ret = new string[1];
            ret[0] = testString;
            return ret;
        }
        split = split.ToLower();
        string testStringL = testString.ToLower();

        int offset = 0;
        int index = 0;
        int[] offsets = new int[testString.Length + 1];

        while (index < testStringL.Length)
        {
            int indexOf = testStringL.IndexOf(split, index);
            if (indexOf != -1)
            {
                offsets[offset++] = indexOf;
                index = (indexOf + split.Length);
            }
            else
            {
                index = testStringL.Length;
            }
        }

        string[] final = new string[offset + 1];
        if (offset == 0)
        {
            final[0] = testString;
        }
        else
        {
            offset--;
            final[0] = testString.Substring(0, offsets[0]);
            for (int i = 0; i < offset; i++)
            {
                final[i + 1] = testString.Substring(offsets[i] + split.Length, offsets[i + 1] - offsets[i] - split.Length);
            }
            final[offset + 1] = testString.Substring(offsets[offset] + split.Length);
        }
        return final;
    }

    private static string[] SplitPropertyClause(string clause)
    {
        clause = clause.Trim();

        string clause2 = clause.ToLower();

        if (clause2.Contains(">="))
        {
            return SplitPropertyClause(clause, ">=");
        }
        else if (clause2.Contains("<="))
        {
            return SplitPropertyClause(clause, "<=");
        }
        else if (clause2.Contains("<>"))
        {
            return SplitPropertyClause(clause, "<>");
        }
        else if (clause2.Contains(">"))
        {
            return SplitPropertyClause(clause, ">");
        }
        else if (clause2.Contains("<"))
        {
            return SplitPropertyClause(clause, "<");
        }
        else if (clause2.Contains("="))
        {
            return SplitPropertyClause(clause, "=");
        }
        else if (clause2.ToLower().Contains(" like "))
        {
            return SplitPropertyClause(clause, " like ");
        }
        else
        {
            throw new Exception("Parser Error: Unknown operator in clause " + clause);
        }
    }

    private static string[] SplitPropertyClause(string clause, string op)
    {
        int pos = clause.ToLower().IndexOf(op.ToLower());
        if (pos == 0)
        {
            throw new Exception("Parse Error: " + clause + " do not contain " + op);
        }

        string[] parts = new string[3];
        parts[0] = (clause.Substring(0, pos)).Trim();
        parts[1] = op.Trim();
        parts[2] = (clause.Substring(pos + op.Length, clause.Length - pos - op.Length)).Trim();

        return parts;
    }
    #endregion

    #region FromWFS
    /*
    private static void ParseWFSFilter(IFeatureClass fc, XmlNode parentNode, string LogicalOperator, XmlNamespaceManager ns, StringBuilder sb, ref IQueryFilter newFilter)
    {
        if (parentNode == null) return;

        foreach (XmlNode child in parentNode.ChildNodes)
        {
            XmlNode gmlNode = null;

            switch (child.Name)
            {
                case "ogc:And":
                case "And":
                    StringBuilder sb1 = new StringBuilder();
                    ParseWFSFilter(fc, child, "AND", ns, sb1, ref newFilter);
                    AppendClause(LogicalOperator, "(" + sb1.ToString() + ")", sb);
                    //sb.Append("(" + sb1.ToString() + ")");
                    break;
                case "ogc:Or":
                case "Or":
                    StringBuilder sb2 = new StringBuilder();
                    ParseWFSFilter(fc, child, "OR", ns, sb2, ref newFilter);
                    AppendClause(LogicalOperator, "(" + sb2.ToString() + ")", sb);
                    //sb.Append("(" + sb2.ToString() + ")");
                    break;
                case "ogc:Not":
                case "Not":
                    StringBuilder sb3 = new StringBuilder();
                    ParseWFSFilter(fc, child, "NOT", ns, sb3, ref newFilter);
                    AppendClause(LogicalOperator, "(" + sb3.ToString() + ")", sb);
                    //sb.Append("(" + sb3.ToString() + ")");
                    break;
                case "ogc:BBOX":
                case "BBOX":
                    if (newFilter != null)
                        throw new Exception("Invalid or corrupt Filter!!!");
                    newFilter = new SpatialFilter();
                    ((ISpatialFilter)newFilter).SpatialRelation = spatialRelation.SpatialRelationEnvelopeIntersects;
                    gmlNode = (ns != null) ? child.SelectSingleNode("GML:*", ns) : child.ChildNodes[0];
                    break;
                case "ogc:Intersects":
                case "Intersects":
                    if (newFilter != null)
                        throw new Exception("Invalid or corrupt Filter!!!");
                    newFilter = new SpatialFilter();
                    ((ISpatialFilter)newFilter).SpatialRelation = spatialRelation.SpatialRelationIntersects;
                    gmlNode = (ns != null) ? child.SelectSingleNode("GML:*", ns) : child.ChildNodes[0];
                    break;
                case "ogc:Within":
                case "Within":
                    if (newFilter != null)
                        throw new Exception("Invalid or corrupt Filter!!!");
                    newFilter = new SpatialFilter();
                    ((ISpatialFilter)newFilter).SpatialRelation = spatialRelation.SpatialRelationContains;
                    gmlNode = (ns != null) ? child.SelectSingleNode("GML:*", ns) : child.ChildNodes[0];
                    break;
                case "ogc:Contains":
                case "Contains":
                    if (newFilter != null)
                        throw new Exception("Invalid or corrupt Filter!!!");
                    newFilter = new SpatialFilter();
                    ((ISpatialFilter)newFilter).SpatialRelation = spatialRelation.SpatialRelationContains;
                    gmlNode = (ns != null) ? child.SelectSingleNode("GML:*", ns) : child.ChildNodes[0];
                    break;
                default:
                    string clause = ParseWFSFilterNode(fc, child, ns);
                    AppendClause(LogicalOperator, clause, sb);
                    break;
            }

            if (gmlNode != null && newFilter is ISpatialFilter)
            {
                ((ISpatialFilter)newFilter).Geometry = GeometryTranslator.GML2Geometry(gmlNode.OuterXml);
                if (((ISpatialFilter)newFilter).Geometry == null)
                    throw new Exception("Unknown GML Geometry...");

                if (gmlNode.Attributes["srsName"] != null)
                    ((ISpatialFilter)newFilter).FilterSpatialReference = SpatialReference.FromID(gmlNode.Attributes["srsName"].Value);
            }
        }
    }

    private static string ParseWFSFilterNode(IFeatureClass fc, XmlNode node, XmlNamespaceManager ns)
    {
        //if(fc==null) 
        //    throw new ArgumentException("ParseWFSFilterNode: FeatureClass is null...");
        if (node == null) return String.Empty;

        string op = String.Empty;
        switch (node.Name)
        {
            case "PropertyIsEqualTo":
            case "ogc:PropertyIsEqualTo":
                op = "=";
                break;
            case "PropertyIsLike":
            case "ogc:PropertyIsLike":
                op = " like ";
                break;
            case "PropertyIsGreaterThan":
            case "ogc:PropertyIsGreaterThan":
                op = ">";
                break;
            case "PropertyIsGreaterThanOrEqualTo":
            case "ogc:PropertyIsGreaterThanOrEqualTo":
                op = ">=";
                break;
            case "PropertyIsLessThan":
            case "ogc:PropertyIsLessThan":
                op = "<";
                break;
            case "PropertyIsLessThanOrEqualTo":
            case "ogc:PropertyIsLessThanOrEqualTo":
                op = "<=";
                break;
            case "PropertyIsNotEqualTo":
            case "ogc:PropertyIsNotEqualTo":
                op = "<>";
                break;
            default:
                throw new Exception("Unknown operation: " + node.Name + ". Xml is case sensitive.");
        }

        XmlNodeList propertyName = (ns != null) ? node.SelectNodes("OGC:PropertyName", ns) : node.SelectNodes("PropertyName");
        if (propertyName.Count > 1)
        {
            throw new Exception("More than one PropertyName in Property: " + node.OuterXml);
        }
        if (propertyName.Count == 0)
        {
            throw new Exception("No PropertyName in Property: " + node.OuterXml);
        }
        XmlNodeList Literal = (ns != null) ? node.SelectNodes("OGC:Literal", ns) : node.SelectNodes("Literal");
        if (Literal.Count > 1)
        {
            throw new Exception("More than one Literal in Property: " + node.OuterXml);
        }
        if (Literal.Count == 0)
        {
            throw new Exception("No Literal in Property: " + node.OuterXml);
        }

        string fieldname = propertyName[0].InnerText;
        string fieldvalue = Literal[0].InnerText;

        if (fc != null)
        {
            IField field = fc.FindField(fieldname);
            if (field == null)
            {
                throw new Exception("Type " + fc.Name + " do not has field " + fieldname);
            }

            switch (field.type)
            {
                case FieldType.String:
                    fieldvalue = "'" + fieldvalue + "'";
                    break;
                case FieldType.Date:
                    fieldvalue = "'" + fieldvalue + "'";
                    break;
            }
        }

        return fieldname + op + fieldvalue;
    }

    private static void AppendClause(string op, string clause, StringBuilder sb)
    {
        if (clause == String.Empty) return;

        if (sb.Length > 0)
            sb.Append(" " + op + " ");
        sb.Append(clause);
    }

    private static string RemoveOuterBrackets(string sql)
    {
        if (sql == null || sql == String.Empty) return String.Empty;

        byte[] c = new byte[sql.Length];
        int pos = 0;
        for (int i = 0; i < sql.Length; i++)
        {
            if (sql[i] == '(')
                c[i] = (byte)++pos;
            if (sql[i] == ')')
                c[i] = (byte)pos--;
        }

        if (c[0] != 1 || c[c.Length - 1] != 1) return sql;
        if (CountValues(c, 1) == 2)
        {
            return RemoveOuterBrackets(sql.Substring(1, sql.Length - 2));
        }
        else
        {
            return sql;
        }
    }
    private static int CountValues(byte[] array, byte val)
    {
        int counter = 0;
        for (int i = 0; i < array.Length; i++)
            if (array[i] == val)
                counter++;

        return counter;
    }
     * */
    #endregion

    #endregion

    #endregion
}

public class Filter_Capabilities
{
    #region Declarations
    private List<string> GeometryOperands = new List<string>();
    private List<string> SpatialOperators = new List<string>();
    private List<string> ComparisonOperators = new List<string>();
    private List<string> SimpleArithmeticOperators = new List<string>();
    private List<string> ArithmeticFunctions = new List<string>();
    #endregion

    public Filter_Capabilities()  // Default Constructor
    {
        SpatialOperators.Add("BBOX");
        SpatialOperators.Add("Contains");
        SpatialOperators.Add("Intersects");
        SpatialOperators.Add("Within");
    }

    public Filter_Capabilities(XmlNode node, XmlNamespaceManager ns)
    {
        if (node == null)
        {
            return;
        }

        #region GeometryOperands
        foreach (XmlNode n in node.SelectNodes("OGC:Spatial_Capabilities/OGC:GeometryOperands/OGC:GeometryOperand", ns))
        {
            GeometryOperands.Add(n.InnerText);
        }
        foreach (XmlNode n in node.SelectNodes("OGC:Spatial_Capabilities/OGC:Geometry_Operands", ns))
        {
            foreach (XmlNode c in n.ChildNodes)
            {
                GeometryOperands.Add(TypeWithoutPrefix(c.Name));
            }
        }
        #endregion

        #region SpatialOperators
        foreach (XmlNode n in node.SelectNodes("OGC:Spatial_Capabilities/OGC:SpatialOperators/OGC:SpatialOperator[@name]", ns))
        {
            SpatialOperators.Add(n.Attributes["name"].Value);
        }
        foreach (XmlNode n in node.SelectNodes("OGC:Spatial_Capabilities/OGC:Spatial_Operators", ns))
        {
            foreach (XmlNode c in n.ChildNodes)
            {
                SpatialOperators.Add(TypeWithoutPrefix(c.Name));
            }
        }
        #endregion

        #region ComparisonOperators
        foreach (XmlNode n in node.SelectNodes("OGC:Scalar_Capabilities/OGC:ComparisonOperators/OGC:ComparisonOperator", ns))
        {
            ComparisonOperators.Add(n.InnerText);
        }
        foreach (XmlNode n in node.SelectNodes("OGC:Scalar_Capabilities/OGC:Comparison_Operators", ns))
        {
            foreach (XmlNode c in n.ChildNodes)
            {
                ComparisonOperators.Add(TypeWithoutPrefix(c.Name));
            }
        }
        #endregion

        #region ArithmeticFunctions
        foreach (XmlNode n in node.SelectNodes("OGC:Scalar_Capabilities/OGC:ArithmeticOperators/OGC:Functions/OGC:FunctionNames/OGC:FunctionName", ns))
        {
            ArithmeticFunctions.Add(n.InnerText);
        }
        foreach (XmlNode n in node.SelectNodes("OGC:Scalar_Capabilities/OGC:Arithmetic_Operators/OGC:Functions", ns))
        {
            foreach (XmlNode c in n.ChildNodes)
            {
                ArithmeticFunctions.Add(TypeWithoutPrefix(c.Name));
            }
        }
        #endregion

        // keine Ahnung wie so was ausschaut
        //#region SimpleArithmeticOperators
        //foreach (XmlNode n in node.SelectNodes("OGC:Scalar_Capabilities/OGC:ArithmeticOperators/OGC:SimpleArithmetic", ns))
        //{
        //    SimpleArithmeticOperators.Add(n.InnerText);
        //}
        //foreach (XmlNode n in node.SelectNodes("OGC:Scalar_Capabilities/OGC:Arithmetic_Operators/OGC:SimpleArithmetic", ns))
        //{
        //    SimpleArithmeticOperators.Add(n.InnerText);
        //}
        //#endregion
    }

    private string TypeWithoutPrefix(string type)
    {
        if (type.Contains(":"))
        {
            return type.Substring(type.IndexOf(":") + 1, type.Length - type.IndexOf(":") - 1);
        }
        else
        {
            return type;
        }
    }

    #region Supports...
    private string Supports(List<string> list, string op)
    {
        foreach (string l in list)
        {
            if (l.ToLower() == op.ToLower())
            {
                return l;
            }
        }
        return String.Empty;
    }
    public bool SuportsGeometryOperand(string op)
    {
        return Supports(GeometryOperands, op) != String.Empty;
    }
    public bool SupportsSpatialOperator(string op)
    {
        return Supports(SpatialOperators, op) != String.Empty;
    }
    public bool SupportsSpatialOperator(/*spatialRelation relation*/)
    {
        return true;

        /*
        switch (relation)
        {
            case spatialRelation.SpatialRelationMapEnvelopeIntersects:
                return SupportsSpatialOperator("BBOX");
            case spatialRelation.SpatialRelationWithin:
                return SupportsSpatialOperator("Contains");
            case spatialRelation.SpatialRelationIntersects:
                return SupportsSpatialOperator("Intersects");
            case spatialRelation.SpatialRelationEnvelopeIntersects:
                return SupportsSpatialOperator("Intersects");
            case spatialRelation.SpatialRelationContains:
                return SupportsSpatialOperator("Within");
        }
        return false;
         * */
    }
    public bool SupportsComparisonOperator(string op)
    {
        return Supports(ComparisonOperators, op) != String.Empty;
    }
    public bool SupportsSimpleArithmeticOperator(string op)
    {
        return Supports(SimpleArithmeticOperators, op) != String.Empty;
    }
    public bool SupportsArithmeticFunction(string op)
    {
        return Supports(ArithmeticFunctions, op) != String.Empty;
    }
    #endregion

    public string SpatialOperator(/*spatialRelation relation*/)
    {
        //return "BBOX";
        return "Intersects";
        /*
        bool bbox = SupportsSpatialOperator("BBOX");
        if (!SupportsSpatialOperator(relation))
        {
            if (SupportsSpatialOperator("BBOX"))
                return "BBOX";
            return String.Empty;
        }

        switch (relation)
        {
            case spatialRelation.SpatialRelationMapEnvelopeIntersects:
                return "BBOX";
            case spatialRelation.SpatialRelationWithin:
                return "Contains";
            case spatialRelation.SpatialRelationIntersects:
                return "Intersects";
            case spatialRelation.SpatialRelationEnvelopeIntersects:
                return "Intersects";
            case spatialRelation.SpatialRelationContains:
                return "Within";
        }

        return String.Empty;
         * */
    }

}
