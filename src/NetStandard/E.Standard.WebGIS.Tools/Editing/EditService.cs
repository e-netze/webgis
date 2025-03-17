using E.Standard.DbConnector;
using E.Standard.Extensions;
using E.Standard.Extensions.Compare;
using E.Standard.Extensions.IO;
using E.Standard.Json;
using E.Standard.Security.Core;
using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Editing;

internal class EditService
{
    #region Legend

    async public Task<ApiEventResponse> ShowSelectByLegend(IBridge bridge, ApiToolEventArguments e)
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId),
            CurrentMapSrsId = e.GetInt(Edit.EditMapCrsId)
        };
        var feature = editEnvironment.GetFeature(bridge, e);
        var editTheme = editEnvironment[e];

        var legendField = e.CommandIndexValue;

        List<UIElement> menuItems = new List<UIElement>();
        foreach (var legendItem in await bridge.GetLayerLegendItems(editEnvironment.EditThemeDefinition.ServiceId, editEnvironment.EditThemeDefinition.LayerId))
        {
            if (legendItem?.Values == null)
            {
                // Kein Wert kommt meinses bei "all other values"
                continue;
            }
            if (legendItem.Values.Length != 1)
            {
                throw new Exception("Legenden für diesen Layer dürfen sich nur auf ein Feld beziehen");
            }

            var menuItem = new UIMenuItem(this, e, UIButton.UIButtonType.servertoolcommand, "select-by-legend[" + legendField + "]")
            {
                text = legendItem.Label,
                value = legendItem.Values[0],
                icon = "data:image/png;base64, " + Convert.ToBase64String(legendItem.Data)
            };
            menuItems.Add(menuItem);
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]{
                new UIMenu()
                    {
                        elements = menuItems.ToArray(),
                        target = UIElementTarget.modaldialog.ToString(),
                        header= menuItems.Count>0 ? "Legende" : "Keine Legende gefunden"
                    }
                }
        };
    }

    public ApiEventResponse SelectByLegend(IBridge bridge, ApiToolEventArguments e)
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e)
        {
            CurrentMapScale = e.GetDouble(Edit.EditMapScaleId),
            CurrentMapSrsId = e.GetInt(Edit.EditMapCrsId)
        };
        var feature = editEnvironment.GetFeature(bridge, e);
        var editTheme = editEnvironment[e];

        var legendField = e.CommandIndexValue;

        return new ApiEventResponse()
        {
            UISetters = new IUISetter[]
            {
                editEnvironment.FieldSetter(legendField, e.MenuItemValue)
            }
        };
    }

    #endregion

    #region AutoComplete

    async public Task<ApiEventResponse> OnAutocomplete(IBridge bridge, ApiToolEventArguments e)
    {
        EditEnvironment editEnvironment = new EditEnvironment(bridge, e);
        var editTheme = editEnvironment[e["themeid"]];
        if (editTheme == null)
        {
            throw new Exception("Unknown edit theme");
        }

        string connectionString = editTheme.GetDbConnectionString(e["field"]);
        if (String.IsNullOrWhiteSpace(connectionString))
        {
            throw new Exception("No connection string for field: " + e["field"]);
        }

        var sqlResult = editTheme.GetDbSqlStatement(e["field"]);
        if (String.IsNullOrWhiteSpace(sqlResult.statement))
        {
            throw new Exception("No sql statement for field: " + e["field"]);
        }

        var values = new List<object>();

        if (connectionString.IsValidHttpUrl())
        {
            #region HttpRequest

            var url = connectionString;
            if (!String.IsNullOrEmpty(sqlResult.statement))
            {
                url += $"{(url.Contains("?") ? "&" : "?")}{sqlResult.statement.Replace("{0}", e["term"])}";
            }
            var jsonResult = (await bridge.HttpService.GetStringAsync(url)).Trim();

            var domains = JSerializer.Deserialize<object[]>(jsonResult);
            foreach (var domain in domains)
            {
                if (domain is JObject)
                {
                    var value = ((JObject)domain).Values().FirstOrDefault()?.ToString();

                    StringBuilder subText = new StringBuilder();
                    foreach (var jValue in ((JObject)domain).Values().Skip(1).Where(v => v is JValue))
                    {
                        subText.Append($"{jValue.Path}: {jValue}\n");
                    }

                    values.Add(new
                    {
                        label = value,
                        value = value,
                        subtext = subText.ToString()
                    });
                }
                else
                {
                    values.Add(domain?.ToString());
                }
            }

            #endregion
        }
        else
        {
            #region Database

            string sql = bridge.ReplaceUserAndSessionDependentFilterKeys(sqlResult.statement, startingBracket: "{{", endingBracket: "}}");

            if (sql.Contains("{0}"))
            {
                sql = String.Format(sql, SqlInjection.ParsePro(e["term"]));
            }

            using (DBFactory dbFactory = new DBFactory(connectionString))
            using (var dbConnection = dbFactory.GetConnection())
            using (var dbCommand = dbFactory.GetCommand())
            {
                await dbConnection.OpenAsync();

                dbCommand.CommandText = sql;
                using (var reader = await dbCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader.FieldCount > 1)
                        {
                            var value = reader[0]?.ToString();

                            StringBuilder subText = new StringBuilder();
                            for (int i = 1; i < reader.FieldCount; i++)
                            {
                                subText.Append($"{reader.GetName(i)}: {reader[i]?.ToString()}\n");
                            }

                            values.Add(new
                            {
                                label = value,
                                value = value,
                                subtext = subText.ToString()
                            });
                        }
                        else
                        {
                            values.Add(reader[0]?.ToString());
                        }
                    }
                }
            }

            #endregion
        }

        return new ApiRawJsonEventResponse(values.ToArray());
    }

    #endregion

    #region Cascading Combos

    async public Task<ApiEventResponse> OnUpdateCombo(IBridge bridge, ApiToolEventArguments e,
                                                      EditEnvironment editEnvironment = null,
                                                      EditEnvironment.EditTheme editTheme = null,
                                                      string editFieldPrefix = "")
    {
        editFieldPrefix = editFieldPrefix.OrTake("editfield");
        if (!String.IsNullOrEmpty(e[$"_{Mobile.UpdateFeature.EditAttributesFieldPrefix}_themeid"]))
        {
            editFieldPrefix = Mobile.UpdateFeature.EditAttributesFieldPrefix;
        }

        editEnvironment = editEnvironment ?? new EditEnvironment(bridge, e, editFieldPrefix: editFieldPrefix);
        editTheme = editTheme ?? editEnvironment[e[$"_{editFieldPrefix}_themeid"]];

        if (editTheme == null)
        {
            throw new Exception("Unknown edit theme");
        }

        var comboId = e["combo-id"];
        var field = comboId.Substring($"{editFieldPrefix}_".Length);

        string connectionString = editTheme.GetDbConnectionString(field);
        if (String.IsNullOrWhiteSpace(connectionString))
        {
            throw new Exception("No connection string for field: " + field);
        }

        var sqlResult = editTheme.GetDbSqlStatement(field);
        if (String.IsNullOrWhiteSpace(sqlResult.statement))
        {
            throw new Exception("No sql statement for field: " + e["field"]);
        }

        var sql = bridge.ReplaceUserAndSessionDependentFilterKeys(sqlResult.statement, startingBracket: "{{", endingBracket: "}}");
        var sqlKeyParameters = E.Standard.WebGIS.CMS.Globals.KeyParameters(sql, startingBracket: "{{", endingBracket: "}}");

        Dictionary<string, string> options = new Dictionary<string, string>();

        if (connectionString.IsValidHttpUrl())
        {
            #region Web Service (DataLinq)

            foreach (var sqlKeyParameter in sqlKeyParameters)
            {
                sql = sql.Replace("{{" + sqlKeyParameter + "}}", e[$"{editFieldPrefix}_{sqlKeyParameter}"]?.ToString());
            }

            connectionString += $"{(connectionString.Contains("?") ? "&" : "?")}{sql}";

            var jsonResult = await bridge.HttpService.GetStringAsync(connectionString);
            var domains = JSerializer.Deserialize<object[]>(jsonResult);
            string valueProperty = sqlResult.valueFieldName.OrTake("value");
            string labelProperty = sqlResult.aliasFieldName.OrTake("name");

            foreach (var domain in domains)
            {
                if (JSerializer.IsJsonElement(domain))
                {
                    options.Add(
                        JSerializer.GetJsonElementValue(domain, valueProperty).ToStringOrEmpty(),
                        JSerializer.GetJsonElementValue(domain, labelProperty).ToStringOrEmpty());
                }
            }

            #endregion
        }
        else
        {
            #region Database

            using (var dbFactory = new DBFactory(connectionString))
            using (var connection = dbFactory.GetConnection())
            using (var command = dbFactory.GetCommand(connection))
            {

                int index = 0;
                foreach (var sqlKeyParameter in sqlKeyParameters)
                {
                    var parameterName = dbFactory.ParaName($"p{index++}");
                    command.Parameters.Add(dbFactory.GetParameter(parameterName, e[$"{editFieldPrefix}_{sqlKeyParameter}"]?.ToString()));

                    sql = sql.Replace("{{" + sqlKeyParameter + "}}", parameterName);
                }

                command.CommandText = sql;
                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string value = reader[sqlResult.valueFieldName]?.ToString() ?? String.Empty,
                               label = reader[sqlResult.aliasFieldName]?.ToString() ?? String.Empty;

                        options.Add(value, label);
                    }
                }
            }

            #endregion
        }

        return new ApiEventResponse()
        {
            UISetters = new IUISetter[]{
                new UISelectOptionsSetter(comboId, e[comboId], options.Keys.Select(k => new UISelect.Option()
                {
                    value = k,
                    label = options[k]
                }))
            }
        };
    }

    #endregion
}
