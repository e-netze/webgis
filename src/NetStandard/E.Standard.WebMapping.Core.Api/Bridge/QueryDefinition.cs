using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Api.Bridge;

public class QueryDefinition
{
    public enum VisibilityFlag
    {
        Any = 0,
        Visible = 1,
        Invisible = 2
    }

    public string ServiceId { get; set; }
    public string QueryId { get; set; }
    public bool LayerVisible { get; set; }

    static public QueryDefinition[] FromString(string defs, VisibilityFlag visibilityFlag = VisibilityFlag.Any)
    {
        List<QueryDefinition> ret = new List<QueryDefinition>();

        foreach (string def in defs.Split(';'))
        {
            string[] p = def.Split(',');
            if (p.Length == 3)
            {
                var queryDef = new QueryDefinition()
                {
                    ServiceId = p[0],
                    QueryId = p[1],
                    LayerVisible = p[2] == "1"
                };

                if (visibilityFlag == VisibilityFlag.Visible && !queryDef.LayerVisible)
                {
                    continue;
                }

                if (visibilityFlag == VisibilityFlag.Invisible && queryDef.LayerVisible)
                {
                    continue;
                }

                ret.Add(queryDef);
            }
        }

        return ret.ToArray();
    }

    async static public Task<IQueryBridge[]> QueriesFromString(IBridge bridge, string defs, VisibilityFlag visibilityFlag = VisibilityFlag.Any)
    {
        var queryDefs = FromString(defs, visibilityFlag);

        List<IQueryBridge> ret = new List<IQueryBridge>();
        foreach (var queryDef in queryDefs)
        {
            IQueryBridge query = await bridge.GetQuery(queryDef.ServiceId, queryDef.QueryId);
            if (query != null)
            {
                ret.Add(query);
            }
        }
        return ret.ToArray();
    }
}
