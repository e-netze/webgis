using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using E.Standard.DbConnector;
using E.Standard.Extensions;
using E.Standard.Extensions.Compare;
using E.Standard.Extensions.IO;
using E.Standard.Json;
using E.Standard.WebMapping.Core.Abstraction;

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
                if (!String.IsNullOrEmpty(term) && !LookupSettings.IsValidTerm(term))
                {
                    return values;
                }

                var distinctValues = await ((ILayerDistinctQuery)layer).QueryDistinctValues(
                    _requestContext, parameter,
                    where: String.IsNullOrEmpty(term) ? null : $"{parameter} like '{EscapeSqlLiteral(term)}%'",
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

            term ??= String.Empty;

            if (!LookupSettings.IsValidTerm(term))
            {
                return values;
            }

            foreach (var replacementValue in replacements?.Values ?? Enumerable.Empty<string>())
            {
                if (!LookupSettings.IsValidTerm(replacementValue))
                {
                    return values;
                }
            }

            using (DBFactory dbFactory = new DBFactory(lookupConnection.ConnectionString))
            using (var dbConnection = dbFactory.GetConnection())
            {
                var termParameters = new List<DbParameter>();
                string concatOperator = dbFactory.DatabaseType is DBType.sql or DBType.microsoftsql ? " + " : " || ";

                // '{0}' / '{0}%' / '%{0}%' inside string literals are bound as parameters
                string sql = SqlLiteralRegex.Replace(lookupConnection.SqlClause ?? String.Empty, m =>
                {
                    if (!m.Value.Contains("{0}"))
                    {
                        return m.Value;
                    }

                    var parts = m.Value.Substring(1, m.Value.Length - 2).Split(new[] { "{0}" }, StringSplitOptions.None);
                    var expressions = new List<string>();

                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].Length > 0)
                        {
                            expressions.Add($"'{parts[i]}'");
                        }

                        if (i < parts.Length - 1)
                        {
                            var name = $"lookupterm{termParameters.Count}";
                            termParameters.Add(dbFactory.GetParameter(name, term));
                            expressions.Add(dbFactory.ParaName(name));
                        }
                    }

                    return $"({String.Join(concatOperator, expressions)})";
                });

                // remaining {0} outside of string literals: validated and escaped
                sql = sql.Replace("{0}", EscapeSqlLiteral(term));

                if (replacements != null)
                {
                    foreach (var key in replacements.Keys)
                    {
                        sql = sql.Replace($"{{{key}}}", EscapeSqlLiteral(replacements[key]));
                    }
                }

                await dbConnection.OpenAsync();
                var dbCommand = dbFactory.GetCommand(dbConnection);
                dbCommand.CommandText = sql;
                foreach (var termParameter in termParameters)
                {
                    dbCommand.Parameters.Add(termParameter);
                }

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

    private static readonly Regex SqlLiteralRegex = new Regex(@"'(?:[^']|'')*'", RegexOptions.Compiled);

    private static string EscapeSqlLiteral(string value) => (value ?? String.Empty).Replace("'", "''");

    public bool RequiresLayer(ILookupConnection lookupConnection)
        => lookupConnection?.ConnectionString == "#";
}
