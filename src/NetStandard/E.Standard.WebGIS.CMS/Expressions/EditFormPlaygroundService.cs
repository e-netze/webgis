#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Xml;

using E.Standard.DbConnector;

namespace E.Standard.WebGIS.CMS.Expressions;

public sealed record EditFormPlaygroundField(
    string Id,
    string Name,
    string FieldName,
    string Category,
    string FieldType,
    bool Visible,
    bool Readonly,
    bool Required,
    string AutoValue,
    string AutoValueConnection = "",
    string AutoValueSql = "");

public sealed record EditFormPlaygroundTheme(
    string Path,
    string Name,
    string ServiceTheme,
    IReadOnlyList<EditFormPlaygroundField> Fields);

public sealed record EditFormPlaygroundResult(
    string FieldName,
    int Feature,
    string? Value,
    string? Error);

public sealed class EditFormPlaygroundRequest
{
    public string CmsId { get; set; } = String.Empty;
    public string ThemePath { get; set; } = String.Empty;
    public string Operation { get; set; } = String.Empty;
    public string Username { get; set; } = String.Empty;
    public string DatabaseUsername { get; set; } = String.Empty;
    public double MapScale { get; set; }
    public int MapSrefId { get; set; }
    public string GeoJson { get; set; } = String.Empty;
    public string ValuesJson { get; set; } = "{}";
    public string Deployment { get; set; } = String.Empty;
    public string[] Fields { get; set; } = Array.Empty<string>();
}

public static class EditFormPlaygroundService
{
    public const int MaxSelectedFields = 20;

    private static readonly Dictionary<int, string> AutoValueNames = new()
    {
        [101] = "guid",
        [102] = "guid_sql",
        [103] = "guid_v7",
        [104] = "guid_v7_sql",
        [201] = "create_login",
        [202] = "create_login_full",
        [203] = "create_login_short",
        [204] = "create_date",
        [205] = "create_time",
        [206] = "create_datetime_sql",
        [207] = "create_datetime_sql2",
        [208] = "create_login_domain",
        [209] = "create_datetime_utc",
        [301] = "change_login",
        [302] = "change_login_full",
        [303] = "change_login_short",
        [304] = "change_date",
        [305] = "change_time",
        [306] = "change_datetime_sql",
        [307] = "change_datetime_sql2",
        [308] = "change_login_domain",
        [309] = "change_datetime_utc",
        [401] = "scale",
        [402] = "edit_operation",
        [403] = "map_srefid",
        [404] = "edit_service_id",
        [405] = "edit_layer_id",
        [406] = "edit_theme_id",
        [501] = "shape_len",
        [502] = "shape_len_int",
        [503] = "shape_area",
        [504] = "shape_perimeter",
        [505] = "shape_area_int",
        [506] = "shape_minx",
        [507] = "shape_miny",
        [508] = "shape_maxx",
        [509] = "shape_maxy",
        [510] = "shape_centroid_x",
        [511] = "shape_centroid_y",
        [512] = "shape_vertex_count",
        [513] = "shape_part_count",
        [514] = "shape_type",
        [515] = "shape_srefid",
        [601] = "db_select",
        [602] = "db_select_on_insert"
    };

    public static IReadOnlyList<EditFormPlaygroundTheme> GetThemes(XmlDocument cmsDocument)
    {
        ArgumentNullException.ThrowIfNull(cmsDocument);

        return cmsDocument.SelectNodes("//item[@filtertype='edittheme']")
            ?.Cast<XmlNode>()
            .Select(ParseTheme)
            .Where(theme => theme is not null)
            .Cast<EditFormPlaygroundTheme>()
            .ToArray()
            ?? Array.Empty<EditFormPlaygroundTheme>();
    }

    public static IReadOnlyList<EditFormPlaygroundResult> Evaluate(
        XmlDocument cmsDocument,
        EditFormPlaygroundRequest request)
    {
        ArgumentNullException.ThrowIfNull(cmsDocument);
        ArgumentNullException.ThrowIfNull(request);
        if (request.Operation is not ("insert" or "update"))
        {
            throw new ArgumentException("Operation must be insert or update.", nameof(request));
        }
        if (!Double.IsFinite(request.MapScale) || request.MapScale < 0)
        {
            throw new ArgumentException("Map scale must be a non-negative number.", nameof(request));
        }
        if (request.MapSrefId < 0)
        {
            throw new ArgumentException("Map SRefId must be zero or a positive integer.", nameof(request));
        }
        if (request.Fields is null || request.Fields.Length is < 1 or > MaxSelectedFields)
        {
            throw new ArgumentException(
                $"Select between 1 and {MaxSelectedFields} fields.",
                nameof(request));
        }

        var theme = GetThemes(cmsDocument)
            .SingleOrDefault(theme => String.Equals(
                theme.Path,
                request.ThemePath,
                StringComparison.OrdinalIgnoreCase));
        if (theme is null)
        {
            throw new ArgumentException("The selected editing theme was not found.", nameof(request));
        }

        var selectedNames = request.Fields.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (selectedNames.Count != request.Fields.Length)
        {
            throw new ArgumentException("A field was selected more than once.", nameof(request));
        }

        var selectedFields = theme.Fields
            .Where(field => selectedNames.Contains(field.Id))
            .ToArray();
        if (selectedFields.Length != selectedNames.Count)
        {
            throw new ArgumentException("One or more selected fields are not in this editing theme.", nameof(request));
        }

        var featureCount = ExpressionPlaygroundService.Evaluate(
            request.GeoJson,
            "=1",
            CmsExpressionType.AutoValue).Count;
        var parameterValues = ParseValues(request.ValuesJson);
        var properties = GetFeatureProperties(request.GeoJson);
        var results = new List<EditFormPlaygroundResult>();
        foreach (var field in selectedFields)
        {
            EvaluateField(
                field,
                theme,
                request,
                parameterValues,
                featureCount,
                results,
                properties);
        }

        return results;
    }

    private static EditFormPlaygroundTheme? ParseTheme(XmlNode themeNode)
    {
        var path = GetItemPath(themeNode);
        if (String.IsNullOrEmpty(path))
        {
            return null;
        }

        var fieldsNode = FindChildItem(themeNode, "EditingFields");
        var fields = fieldsNode?.SelectNodes(".//item[@type='file']")
            ?.Cast<XmlNode>()
            .Select(ParseField)
            .Where(field => field is not null)
            .Cast<EditFormPlaygroundField>()
            .ToArray()
            ?? Array.Empty<EditFormPlaygroundField>();

        var serviceTheme = FindChildItem(themeNode, "EditingTheme")
            ?.SelectSingleNode(".//item[@type='link']")
            ?.Attributes?["target"]?.Value
            ?? String.Empty;

        return new EditFormPlaygroundTheme(
            path,
            themeNode.Attributes?["displayname"]?.Value
                ?? themeNode.Attributes?["name"]?.Value
                ?? path,
            serviceTheme,
            fields);
    }

    private static XmlNode? FindChildItem(XmlNode parent, string name)
        => parent.SelectNodes("./item")
            ?.Cast<XmlNode>()
            .FirstOrDefault(item => String.Equals(
                item.Attributes?["name"]?.Value,
                name,
                StringComparison.OrdinalIgnoreCase));

    private static EditFormPlaygroundField? ParseField(XmlNode fieldNode)
    {
        var configText = fieldNode.InnerText;
        if (String.IsNullOrWhiteSpace(configText))
        {
            throw new FormatException(
                $"Editing field '{fieldNode.Attributes?["name"]?.Value}' has no configuration.");
        }

        var config = new XmlDocument { XmlResolver = null };
        try
        {
            config.LoadXml(configText);
        }
        catch (XmlException exception)
        {
            throw new FormatException(
                $"Editing field '{fieldNode.Attributes?["name"]?.Value}' has invalid configuration XML.",
                exception);
        }

        var fieldName = ConfigValue(config, "field");
        if (String.IsNullOrWhiteSpace(fieldName))
        {
            throw new FormatException(
                $"Editing field '{fieldNode.Attributes?["name"]?.Value}' has no field name.");
        }

        var autoValueId = Int32.TryParse(ConfigValue(config, "autovalue"), out var parsedId)
            ? parsedId
            : 0;
        var autoValue = autoValueId switch
        {
            0 => String.Empty,
            1 => ConfigValue(config, "customautovalue"),
            _ => AutoValueNames.TryGetValue(autoValueId, out var name) ? name : $"unknown:{autoValueId}"
        };

        var categoryNode = fieldNode.ParentNode;
        return new EditFormPlaygroundField(
            fieldNode.Attributes?["name"]?.Value ?? fieldName,
            ConfigValue(config, "name").OrElse(fieldNode.Attributes?["displayname"]?.Value ?? fieldName),
            fieldName,
            categoryNode?.Attributes?["displayname"]?.Value
                ?? categoryNode?.Attributes?["name"]?.Value
                ?? String.Empty,
            FieldTypeName(ConfigValue(config, "type")),
            BooleanValue(config, "visible", defaultValue: true),
            BooleanValue(config, "readonly"),
            BooleanValue(config, "required"),
            autoValue,
            ConfigValue(config, "customautovalue"),
            ConfigValue(config, "customautovalue2"));
    }

    private static void EvaluateField(
        EditFormPlaygroundField field,
        EditFormPlaygroundTheme theme,
        EditFormPlaygroundRequest request,
        IReadOnlyDictionary<string, string> parameterValues,
        int featureCount,
        List<EditFormPlaygroundResult> results,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> properties)
    {
        var autoValue = field.AutoValue.Trim();
        if (String.IsNullOrEmpty(autoValue))
        {
            AddPassThroughResults(results, field, properties, null);
            return;
        }

        if (autoValue.StartsWith("mask-insert-default::", StringComparison.OrdinalIgnoreCase))
        {
            AddPassThroughResults(
                results,
                field,
                properties,
                "Insert-mask defaults are displayed by the editor and are not applied as AutoValues.");
            return;
        }

        var conditionalValue = autoValue;
        if (conditionalValue.StartsWith("oninsert:", StringComparison.OrdinalIgnoreCase))
        {
            if (request.Operation != "insert")
            {
                AddPassThroughResults(results, field, properties, "Only evaluated on insert.");
                return;
            }
            conditionalValue = conditionalValue["oninsert:".Length..];
        }
        else if (conditionalValue.StartsWith("onupdate:", StringComparison.OrdinalIgnoreCase))
        {
            if (request.Operation != "update")
            {
                AddPassThroughResults(results, field, properties, "Only evaluated on update.");
                return;
            }
            conditionalValue = conditionalValue["onupdate:".Length..];
        }

        if (conditionalValue.StartsWith('='))
        {
            try
            {
                var evaluated = ExpressionPlaygroundService.Evaluate(
                    request.GeoJson,
                    conditionalValue,
                    CmsExpressionType.AutoValue);
                foreach (var result in evaluated)
                {
                    results.Add(new EditFormPlaygroundResult(
                        field.FieldName,
                        result.Index,
                        result.Result,
                        result.Error));
                }
            }
            catch (Exception exception) when (
                exception is ArgumentException
                or FormatException
                or JsonException
                or Newtonsoft.Json.JsonException)
            {
                AddFieldResults(results, field.FieldName, featureCount, null, exception.Message);
            }
            return;
        }

        if (parameterValues.TryGetValue(conditionalValue, out var explicitContextValue))
        {
            AddFieldResults(results, field.FieldName, featureCount, explicitContextValue, null);
            return;
        }

        if (TryGetParameterValue(conditionalValue, parameterValues, out var parameterValue))
        {
            AddFieldResults(results, field.FieldName, featureCount, parameterValue, null);
            return;
        }

        if (conditionalValue is "db_select" or "db_select_on_insert")
        {
            EvaluateDbSelect(conditionalValue, field, request, properties, results);
            return;
        }

        var shapeExpression = ShapeExpression(conditionalValue);
        if (shapeExpression is not null)
        {
            try
            {
                var evaluated = ExpressionPlaygroundService.Evaluate(
                    request.GeoJson,
                    shapeExpression,
                    CmsExpressionType.AutoValue);
                foreach (var result in evaluated)
                {
                    results.Add(new EditFormPlaygroundResult(
                        field.FieldName,
                        result.Index,
                        result.Result,
                        result.Error));
                }
            }
            catch (Exception exception) when (
                exception is ArgumentException
                or FormatException
                or JsonException
                or Newtonsoft.Json.JsonException)
            {
                AddFieldResults(results, field.FieldName, featureCount, null, exception.Message);
            }
            return;
        }

        if (TryGetContextValue(conditionalValue, theme, request, out var contextValue))
        {
            AddFieldResults(results, field.FieldName, featureCount, contextValue, null);
            return;
        }

        AddFieldResults(
            results,
            field.FieldName,
            featureCount,
            null,
            GetUnsupportedReason(conditionalValue));
    }

    private static void EvaluateDbSelect(
        string autoValue,
        EditFormPlaygroundField field,
        EditFormPlaygroundRequest request,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> properties,
        List<EditFormPlaygroundResult> results)
    {
        if (autoValue == "db_select_on_insert" && request.Operation != "insert")
        {
            AddPassThroughResults(results, field, properties, "Only evaluated on insert.");
            return;
        }

        var connectionString = field.AutoValueConnection.Trim();
        var sql = field.AutoValueSql.Trim();
        var isDataLinq = connectionString.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        string? configurationError = null;
        if (connectionString.Length == 0)
        {
            configurationError = "ConnectionString not set (custom AutoValue).";
        }
        else if (connectionString.Contains("{{secret-", StringComparison.OrdinalIgnoreCase))
        {
            configurationError = "ConnectionString contains an unresolved CMS secret. Select a deployment whose environment defines this secret.";
        }
        else if (sql.Length == 0)
        {
            configurationError = "SQL statement not set (custom AutoValue 2).";
        }
        else if (!isDataLinq && !sql.StartsWith("select", StringComparison.OrdinalIgnoreCase)
            && !sql.StartsWith("with", StringComparison.OrdinalIgnoreCase))
        {
            configurationError = "Only SELECT statements are executed by the simulator.";
        }

        if (configurationError is not null)
        {
            for (var index = 0; index < properties.Count; index++)
            {
                results.Add(new EditFormPlaygroundResult(field.FieldName, index + 1, null, configurationError));
            }
            return;
        }

        var keys = Globals.KeyParameters(sql, "{{", "}}") ?? Array.Empty<string>();
        for (var index = 0; index < properties.Count; index++)
        {
            string? value = null;
            string? error = null;
            try
            {
                var missing = keys.Where(key => !properties[index].ContainsKey(key)).Distinct().ToArray();
                if (missing.Length > 0)
                {
                    throw new ArgumentException(
                        $"SQL placeholders not found in GeoJSON properties: {String.Join(", ", missing)}");
                }

                if (isDataLinq)
                {
                    value = GetDataLinqValue(connectionString, sql, keys, properties[index]);
                    results.Add(new EditFormPlaygroundResult(field.FieldName, index + 1, value, null));
                    continue;
                }

                using var dbFactory = new DBFactory(connectionString);
                using var connection = dbFactory.GetConnection();
                using var command = dbFactory.GetCommand(connection);
                command.CommandTimeout = 15;

                var commandText = sql;
                var parameterIndex = 0;
                foreach (var key in keys)
                {
                    var parameterName = dbFactory.ParaName($"p{parameterIndex++}");
                    command.Parameters.Add(dbFactory.GetParameter(parameterName, properties[index][key]));
                    commandText = commandText.Replace("{{" + key + "}}", parameterName);
                }

                command.CommandText = commandText;
                connection.Open();
                value = command.ExecuteScalar()?.ToString();
            }
            catch (Exception exception)
            {
                error = $"Database AutoValue failed: {exception.Message.Replace(connectionString, "***")}";
            }

            results.Add(new EditFormPlaygroundResult(field.FieldName, index + 1, value, error));
        }
    }

    private static readonly System.Net.Http.HttpClient DataLinqClient = new() { Timeout = TimeSpan.FromSeconds(15) };

    private static string? GetDataLinqValue(
        string url,
        string query,
        string[] keys,
        IReadOnlyDictionary<string, string?> properties)
    {
        foreach (var key in keys)
        {
            query = query.Replace("{{" + key + "}}", Uri.EscapeDataString(properties[key] ?? String.Empty));
        }

        url += (url.Contains('?') ? "&" : "?") + query;
        using var response = DataLinqClient.Send(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url));
        response.EnsureSuccessStatusCode();
        using var reader = new System.IO.StreamReader(response.Content.ReadAsStream());
        using var document = JsonDocument.Parse(reader.ReadToEnd());

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException("DataLinq endpoint must return a JSON array.");
        }

        var first = document.RootElement.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in first.EnumerateObject())
        {
            if (String.Equals(property.Name, "name", StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.ToString();
            }
        }
        return null;
    }

    private static bool TryGetContextValue(
        string autoValue,
        EditFormPlaygroundTheme theme,
        EditFormPlaygroundRequest request,
        out string? value)
    {
        var now = DateTime.Now;
        var username = request.Username.Trim();
        var login = username.Contains("::", StringComparison.Ordinal)
            ? username[(username.LastIndexOf("::", StringComparison.Ordinal) + 2)..]
            : username;
        var shortLogin = login.Contains('@') ? login[..login.IndexOf('@')] : login;
        var serviceTheme = theme.ServiceTheme.Trim('/');
        var pathParts = serviceTheme.Split('/', StringSplitOptions.RemoveEmptyEntries);

        value = autoValue switch
        {
            "guid" => Guid.NewGuid().ToString("N"),
            "guid_sql" => Guid.NewGuid().ToString("B"),
            "guid_v7" => Guid.CreateVersion7().ToString("N"),
            "guid_v7_sql" => Guid.CreateVersion7().ToString("B"),
            "create_login" when request.Operation == "insert" => login,
            "create_login_full" when request.Operation == "insert" => username,
            "create_login_short" when request.Operation == "insert" => shortLogin,
            "create_login_domain" when request.Operation == "insert" => GetDomain(login),
            "change_login" => login,
            "change_login_full" => username,
            "change_login_short" => shortLogin,
            "change_login_domain" => GetDomain(login),
            "create_user" when request.Operation == "insert" => request.DatabaseUsername,
            "change_user" => request.DatabaseUsername,
            "create_date" when request.Operation == "insert" => now.ToShortDateString(),
            "create_time" when request.Operation == "insert" => now.ToShortTimeString(),
            "create_datetime_sql" when request.Operation == "insert" => $"{now.ToShortDateString()} {now.ToShortTimeString()}",
            "create_datetime_sql2" when request.Operation == "insert" => now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.CurrentCulture),
            "create_datetime_utc" when request.Operation == "insert" => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            "change_date" => now.ToShortDateString(),
            "change_time" => now.ToShortTimeString(),
            "change_datetime_sql" => $"{now.ToShortDateString()} {now.ToShortTimeString()}",
            "change_datetime_sql2" => now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.CurrentCulture),
            "change_datetime_utc" => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            "datetime" => now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            "scale" => Math.Round(request.MapScale, 0).ToString(CultureInfo.InvariantCulture),
            "edit_operation" => request.Operation,
            "map_srefid" => request.MapSrefId.ToString(CultureInfo.InvariantCulture),
            "edit_service_id" => null,
            "edit_layer_id" => pathParts.Length > 0 ? pathParts[^1] : null,
            "edit_theme_id" => null,
            _ => null
        };

        if (String.IsNullOrEmpty(value)
            && autoValue is "create_user" or "change_user")
        {
            return false;
        }

        return value is not null;
    }

    private static string? ShapeExpression(string autoValue)
    {
        var parts = autoValue.Split(':', 2);
        var metric = parts[0];
        var targetSref = String.Empty;
        if (parts.Length == 2)
        {
            if (!Int32.TryParse(parts[1], out var targetSrefId) || targetSrefId <= 0)
            {
                return null;
            }
            targetSref = targetSrefId.ToString(CultureInfo.InvariantCulture);
        }

        var function = metric switch
        {
            "shape_len" or "shape_len_int" => "shape_len",
            "shape_area" or "shape_area_int" => "shape_area",
            "shape_perimeter" or "shape_centroid_x" or "shape_centroid_y" => metric,
            _ => String.Empty
        };
        if (String.IsNullOrEmpty(function))
        {
            return null;
        }

        var functionCall = $"{function}({targetSref})";
        return metric switch
        {
            "shape_len_int" or "shape_area_int" => $"=round({functionCall}, 0)",
            _ => $"={functionCall}"
        };
    }

    private static bool TryGetParameterValue(
        string autoValue,
        IReadOnlyDictionary<string, string> parameterValues,
        out string? value)
    {
        var prefix = autoValue.StartsWith("role-parameter:", StringComparison.OrdinalIgnoreCase)
            ? "role-parameter:"
            : autoValue.StartsWith("url-parameter:", StringComparison.OrdinalIgnoreCase)
                ? "url-parameter:"
                : String.Empty;

        if (String.IsNullOrEmpty(prefix))
        {
            value = null;
            return false;
        }

        var key = autoValue[prefix.Length..];
        return parameterValues.TryGetValue(key, out value);
    }

    private static IReadOnlyDictionary<string, string> ParseValues(string valuesJson)
    {
        if (String.IsNullOrWhiteSpace(valuesJson))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        using var document = JsonDocument.Parse(valuesJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Context values must be a JSON object.", nameof(valuesJson));
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False))
            {
                throw new ArgumentException(
                    $"Context value '{property.Name}' must be a string, number, or boolean.",
                    nameof(valuesJson));
            }

            var value = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? String.Empty
                : property.Value.ToString();
            if (property.Name.Length > 128 || value.Length > 512 || values.Count >= 50)
            {
                throw new ArgumentException(
                    "Context values are limited to 50 entries, 128-character keys, and 512-character values.",
                    nameof(valuesJson));
            }
            values[property.Name] = value;
        }

        return values;
    }

    private static string GetUnsupportedReason(string autoValue)
        => autoValue switch
        {
            "db_select" or "db_select_on_insert" =>
                "Database AutoValues are not executed by this local simulation.",
            _ when autoValue.Contains(" from ", StringComparison.OrdinalIgnoreCase) =>
                "Spatial service queries are not executed by this local simulation.",
            "shape_vertex_count" or "shape_part_count" or "shape_type" or "shape_srefid"
                or "shape_minx" or "shape_miny" or "shape_maxx" or "shape_maxy" =>
                "This geometry AutoValue is not yet supported by the simulator.",
            "create_user" or "change_user" =>
                "Enter a simulated database username to preview this AutoValue.",
            _ => $"AutoValue '{autoValue}' requires runtime context not provided to the simulator."
        };

    private static string GetDomain(string login)
        => login.Contains('@') ? login[(login.IndexOf('@') + 1)..] : String.Empty;

    private static string FieldTypeName(string type)
        => Int32.TryParse(type, out var id)
            ? id switch
            {
                0 => "text",
                1 => "domain",
                2 => "textarea",
                3 => "autocomplete",
                10 => "date",
                11 => "date_dateonly",
                20 => "file",
                30 => "angle360",
                31 => "angle360_geographic",
                50 => "attribute_picker",
                _ => $"type:{id}"
            }
            : type;

    private static string GetItemPath(XmlNode node)
        => String.Join(
            "/",
            Ancestors(node)
                .Where(item => item.Attributes?["name"] is not null)
                .Select(item => item.Attributes!["name"]!.Value)
                .Reverse());

    private static IEnumerable<XmlNode> Ancestors(XmlNode node)
    {
        for (var current = node; current is not null && current.Name == "item"; current = current.ParentNode)
        {
            yield return current;
        }
    }

    private static string ConfigValue(XmlDocument config, string key)
        => config.SelectSingleNode($"/config/{key}")?.InnerText ?? String.Empty;

    private static bool BooleanValue(XmlDocument config, string key, bool defaultValue = false)
        => Boolean.TryParse(ConfigValue(config, key), out var value) ? value : defaultValue;

    private static IReadOnlyList<IReadOnlyDictionary<string, string?>> GetFeatureProperties(string geoJson)
    {
        using var document = JsonDocument.Parse(geoJson);
        var root = document.RootElement;
        var featureElements = root.TryGetProperty("features", out var features)
            && features.ValueKind == JsonValueKind.Array
                ? features.EnumerateArray().ToArray()
                : [root];

        return featureElements.Select(feature =>
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (feature.TryGetProperty("properties", out var props)
                && props.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in props.EnumerateObject())
                {
                    values[property.Name] = property.Value.ValueKind switch
                    {
                        JsonValueKind.Null or JsonValueKind.Undefined => null,
                        JsonValueKind.String => property.Value.GetString(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => property.Value.GetRawText()
                    };
                }
            }
            return (IReadOnlyDictionary<string, string?>)values;
        }).ToArray();
    }

    private static void AddPassThroughResults(
        List<EditFormPlaygroundResult> results,
        EditFormPlaygroundField field,
        IReadOnlyList<IReadOnlyDictionary<string, string?>> properties,
        string? note)
    {
        for (var index = 0; index < properties.Count; index++)
        {
            var found = properties[index].TryGetValue(field.FieldName, out var value);
            var message = !found
                ? $"Attribute '{field.FieldName}' not in GeoJSON properties. {note}".Trim()
                : note ?? "Attribute value from GeoJSON (no AutoValue applied).";
            results.Add(new EditFormPlaygroundResult(
                field.FieldName,
                index + 1,
                value,
                found && note is null ? null : message));
        }
    }

    private static void AddFieldResults(
        List<EditFormPlaygroundResult> results,
        string fieldName,
        int featureCount,
        string? value,
        string? error)
    {
        for (var feature = 1; feature <= featureCount; feature++)
        {
            results.Add(new EditFormPlaygroundResult(fieldName, feature, value, error));
        }
    }

    private static string OrElse(this string value, string fallback)
        => String.IsNullOrEmpty(value) ? fallback : value;
}
