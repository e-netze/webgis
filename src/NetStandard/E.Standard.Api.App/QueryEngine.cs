using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Exceptions;
using E.Standard.CMS.Core;
using E.Standard.Extensions.Compare;
using E.Standard.Platform;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Api.App;

public class QueryEngine
{
    public enum AdvancedQueryMethod
    {
        Normal,
        HasFeatures,
        FirstFeature,
        FirstFeatureGeometry,
        FirstLegendValue
    }

    public int FeatureCount
    {
        get;
        private set;
    }

    public bool HasMore { get; private set; }

    public FeatureCollection Features
    {
        get;
        private set;
    }

    public FeatureCollection FeaturesOrEmtpyFeatureCollection => this.Features != null ? this.Features : new FeatureCollection();

    public bool HasWarningsOrInformation => this.Features.Warnings != null || this.Features.Informations != null;

    async public Task<bool> PerformAsync(IRequestContext requestContext,
                                         QueryDTO query,
                                         NameValueCollection queryItems,
                                         bool queryGeometry = false,
                                         SpatialReference featureSref = null,
                                         Shape queryShape = null,
                                         SpatialReference filterSref = null,
                                         AdvancedQueryMethod advancedQueryMethod = AdvancedQueryMethod.Normal,
                                         string visFilterClause = "",
                                         QueryFields queryFields = QueryFields.TableFields,
                                         int limit = 0,
                                         string orderBy = "",
                                         double mapScale = 0D,
                                         bool suppressResolveAttributeDomains = false)
    {
        FeatureCollection features = new FeatureCollection();

        try
        {
            if (query == null)
            {
                throw new Exception("unknown query");
            }

            if (query.Service == null)
            {
                throw new Exception("unknown service");
            }

            var layer = query.Service.Layers.FindByLayerId(query.LayerId);

            if (layer == null)
            {
                throw new Exception($"unknown layer: {query.LayerId}");
            }

            if (mapScale >= 1D && query.ApplyZoomLimits && !layer.InScale(mapScale))
            {
                throw new QueryEngineInformationException($"{query.Name} kann nur innerhalb der Maßstabsgrenzen 1:{Math.Max(0, layer.MinScale)} - 1:{Math.Max(0, layer.MaxScale)} abgefragt werden.");
            }

            #region Build Query String

            StringBuilder where = new StringBuilder();
            StringBuilder subfields = new StringBuilder();
            long[] orderByIds = null;

            #region Where Clause
            //CmsNode queryNode = cms.SelectSingleNode(ui, Globals.GdiXPath((string)mapSession.Map.UserValue(webgisConst.QueryTheme, String.Empty)));

            if (!String.IsNullOrEmpty(queryItems?["#oid#"]))
            {
                where.Append(layer.IdFieldName + "=" + queryItems["#oid#"]);
            }
            else if (!String.IsNullOrEmpty(queryItems?["#oids#"]))
            {
                where.Append(layer.IdFieldName + " in (" + queryItems["#oids#"] + ")");
                try
                {
                    orderByIds = queryItems["#oids#"].Split(',').Select(o => long.Parse(o)).ToArray();
                }
                catch { orderByIds = null; }
            }
            else if (!String.IsNullOrEmpty(queryItems?["~oid~"]))
            {
                where.Append(layer.IdFieldName + "=" + queryItems["~oid~"]);
            }
            else if (!String.IsNullOrEmpty(queryItems?["~oids~"]))
            {
                where.Append(layer.IdFieldName + " in (" + queryItems["~oids~"] + ")");
                try
                {
                    orderByIds = queryItems["~oids~"].Split(',').Select(o => long.Parse(o)).ToArray();
                }
                catch { orderByIds = null; }
            }
            else if (query.items != null)
            {
                foreach (QueryDTO.ItemDTO item in query.items)
                //foreach (CmsNode itemNode in cms.SelectNodes(ui, Globals.GdiXPath((string)mapSession.Map.UserValue(webgisConst.QueryTheme, String.Empty) + "/searchitems/*")))
                {
                    string queryValue = queryItems?[item.id];
                    if (String.IsNullOrEmpty(queryValue))
                    {
                        continue;
                    }

                    queryValue = queryValue.Trim();

                    if (where.Length > 0)
                    {
                        where.Append(" AND ");
                    }

                    where.Append("(");
                    bool first = true;

                    queryValue = queryValue.Replace("&lt;", "<").Replace("&gt;", ">");  // Das kann bei Kunden Wasserdienst vorkommen. Die haben spitze klammern in den Namen und die werden mit enc() übersetzt...

                    QueryMethod method = item.QueryMethod;
                    string[] vals = (int)method < 1000 ? queryValue.Split(';') : new string[] { queryValue };
                    if ((int)method >= 1000)
                    {
                        method = (QueryMethod)((int)method - 1000);
                    }

                    foreach (string val_ in vals)
                    {
                        string val = val_.Trim();
                        foreach (string fieldName in item.Fields)
                        {
                            string fieldname = fieldName.Trim();
                            if (String.IsNullOrEmpty(fieldname))
                            {
                                continue;
                            }

                            if (fieldname.StartsWith("("))
                            {
                                int pos = fieldname.IndexOf(")");
                                fieldname = fieldname.Substring(pos + 1, fieldname.Length - pos - 1);
                            }

                            IField field = layer.Fields.FindField(fieldname);
                            if (field == null)
                            {
                                continue;
                            }

                            if (item.Fields.Length > 1)
                            {
                                switch (field.Type)
                                {
                                    case FieldType.BigInteger:
                                    case FieldType.Interger:
                                    case FieldType.SmallInteger:
                                    case FieldType.Float:
                                    case FieldType.Double:
                                        double dummy;
                                        if (!val.TryToPlatformDouble(out dummy))
                                        {
                                            continue;
                                        }

                                        break;
                                }
                            }

                            string quote = String.Empty;
                            if (field.Type == FieldType.String || field.Type == FieldType.Date || field.Type == FieldType.Char)
                            {
                                quote = "'";
                            }

                            if (!first)
                            {
                                where.Append(" OR ");
                            }

                            first = false;

                            if (item.UseUpper && field.Type == FieldType.String)
                            {
                                val = val.ToUpper();
                                where.Append("UPPER(" + fieldname + ")");
                            }
                            else
                            {
                                where.Append(fieldname);
                            }

                            if (val.EndsWith("#"))
                            {
                                method = QueryMethod.Exact;
                                val = val.Substring(0, val.Length - 1);
                            }
                            if (!String.IsNullOrEmpty(item.Regex))
                            {
                                System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(item.Regex);
                                if (!r.IsMatch(val))
                                {
                                    throw new Exception("'" + val.Replace("'", "") + " ist keine gültige Eingabe für " + item.name + "'");
                                }
                            }
                            if (!String.IsNullOrEmpty(item.FormatExpression))
                            {
                                val = String.Format(item.FormatExpression, val);
                                quote = "";
                            }
                            if (String.IsNullOrEmpty(quote) &&
                                (method == QueryMethod.BeginningWildcard ||
                                 method == QueryMethod.BothWildcards ||
                                 method == QueryMethod.EndingWildcard ||
                                 method == QueryMethod.SpacesToWildcard ||
                                 method == QueryMethod.SpacesToWildcardWithBeginningAndEndingWildcard ||
                                 method == QueryMethod.SpacesToWildcardWithBeginningWildcard ||
                                 method == QueryMethod.SpacesToWildcardWithEndingWildcard
                                 ))
                            {
                                method = QueryMethod.Exact;
                            }

                            if (field.Type == FieldType.GUID)  // Kommt bei OÖ vor...
                            {
                                val = val.Trim();
                                try
                                {
                                    Guid guidVal = new Guid(val);
                                    val = guidVal.ToString();
                                }
                                catch
                                {
                                    try
                                    {
                                        Guid guidVal = new Guid(val.Replace("-", ""));
                                        val = guidVal.ToString();
                                    }
                                    catch { }
                                }
                                if (!val.StartsWith("{"))
                                {
                                    val = "{" + val;
                                }

                                if (!val.EndsWith("}"))
                                {
                                    val = val + "}";
                                }

                                method = QueryMethod.Exact;
                                quote = "'";
                            }
                            switch (method)
                            {
                                case QueryMethod.Exact:
                                    where.Append("=" + quote + val + quote);
                                    break;
                                case QueryMethod.BeginningWildcard:
                                    where.Append(" like '%" + val.Replace("*", "%") + "'");
                                    break;
                                case QueryMethod.BothWildcards:
                                    where.Append(" like '%" + val.Replace("*", "%") + "%'");
                                    break;
                                case QueryMethod.EndingWildcard:
                                    where.Append(" like '" + val.Replace("*", "%") + "%'");
                                    break;
                                case QueryMethod.ExactOrWildcards:
                                    if (val.Contains("*") || val.Contains("%"))
                                    {
                                        where.Append(" like '" + val.Replace("*", "%") + "'");
                                    }
                                    else
                                    {
                                        where.Append("=" + quote + val + quote);
                                    }

                                    break;
                                case QueryMethod.LowerThan:
                                    where.Append("<" + quote + val + quote);
                                    break;
                                case QueryMethod.LowerOrEqualThan:
                                    where.Append("<=" + quote + val + quote);
                                    break;
                                case QueryMethod.GreaterThan:
                                    where.Append(">" + quote + val + quote);
                                    break;
                                case QueryMethod.GreaterOrEqualThan:
                                    where.Append(">=" + quote + val + quote);
                                    break;
                                case QueryMethod.Not:
                                    where.Append("<>" + quote + val + quote);
                                    break;
                                case QueryMethod.In:
                                    where.Append(" in(");
                                    where.Append(String.Join(",", val.Split(',').Select(v => quote + v.Trim() + quote)));
                                    where.Append(")");
                                    break;
                                case QueryMethod.NotIn:
                                    where.Append(" not in(");
                                    where.Append(String.Join(",", val.Split(',').Select(v => quote + v.Trim() + quote)));
                                    where.Append(")");
                                    break;
                                case QueryMethod.SpacesToWildcard:
                                    where.Append(" like '" + val.Replace("*", "%").Replace(" ", "%") + "'");
                                    break;
                                case QueryMethod.SpacesToWildcardWithEndingWildcard:
                                    where.Append(" like '" + val.Replace("*", "%").Replace(" ", "%") + "%'");
                                    break;
                                case QueryMethod.SpacesToWildcardWithBeginningWildcard:
                                    where.Append(" like '%" + val.Replace("*", "%").Replace(" ", "%") + "'");
                                    break;
                                case QueryMethod.SpacesToWildcardWithBeginningAndEndingWildcard:
                                    where.Append(" like '%" + val.Replace("*", "%").Replace(" ", "%") + "%'");
                                    break;
                            }
                        }
                    }
                    where.Append(")");
                }
            }

            if (!String.IsNullOrWhiteSpace(visFilterClause))
            {
                //if (where.Length > 0) where.Append(" AND ");
                //where.Append("(");
                //where.Append(visFilterClause);
                //where.Append(")");

                where = new StringBuilder(where.ToString().AppendWhereClause(visFilterClause));
            }

            #endregion

            if (queryShape == null && query.AllowEmptyQueries == false && where.Length == 0)
            {
                throw new Exception("Bitte geben Sie einen Suchbegriff ein!");
            }

            #region SubFields
            /*
            UniqueList uList = mapSession.Map.Environment.UserValue(webgisConst.ActiveQueryTabFields, null) as UniqueList;
            List<DataColumn> columns = mapSession.Map.Environment.UserValue(webgisConst.ActiveQueryTabColumns, null) as List<DataColumn>;
            if (uList == null || columns == null)
                return null;

            subfields.Append(uList.ToString(" "));
             * */
            UniqueList fields = new UniqueList();
            fields.Add(layer.IdFieldName);
            if (queryGeometry)
            {
                fields.Add(layer.ShapeFieldName);
            }

            if (queryFields == QueryFields.TableFields && query.Fields != null)
            {
                #region TableFields from CMS

                foreach (TableFieldDTO tableField in query.Fields)
                {
                    var featureFieldNames = tableField.FeatureFieldNames;

                    if (featureFieldNames != null)
                    {
                        foreach (var featureFieldName in featureFieldNames)
                        {
                            if (layer.Fields != null &&
                                layer.Fields.Count > 0 &&
                                layer.Fields.FindField(featureFieldName, StringComparison.InvariantCultureIgnoreCase) != null)
                            {
                                fields.Add(featureFieldName);
                            }
                            else
                            {
                                // ignore
                                if (!(layer is IRasterlayer || layer.Fields.OrEmpty().Count() == 0 || IsSpecialPlaceholder(featureFieldName)))
                                {
                                    features.AddWarning($"field ignored (not found in layer): {featureFieldName}");
                                }
                                if (featureFieldName == "*" && layer.Fields != null)
                                {
                                    foreach (var field in layer.Fields.Where(f => f.Type != FieldType.Shape))  // Shapefeld nicht hinzufügen: wird oben schon hinzugefügt, wenn queryGeometry = true. 
                                    {                                                                          // Ansonsten, würde die Geometrie auch Abgefragt, wenn queryGeometry = false ist, was nicht sein sollte!!
                                        fields.Add(field.Name);
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion
            }
            else if (queryFields == QueryFields.All)
            {
                #region All Fields

                foreach (var field in layer.Fields.Where(f => f.Type != FieldType.Shape))  // Shapefeld nicht hinzufügen: wird oben schon hinzugefügt, wenn queryGeometry = true. 
                {                                                                          // Ansonsten, würde die Geometrie auch Abgefragt, wenn queryGeometry = false ist, was nicht sein sollte!!
                    fields.Add(field.Name);
                }

                #endregion
            }
            subfields.Append(fields.ToString(" "));

            #endregion

            #region Sorting
            /*
            UniqueList sortingList = new UniqueList();
            foreach (DataColumn column in columns)
            {
                if (column is FieldTabColumn && ((FieldTabColumn)column).InitialSorting == true)
                    sortingList.Add(column.ColumnName);
            }
             * */
            #endregion

            #endregion

            #region Query

            QueryFilter filter = null;

            if (queryShape != null)
            {
                filter = new SpatialFilter(layer.IdFieldName, queryShape, 1000, 1);
                ((SpatialFilter)filter).FilterSpatialReference = filterSref;
            }
            else
            {
                filter = new QueryFilter(layer.IdFieldName, 1000, 1);
            }

            filter.Where = where.ToString();
            filter.SubFields = subfields.ToString();
            //Filter.SetFeatureFilterCoordsys(mapSession, filter, layer);
            //if (collectionLayer)
            //    Filter.ReduceSubFields(filter, layer, true);
            filter.FeatureSpatialReference = featureSref;
            filter.QueryGeometry = queryGeometry;
            filter.SuppressResolveAttributeDomains = suppressResolveAttributeDomains;

            #region Limit Records

            if (limit != 0)
            {
                filter.FeatureLimit = limit;
            }

            #endregion

            #region Sorting

            if (!String.IsNullOrEmpty(orderBy))
            {
                filter.OrderBy = orderBy;
            }

            #endregion

            if (advancedQueryMethod == AdvancedQueryMethod.HasFeatures && layer is ILayer2)
            {
                int featureCount = await ((ILayer2)layer).HasFeaturesAsync(filter, requestContext);
                if (featureCount > 0)
                {
                    features.Add(new WebMapping.Core.Feature());
                    this.FeatureCount = featureCount;
                }
            }
            else if (advancedQueryMethod == AdvancedQueryMethod.FirstFeature && layer is ILayer2)
            {
                WebMapping.Core.Feature feature = await ((ILayer2)layer).FirstFeatureAsync(filter, requestContext);
                if (feature != null)
                {
                    features.Add(feature);
                    this.FeatureCount = 1;
                }
            }
            else if (advancedQueryMethod == AdvancedQueryMethod.FirstFeatureGeometry && layer is ILayer2)
            {
                Shape featureShape = await ((ILayer2)layer).FirstFeatureGeometryAsync(filter, requestContext);
                if (featureShape != null)
                {
                    features.Add(new WebMapping.Core.Feature()
                    {
                        Shape = featureShape
                    });
                    this.FeatureCount = 1;
                }
            }
            else if (advancedQueryMethod == AdvancedQueryMethod.FirstLegendValue)
            {
                string firstLegendValue = layer is ILegendRendererHelper ? await ((ILegendRendererHelper)layer).FirstLegendValueAsync(filter, requestContext) : String.Empty;
                var feature = new WebMapping.Core.Feature();
                feature.Attributes.Add(new WebMapping.Core.Attribute("FirstLegendValue", firstLegendValue));
                features.Add(feature);
                this.Features = features;
                this.FeatureCount = 1;

                return true;
            }
            else
            {
                if (limit > 1 && !String.IsNullOrEmpty(layer.IdFieldName))
                {
                    if (layer is ILayer3)
                    {
                        if (query.Union == true)
                        {
                            var countFeatures = await ((ILayer3)layer).FeaturesCountOnly(filter, requestContext);
                            if (countFeatures > limit && query.Union == true)
                            {
                                throw new QueryEngineInformationException($"{query.Name} kann nicht abgefragt werden: zu viele Ergebnisse ({countFeatures})");
                            }
                        }
                    }

                    string filterWhereClause = filter.Where;

                    filter.OrderBy = layer.IdFieldName;

                    while (features.Count < limit)
                    {
                        var queryFeatures = new FeatureCollection();

                        await layer.GetFeaturesAsync(filter, queryFeatures, requestContext);
                        features.AddRange(queryFeatures.Take(limit - features.Count));

                        this.HasMore = queryFeatures.HasMore;

                        if (queryFeatures.HasMore == false || queryFeatures.Count == 0)
                        {
                            break;
                        }

                        filter.Where = $"{(String.IsNullOrEmpty(filterWhereClause) ? "" : $"({filterWhereClause}) AND ")}{layer.IdFieldName}>{queryFeatures.Select(f => f.Oid).Max()}";
                    }
                }
                else
                {
                    await layer.GetFeaturesAsync(filter, features, requestContext);

                }

                if (features.Count == 0 && !String.IsNullOrWhiteSpace(features.ResultText))
                {
                    this.FeatureCount = 1;
                }
                else
                {
                    this.FeatureCount = features.Count;
                }

                if (String.IsNullOrEmpty(orderBy) && orderByIds != null)
                {
                    features.OrderByIds(orderByIds);
                }
            }

            if (features.HasMore && query.Union == true)
            {
                throw new QueryEngineInformationException($"Can't union feature from layer {layer.Name}: to many features (>{features.Count})");
            }

            //QueryEnvironment queryEnvironment = new QueryEnvironment(layer, filter);
            //queryEnvironment.Query();
            //queryEnvironment.SetUserValue(webgisConst.TabColumns, columns);
            //if (sortingList.Count > 0)
            //    queryEnvironment.SetUserValue(webgisConst.TabSorting, sortingList);
            //queryEnvironment.SetUserValue(webgisConst.QueryName, (string)mapSession.Map.UserValue(webgisConst.QueryName, String.Empty));
            //queryEnvironment.SetUserValue("querypath", (string)mapSession.Map.UserValue(webgisConst.QueryTheme, String.Empty));

            #endregion
        }
        catch (QueryEngineInformationException qewe)
        {
            features = new FeatureCollection();
            features.AddInformation(qewe.Message);
        }
        //catch (Exception ex)
        //{
        //    throw ex;
        //}
        this.Features = features;

        if (this.Features != null)
        {
            if (this.Features.Count == 0 && !String.IsNullOrWhiteSpace(this.Features.ResultText))
            {
                WebMapping.Core.Feature feature = new WebMapping.Core.Feature();
                feature.Attributes.Add(new WebMapping.Core.Attribute("_fulltext", this.Features.ResultText));
                this.Features = new FeatureCollection(feature);
            }

            if (limit > 0 && this.Features.Count > limit)
            {
                this.Features.RemoveRange(limit, this.Features.Count - limit);
            }

            #region Warnungen loggen

            try
            {
                if (this.Features.Warnings != null)
                {
                    requestContext.GetRequiredService<IWarningsLogger>()
                        .LogString(
                            CmsDocument.UserIdentification.Anonymous,
                            String.Empty, $"{query.Service.Url}/{query.id}", "PerformQuery", $"Warning: Query - {String.Join(", ", this.Features.Warnings)}");
                }
            }
            catch { }

            #endregion

            return this.Features.Count > 0;
        }

        return false;
    }

    async public Task<bool> PerformAsync(IRequestContext requestContext,
                                         QueryDTO query,
                                         ApiQueryFilter filter,
                                         AdvancedQueryMethod advancedQueryMethod = AdvancedQueryMethod.Normal,
                                         string appendFilterClause = "",
                                         int limit = 0,
                                         double mapScale = 0D)
    {
        string visFilterClause = null;

        if (query.Bridge is Bridge)
        {
            visFilterClause = ((Bridge)query.Bridge).GetFilterDefinitionQuery(query);
        }

        if (!String.IsNullOrWhiteSpace(appendFilterClause))
        {
            if (String.IsNullOrWhiteSpace(visFilterClause))
            {
                visFilterClause = appendFilterClause;
            }
            else
            {
                visFilterClause = $"({visFilterClause}) and ({appendFilterClause})";
            }
        }

        if (filter is ApiSpatialFilter)
        {
            return await PerformAsync(requestContext,
                                      query,
                                      filter.QueryItems,
                                      filter.QueryGeometry,
                                      filter.FeatureSpatialReference,
                                      ((ApiSpatialFilter)filter).QueryShape,
                                      ((ApiSpatialFilter)filter).FilterSpatialReference,
                                      advancedQueryMethod: advancedQueryMethod,
                                      queryFields: filter.Fields,
                                      visFilterClause: visFilterClause,
                                      limit: limit,
                                      mapScale: mapScale,
                                      suppressResolveAttributeDomains: filter.SuppressResolveAttributeDomains);
        }
        else if (filter != null)
        {
            return await PerformAsync(requestContext,
                                      query,
                                      filter.QueryItems,
                                      filter.QueryGeometry,
                                      filter.FeatureSpatialReference,
                                      advancedQueryMethod: advancedQueryMethod,
                                      queryFields: filter.Fields,
                                      visFilterClause: visFilterClause,
                                      limit: limit,
                                      mapScale: mapScale,
                                      suppressResolveAttributeDomains: filter.SuppressResolveAttributeDomains);
        }

        return false;
    }

    #region Helper

    private bool IsSpecialPlaceholder(string fieldName)
    {
        if (!String.IsNullOrEmpty(fieldName))
        {
            // Bei speziellen Platzhaltern (zB [BBOX],[spatial::] in einer Hotlink soll auch keine Ignored Meldung erzeugt werden

            if (fieldName == "BBOX" ||
               fieldName.ToLower().StartsWith("spatial::") ||
               fieldName == "*")  // alle Felder (bei WMS)
            {
                return true;
            }

            // SDE Functions sind nicht bei jedem Datenbanktyp dabei (Sqlline) => ignoriern
            if (fieldName.EndsWith(".STArea()", StringComparison.InvariantCultureIgnoreCase) ||
                fieldName.EndsWith(".STLength()", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    #endregion
}