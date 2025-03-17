using E.Standard.ThreadSafe;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.Geojuhu;

public class GeuJuhuService
{
    public IMap _map;
    public List<ILayer> _layers;

    private GeuJuhuService() { }

    public GeuJuhuService(IMap map, List<ILayer> layers)
    {
        _map = map;
        _layers = layers;
    }

    async private Task<bool> Find(IRequestContext requestContext,
                                  string searchString,
                                  FeatureCollection features,
                                  Dictionary<QueryDefinitions.QueryDefinition, ILayer> qDefs,
                                  SpatialReference sRef = null)
    {
        //int threadCount = 0;
        ThreadSaveInc inc = new ThreadSaveInc();

        foreach (QueryDefinitions.QueryDefinition qDef in qDefs.Keys)
        {
            ILayer layer = qDefs[qDef];

            ThreadArgument arg = new ThreadArgument(qDef, layer, searchString, features, inc, sRef);
            //Thread thread = new Thread(new ParameterizedThreadStart(PerformQueryAsync));
            //thread.Start(arg);
            //threadCount++;
            await PerformQueryAsync(arg, requestContext);
        }

        //while (inc.Position < threadCount)
        //{
        //    Console.WriteLine("GeoJuhuService.Find2 sleep 100");
        //    Thread.Sleep(100);
        //}

        return true;
    }

    async public static Task<bool> Perform(IRequestContext requestContext,
                                           string searchString,
                                           FeatureCollection features,
                                           Dictionary<QueryDefinitions.QueryDefinition, ILayer> qDefs,
                                           SpatialReference sRef)
    {
        GeuJuhuService service = new GeuJuhuService();
        return await service.Find(requestContext, searchString, features, qDefs, sRef);
    }

    #region HelperClasses

    private class ThreadArgument
    {
        public QueryDefinitions.QueryDefinition QueryDefinition;
        public ILayer Layer;
        public string SearchString;
        public FeatureCollection Features;
        public ThreadSaveInc Inc;
        public SpatialReference SRef;

        public ThreadArgument(QueryDefinitions.QueryDefinition qDef, ILayer layer, string searchString, FeatureCollection features, ThreadSaveInc inc, SpatialReference sRef = null)
        {
            QueryDefinition = qDef;
            Layer = layer;
            SearchString = searchString;
            Features = features;
            Inc = inc;
            SRef = sRef;
        }
    }

    #endregion

    async private Task PerformQueryAsync(object o, IRequestContext requestContext)
    {
        try
        {
            if (!(o is ThreadArgument))
            {
                return;
            }

            QueryDefinitions.QueryDefinition qDef = ((ThreadArgument)o).QueryDefinition;
            ILayer layer = ((ThreadArgument)o).Layer;
            string searchString = ((ThreadArgument)o).SearchString;

            int sRefId = 0;
            SpatialReference sRef = null;
            if (((ThreadArgument)o).SRef != null)
            {
                sRef = ((ThreadArgument)o).SRef;
                sRefId = sRef.Id;

                if (layer.Service is IServiceSupportedCrs)
                {
                    IServiceSupportedCrs serviceCrs = (IServiceSupportedCrs)layer.Service;
                    if (serviceCrs.SupportedCrs != null && serviceCrs.SupportedCrs.Length > 0)
                    {
                        bool supportsCrs = false;
                        for (int i = 0; i < serviceCrs.SupportedCrs.Length; i++)
                        {
                            if (serviceCrs.SupportedCrs[i] == sRefId)
                            {
                                supportsCrs = true;
                                break;
                            }
                        }
                        if (!supportsCrs)
                        {
                            sRefId = serviceCrs.SupportedCrs[0];
                            sRef = CoreApiGlobals.SRefStore.SpatialReferences.ById(sRefId);
                        }
                    }
                }
            }

            FeatureCollection fCollection = new FeatureCollection();
            for (int level = 0; level < 3; level++)
            {
                string[] args = null;
                QueryBuilder where = null;

                int start = 1, loops = 0;

                #region Where Clause erstellen

                switch (level)
                {
                    case 0:
                        args = new string[1] { searchString };
                        where = new QueryBuilder("OR");
                        break;
                    case 1:
                        where = new QueryBuilder("AND");
                        args = searchString.Split(' ');
                        break;
                    case 2:
                        where = new QueryBuilder("OR");
                        args = searchString.Split(' ');
                        break;
                }

                UniqueList subfields = new UniqueList();
                subfields.Add(layer.IdFieldName);
                subfields.Add(layer.ShapeFieldName);
                foreach (QueryDefinitions.QueryDefinition.SearchItem sItem in qDef.SearchItems)
                {
                    foreach (string fieldName in sItem.Fields.Split(';'))
                    {
                        if (layer.Fields.FindField(fieldName) != null)
                        {
                            subfields.Add(fieldName);
                        }
                    }

                    Regex regex = (!String.IsNullOrEmpty(sItem.RegEx) ? new Regex(sItem.RegEx) : null);

                    where.StartBracket("OR");
                    foreach (string arg in args)
                    {
                        // hier regex einbauen
                        if (regex != null && !arg.Contains("%"))
                        {
                            if (!regex.IsMatch(arg))
                            {
                                continue;
                            }
                        }
                        where.AppendClause(sItem.Fields.Split(';'), sItem.UseUpper, /*QueryMethod.BothWildcards*/ sItem.QueryMethod, layer, arg);
                    }
                    where.EndBracket();
                }
                if (where.ToString().Trim().Length == 0)
                {
                    break;
                }

                List<QueryBuilder> queryBilders = new List<QueryBuilder>() { where };
                if (level == 2)
                {
                    //where = where.CalcCombinations("OR");
                    queryBilders = where.CalcCombinations2("OR");
                }

                #endregion

                foreach (QueryBuilder queryBuilder in queryBilders)
                {
                    while (true)
                    {
                        QueryFilter filter = new QueryFilter(layer.IdFieldName, queryBuilder.ToString(), 500, start);
                        filter.SubFields = subfields.ToString(" ");

                        if (sRef != null)
                        {
                            filter.FeatureSpatialReference = sRef;
                        }

                        await layer.GetFeaturesAsync(filter, fCollection, requestContext);
                        if (fCollection.Count == 0)
                        {
                            break;
                        }

                        qDef.CalcFullTextField(fCollection);
                        foreach (Feature f in fCollection)
                        {
                            f.Attributes.Add(new Core.Attribute("_level", level.ToString()));
                            f.Attributes.Add(new Core.Attribute("_srs", sRefId.ToString()));
                            f.GlobalOid = qDef.Id + ":" + f.Oid.ToString();
                        }
                        ((ThreadArgument)o).Features.AddRange(fCollection);

                        if (!fCollection.HasMore)
                        {
                            break;
                        }

                        start += fCollection.Count;
                        loops++;
                        if (loops > 10)
                        {
                            break;
                        }

                        break; // Schleife einmal immer beenden, kein automatisches mehr (sollte durch die 3 Levels schon ideal gesucht werden und so kaum ein Hasmore vorkommen)
                    }
                    if (fCollection.Count > 0)
                    {
                        break;
                    }
                }
                if (fCollection.Count > 0)
                {
                    break;
                }
            }
        }
        catch { }
        finally
        {
            if (o is ThreadArgument)
            {
                ((ThreadArgument)o).Inc.Inc();
            }
        }
    }
}
