using E.Standard.DbConnector;
using E.Standard.Extensions;
using E.Standard.Extensions.Compare;
using E.Standard.Extensions.IO;
using E.Standard.Json;
using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.Api.App.Services;

public class LookupService
{
    private readonly IRequestContext _requestContext;

    public LookupService(IRequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    async public Task<IDictionary<TValue, TName>> GetLookupValues<TValue, TName>(ILookupConnection lookupConnection,
                                                                                 string term = "",
                                                                                 ILayer layer = null,
                                                                                 string parameter = "",
                                                                                 Dictionary<string, string> replacements = null)
    {

        var values = new Dictionary<TValue, TName>();

        if (lookupConnection == null)
        {
            return values;
        }

        if (lookupConnection.ConnectionString == "#")
        {
            #region Distinct Layer

            if (layer is ILayerDistinctQuery)
            {
                var distinctValues = await ((ILayerDistinctQuery)layer).QueryDistinctValues(
                    _requestContext, parameter,
                    where: String.IsNullOrEmpty(term) ? null : $"{parameter} like '{term}%'",
                    orderBy: parameter);

                foreach (var distinctValue in distinctValues.OrEmpty())
                {
                    values.Add((TValue)Convert.ChangeType(distinctValue, typeof(TValue)),
                               (TName)Convert.ChangeType(distinctValue, typeof(TName)));
                }
            }

            #endregion
        }
        else if (lookupConnection.ConnectionString.IsValidHttpUrl())
        {
            #region Web Serice Datalinq Query

            var url = lookupConnection.ConnectionString;
            var sql = lookupConnection.SqlClause;

            if (!String.IsNullOrEmpty(sql))
            {
                if (replacements != null)
                {
                    foreach (var key in replacements.Keys)
                    {
                        sql = sql.Replace($"{{{{{key}}}}}", System.Web.HttpUtility.UrlEncode(replacements[key]));
                    }
                }

                url += $"{(url.Contains("?") ? "&" : "?")}{sql}";
            }

            var jsonResult = await _requestContext.Http.GetStringAsync(url);

            foreach (var element in JSerializer.Deserialize<object[]>(jsonResult).OrEmpty())
            {
                string valueProperty = "value",
                       nameProperty = "name";  // DoTo: this is hardcoded...

                if (JSerializer.IsJsonElement(element))
                {
                    string value = JSerializer.GetJsonElementValue(element, valueProperty).ToStringOrEmpty();
                    string name = JSerializer.GetJsonElementValue(element, nameProperty).ToStringOrEmpty();

                    if (!String.IsNullOrEmpty(term))
                    {
                        if (name == null || name.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }
                    }

                    values.Add((TValue)Convert.ChangeType(value, typeof(TValue)),
                               (TName)Convert.ChangeType(name, typeof(TName)));
                }
            }

            #endregion
        }
        else
        {
            #region Db Connection

            string sql = lookupConnection.SqlClause.Replace("{0}", term);
            if (replacements != null)
            {
                foreach (var key in replacements.Keys)
                {
                    sql = sql.Replace($"{{{key}}}", replacements[key]);
                }
            }

            using (DBFactory dbFactory = new DBFactory(lookupConnection.ConnectionString))
            using (var dbConnection = dbFactory.GetConnection())
            {
                await dbConnection.OpenAsync();
                var dbCommand = dbFactory.GetCommand(dbConnection);
                dbCommand.CommandText = sql;

                using (var dbReader = await dbCommand.ExecuteReaderAsync())
                {
                    int? nameIndex = null, valueIndex = null;

                    for (int i = 0, to = dbReader.FieldCount; i < to; i++)
                    {
                        if ("NAME".Equals(dbReader.GetName(i), StringComparison.OrdinalIgnoreCase))
                        {
                            nameIndex = i;
                        }
                        else if ("VALUE".Equals(dbReader.GetName(i), StringComparison.OrdinalIgnoreCase))
                        {
                            valueIndex = i;
                        }
                    }

                    while (await dbReader.ReadAsync())
                    {
                        TName name = default(TName);
                        TValue value = default(TValue);

                        if (nameIndex.HasValue && valueIndex.HasValue)
                        {
                            name = (TName)Convert.ChangeType(dbReader[nameIndex.Value], typeof(TName));
                            value = (TValue)Convert.ChangeType(dbReader[valueIndex.Value], typeof(TValue));
                        }
                        else
                        {
                            value = (TValue)Convert.ChangeType(dbReader[0], typeof(TValue));
                            name = (TName)Convert.ChangeType(value, typeof(TName));
                        }

                        if (!values.ContainsKey(value))
                        {
                            values.Add(value, name);
                        }
                    }
                }
            }

            #endregion
        }

        return values;
    }

    public bool RequiresLayer(ILookupConnection lookupConnection)
        => lookupConnection?.ConnectionString == "#";
}
