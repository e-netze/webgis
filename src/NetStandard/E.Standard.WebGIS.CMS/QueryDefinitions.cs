using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.WebGIS.CMS;

public class QueryDefinitions : List<QueryDefinitions.QueryDefinition>
{
    private int _idSequence = 0;

    public QueryDefinitions(CmsHlp cms)
    {
        if (cms == null)
        {
            return;
        }

        CmsNodeCollection queryLinks = cms.GetLayerQueryLinks();
        if (queryLinks == null)
        {
            return;
        }

        foreach (CmsLink queryLink in queryLinks)
        {
            if (queryLink.Target == null)
            {
                continue;
            }

            CmsNode queryNode = queryLink.Target;
            QueryDefinition qdef = new QueryDefinition(cms, queryNode);
            qdef.GeoJuhu = (bool)queryLink.Load("geojuhu", false);
            qdef.GeoJuhuSchema = queryLink.LoadString("geojuhuschema");
            this.Add(qdef);
        }
    }

    public QueryDefinition[] ByLayerGlobalId(string globalId)
    {
        List<QueryDefinition> ret = new List<QueryDefinition>();

        foreach (QueryDefinition qdef in this)
        {
            if (qdef.LayerGlobalId == globalId)
            {
                ret.Add(qdef);
            }
        }

        return ret.ToArray();
    }

    public QueryDefinition ById(string id)
    {
        foreach (QueryDefinition qdef in this)
        {
            if (qdef.Id == id)
            {
                return qdef;
            }
        }

        return null;
    }

    new public void Add(QueryDefinition qdef)
    {
        if (String.IsNullOrEmpty(qdef.LayerGlobalId))  // Abfrage hat keine Abfragethema!!
        {
            return;
        }

        qdef.Id = (_idSequence++).ToString();
        base.Add(qdef);
    }

    public void RemoveByServiceId(string serviceId)
    {
        serviceId = serviceId.ToLower();
        foreach (QueryDefinition qDef in ListOps<QueryDefinitions.QueryDefinition>.Clone(this))
        {
            if (qDef.LayerGlobalId.ToLower().StartsWith(serviceId + ":"))
            {
                this.Remove(qDef);
            }
        }
    }

    #region Classes
    public class QueryDefinition
    {
        public string Name, XPath, CmsName, ServiceName;
        public string Id;
        public SearchItem[] SearchItems;
        public string LayerGlobalId;
        public bool GeoJuhu = false, Gdi = false;
        public string GeoJuhuSchema = String.Empty;
        public string[] MapLinks = null;

        public QueryDefinition(CmsHlp cms, CmsNode queryNode)
        {
            Name = queryNode.Name;
            XPath = queryNode.NodeXPath;
            CmsName = queryNode.Cms.CmsName;
            ServiceName = queryNode.ParentNode.ParentNode.AliasName;

            CmsNodeCollection items = cms.SelectNodes(queryNode.NodeXPath + "/searchitems/*");
            if (items != null)
            {
                SearchItems = new SearchItem[items.Count];
                for (int i = 0; i < SearchItems.Length; i++)
                {
                    SearchItems[i] = new SearchItem(items[i].Name, items[i].LoadString("fields"));
                    SearchItems[i].UseUpper = (bool)items[i].Load("useupper", false);
                    SearchItems[i].RegEx = items[i].LoadString("regex");
                    SearchItems[i].Visible = (bool)items[i].Load("visible", true);
                    SearchItems[i].QueryMethod = (QueryMethod)items[i].Load("method", QueryMethod.Exact);
                }
            }
            LayerGlobalId = cms.GetQueryLayerGlobalId(queryNode);
        }

        public string WhereClause(ILayer layer, string searchText, UniqueList subfields)
        {
            StringBuilder where = new StringBuilder();

            string[] searchStrings = searchText.Split(' ');
            if (searchStrings.Length > 1)
            {
                Array.Resize<string>(ref searchStrings, searchStrings.Length + 1);
                searchStrings[searchStrings.Length - 1] = searchText.Replace(" ", "%");
            }

            foreach (string searchString in searchStrings)
            {
                foreach (SearchItem item in SearchItems)
                {
                    foreach (string fieldName in item.Fields.Split(';'))
                    {
                        IField f = layer.Fields.FindField(fieldName);
                        if (f == null)
                        {
                            continue;
                        }

                        subfields.Add(f.Name);

                        string w = String.Empty;
                        switch (f.Type)
                        {
                            case FieldType.Interger:
                            case FieldType.SmallInteger:
                            case FieldType.BigInteger:
                            case FieldType.Float:
                            case FieldType.Double:
                            case FieldType.ID:
                                if (IsNumeric(searchString))
                                {
                                    w = f.Name + "=" + searchString;
                                }

                                break;
                            default:
                                w = f.Name + " like '%" + searchString + "%'";
                                break;
                        }

                        if (!String.IsNullOrEmpty(w))
                        {
                            if (where.Length > 0)
                            {
                                where.Append(" OR ");
                            }

                            where.Append(w);
                        }
                    }
                }
            }

            return where.ToString();
        }

        public void CalcFullTextField(FeatureCollection features)
        {
            foreach (Feature feature in features)
            {
                StringBuilder sb = new StringBuilder();
                foreach (SearchItem item in SearchItems)
                {
                    foreach (string fieldName in item.Fields.Split(';'))
                    {
                        string val = feature[fieldName];
                        if (String.IsNullOrEmpty(val))
                        {
                            continue;
                        }

                        if (sb.Length > 0)
                        {
                            sb.Append(" ");
                        }

                        sb.Append(val);
                    }
                }
                feature.Attributes.Add(new WebMapping.Core.Attribute("_fulltext", sb.ToString()));
                feature.Attributes.Add(new WebMapping.Core.Attribute("_qdefid", this.Id));
                feature.Attributes.Add(new WebMapping.Core.Attribute("_fguid", Guid.NewGuid().ToString("N").ToLower()));
            }
        }

        private bool IsNumeric(string str)
        {
            double d;
            return double.TryParse(str, out d);
        }

        public bool CheckGeoJuhuSchema(string schema)
        {
            if (String.IsNullOrEmpty(schema))
            {
                return true;
            }

            schema = schema.ToLower().Trim();
            foreach (string geoJuhuSchema in GeoJuhuSchema.Split(','))
            {
                if (geoJuhuSchema.Trim() == "*")
                {
                    return true;
                }

                if (geoJuhuSchema.ToLower().Trim() == schema)
                {
                    return true;
                }
            }

            return false;
        }

        #region Classes
        public class SearchItem
        {
            public string Name, Fields, RegEx = String.Empty;
            public bool UseUpper = false, Visible = true;
            public QueryMethod QueryMethod = QueryMethod.Exact;

            public SearchItem(string name, string fields)
            {
                Name = name;
                Fields = fields.Replace("(UPPER)", "");
            }
        }
        #endregion
    }
    #endregion
}
