using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using E.Standard.CMS.Core.Extensions;
using E.Standard.DbConnector;
using E.Standard.Extensions.IO;
using E.Standard.Json;
using E.Standard.Parsing;
using E.Standard.Parsing.SimpleExpressions;
using E.Standard.Parsing.StructuredExpressions;
using E.Standard.Platform;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;

using static E.Standard.WebGIS.Tools.Editing.Environment.EditEnvironment;

namespace E.Standard.WebGIS.Tools.Editing.Environment.Services;

internal class EditAutoValueService
{
    private readonly EditEnvironment _editEnvironment;
    private readonly EditFeatureCommand _editTask;
    private readonly string _targetFieldName;
    private readonly EditTheme _editTheme;
    private readonly string _autoValue;
    private readonly WebMapping.Core.Feature _feature;
    private readonly string _custom1;
    private readonly string _custom2;

    public EditAutoValueService(
        EditEnvironment editEnvironment,
        EditFeatureCommand editTask,
        string targetFieldName,
        EditTheme editTheme,
        string autoValue,
        WebMapping.Core.Feature feature,
        string custom1,
        string custom2)
    {
        _editEnvironment = editEnvironment;
        _editTask = editTask;
        _targetFieldName = targetFieldName;
        _editTheme = editTheme;
        _autoValue = autoValue;
        _feature = feature;
        _custom1 = custom1;
        _custom2 = custom2;
    }

    public async Task<(string value, bool setIt)> GetAutoValueAsync()
    {
        if (_editTheme == null
            || String.IsNullOrEmpty(_autoValue)
            || _autoValue.StartsWith("mask-insert-default::"))
        {
            return (value: String.Empty, setIt: false);
        }

        var autoValue = _autoValue.Trim();

        if (_feature?.Shape != null && autoValue.ToLower().Contains(" from "))
        {
            return await GetSpatialAutoValueAsync(autoValue);
        }

        if (autoValue.IndexOf("=") == 0)
        {
            return (value: GetExpressionAutoValue(autoValue), setIt: true);
        }

        var roleParameter = GetConditionalParameterName(autoValue, "role-parameter:");
        if (!String.IsNullOrEmpty(roleParameter))
        {
            var roleParameterValue = _editEnvironment.Bridge?
                .CurrentUser?
                .UserRoleParameters
                .ParameterValue<string>(roleParameter);

            return (value: roleParameterValue ?? String.Empty, setIt: true);
        }

        var urlParameter = GetConditionalParameterName(autoValue, "url-parameter:");
        if (!String.IsNullOrEmpty(urlParameter))
        {
            var urlParameterValue = _editEnvironment.Bridge.GetOriginalUrlParameterValue(urlParameter);
            return (value: urlParameterValue ?? String.Empty, setIt: true);
        }

        return await GetSimpleAutoValueAsync(autoValue);
    }

    private async Task<(string value, bool setIt)> GetSpatialAutoValueAsync(string autoValue)
    {
        string[] args = autoValue.SplitQuotedString();

        double bufferDist = 0.0;
        string seperator = ";", serviceId = "";
        int count = 20;
        for (int i = 3; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "bufferdist":
                    bufferDist = Convert.ToDouble(args[++i].Replace(".", ","));
                    break;
                case "seperator":
                    seperator = args[++i];
                    seperator = seperator.Replace("space", " ");
                    break;
                case "max":
                    count = Convert.ToInt32(args[++i]);
                    break;
                case "service":
                    serviceId = args[++i];
                    break;
            }
        }

        ILayerBridge layer = null;

        string fieldName = String.Empty;
        if (args.Length > 2)
        {
            fieldName = args[0].Trim();
            string layerName = args[2].Trim();
            IServiceBridge service = await _editEnvironment.Bridge.GetService(serviceId);
            if (service == null)
            {
                throw new ArgumentException("Autovalue: Service " + serviceId + " not found");
            }

            layer = service.Layers.Where(l => l.Name == layerName).FirstOrDefault();
            if (layer == null)
            {
                layer = service.Layers.Where(l => l.Id == layerName).FirstOrDefault();
            }

            if (layer == null)
            {
                throw new ArgumentException("Layer " + layerName + " not found: " + autoValue);
            }

            if (layer.GeometryType == LayerGeometryType.point)
            {
                bufferDist = Math.Max(bufferDist, 0.03);
            }
        }

        if (layer != null && !String.IsNullOrEmpty(fieldName))
        {
            WebMapping.Core.Filters.BufferFilter shapeBuffer = _feature.Shape.Buffer;
            Shape filterShape = _feature.Shape;
            SpatialReference filterSref = _editEnvironment.Bridge.CreateSpatialReference(_feature.Shape.SrsId);

            if (bufferDist != 0.0)
            {
                _feature.Shape.Buffer = null;
                using (var cts = new CancellationTokenSource())
                {
                    filterShape = filterShape.CalcBuffer(bufferDist, cts) ?? filterShape;
                }
            }

            var features = await _editEnvironment.Bridge.QueryLayerAsync(
                serviceId,
                layer.Id,
                String.Empty,
                QueryFields.All,
                filterSref,
                filterShape);
            _feature.Shape.Buffer = shapeBuffer;

            int featureCounter = 0;
            StringBuilder sb = new StringBuilder();
            foreach (WebMapping.Core.Feature feature in features)
            {
                string value = feature[fieldName];
                if (value == null)
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(seperator);
                }

                sb.Append(value.ToString());

                featureCounter++;
                if (featureCounter >= count)
                {
                    break;
                }
            }

            return (value: sb.ToString(), setIt: true);
        }

        throw new ArgumentException("Spatial-Filter-Autovalues are not supported for API: " + autoValue);
    }

    private string GetExpressionAutoValue(string autoValue)
    {
        string value = autoValue.Substring(1, autoValue.Length - 1);

        if (ExpressionClassifier.Classify(value) == ExpressionSyntax.StructuredExpression)
        {
            try
            {
                return new ExpressionEvaluator()
                    .Evaluate(
                        value,
                        ResolveExpressionField,
                        ResolveExpressionFunction)
                    .ToInvariantString();
            }
            catch (ExpressionException exception)
            {
                throw new ArgumentException(
                    $"{_targetFieldName}: Autovalue expression error: {exception.Message}",
                    exception);
            }
        }

        return GetLegacyExpressionAutoValue(value);
    }

    private string GetLegacyExpressionAutoValue(string value)
    {
        string[] keys = ExtractKeyParameters(value);

        if (keys != null)
        {
            string keyValue = String.Empty;
            foreach (string key in keys)
            {
                if (key is ":shape_len" or ":shape_len_int" or ":shape_area" or ":shape_area_int"
                    && FormatNumericShapeMetric(key.Substring(1), _feature?.Shape) is string shapeValue)
                {
                    keyValue = shapeValue;
                }
                else if (_feature != null && _feature[key] != null)
                {
                    keyValue = _feature[key].ToString();
                }

                value = value.Replace("[" + key + "]", keyValue);
            }
        }

        return value.Trim();
    }

    private ExpressionValue? ResolveExpressionField(string fieldName)
    {
        var attribute = _feature?.Attributes?[fieldName];
        if (attribute is null)
        {
            return null;
        }

        var value = attribute.Value;
        if (Boolean.TryParse(value, out var boolean))
        {
            return ExpressionValue.From(boolean);
        }

        return ExpressionValue.From(value);
    }

    private ExpressionValue? ResolveExpressionFunction(
        string functionName,
        IReadOnlyList<ExpressionValue> arguments)
        => ShapeExpressionFunctions.Resolve(
            _feature?.Shape,
            functionName,
            arguments,
            targetSRefId => GetShapeForCalculation(targetSRefId, functionName));

    private string GetConditionalParameterName(string autoValue, string parameterPrefix)
    {
        if (autoValue.ToLower().StartsWith(parameterPrefix))
        {
            return autoValue.Substring(parameterPrefix.Length);
        }

        string onInsertPrefix = "oninsert:" + parameterPrefix;
        if (autoValue.ToLower().StartsWith(onInsertPrefix) && _editTask == EditFeatureCommand.Insert)
        {
            return autoValue.Substring(onInsertPrefix.Length);
        }

        string onUpdatePrefix = "onupdate:" + parameterPrefix;
        if (autoValue.ToLower().StartsWith(onUpdatePrefix) && _editTask == EditFeatureCommand.Update)
        {
            return autoValue.Substring(onUpdatePrefix.Length);
        }

        return String.Empty;
    }

    private async Task<(string value, bool setIt)> GetSimpleAutoValueAsync(string autoValue)
    {
        var normalizedAutoValue = autoValue.ToLower();

        if (normalizedAutoValue is "db_select" or "db_select_on_insert")
        {
            return await GetDbSelectAutoValueAsync(normalizedAutoValue);
        }

        return GetInsertAutoValue(normalizedAutoValue)
            ?? GetChangeAutoValue(normalizedAutoValue)
            ?? GetShapeAutoValue(normalizedAutoValue)
            ?? GetGeneralAutoValue(normalizedAutoValue)
            ?? (value: String.Empty, setIt: false);
    }

    private (string value, bool setIt)? GetInsertAutoValue(string autoValue)
    {
        if (_editTask != EditFeatureCommand.Insert)
        {
            return null;
        }

        var username = _editEnvironment.Bridge.CurrentUser?.Username;

        return autoValue switch
        {
            "create_user" => (_editTheme.DbUsername, true),
            "create_login" => (username.RemoveUserIdentificationNamespace(), true),
            "create_login_full" => (username, true),
            "create_login_short" => (GetShortLogin(username), true),
            "create_login_domain" => (username.UsernameDomain(), true),
            "guid" => (Guid.NewGuid().ToString("N"), true),
            "guid_sql" => (Guid.NewGuid().ToString("B"), true),
            "guid_v7" => (Guid.CreateVersion7().ToString("N"), true),
            "guid_v7_sql" => (Guid.CreateVersion7().ToString("B"), true),
            "create_date" => (DateTime.Now.ToShortDateString(), true),
            "create_date_yyyy.mm.dd" => (DateTime.Now.ToString("yyyy.MM.dd"), true),
            "create_time" => (DateTime.Now.ToShortTimeString(), true),
            "create_datetime_sql" => (DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString(), true),
            "create_datetime_sql2" => (DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), true),
            "create_datetime_utc" => (
                DateTime.UtcNow.ToString(
                    "yyyy-MM-ddTHH:mm:ss.fffZ",
                    CultureInfo.InvariantCulture),
                true),
            _ => null
        };
    }

    private (string value, bool setIt)? GetChangeAutoValue(string autoValue)
    {
        var username = _editEnvironment.Bridge.CurrentUser?.Username;

        return autoValue switch
        {
            "change_user" => (_editTheme.DbUsername, true),
            "change_login" => (username.RemoveUserIdentificationNamespace(), true),
            "change_login_full" => (username, true),
            "change_login_short" => (GetShortLogin(username), true),
            "change_login_domain" => (username.UsernameDomain(), true),
            "change_date" => (DateTime.Now.ToShortDateString(), true),
            "change_time" => (DateTime.Now.ToShortTimeString(), true),
            "change_datetime_sql" => (DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString(), true),
            "change_datetime_sql2" => (DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), true),
            "change_datetime_utc" => (
                DateTime.UtcNow.ToString(
                    "yyyy-MM-ddTHH:mm:ss.fffZ",
                    CultureInfo.InvariantCulture),
                true),
            _ => null
        };
    }

    private (string value, bool setIt)? GetShapeAutoValue(string autoValue)
    {
        if (_feature?.Shape == null)
        {
            return null;
        }

        if (!TryParseShapeAutoValue(autoValue, out var shapeValueName, out var targetSRefId))
        {
            return null;
        }

        var shape = GetShapeForCalculation(targetSRefId, autoValue);

        var value = shapeValueName switch
        {
            "shape_vertex_count" => SpatialAlgorithms.VertexCount(shape).ToString(),
            "shape_part_count" => SpatialAlgorithms.PartCount(shape).ToString(),
            "shape_type" => ShapeMetrics.GetTypeName(shape),
            "shape_srefid" => shape.SrsId.ToString(),
            _ => FormatNumericShapeMetric(shapeValueName, shape)
        };

        return value is null ? null : (value, true);
    }

    private static string FormatNumericShapeMetric(string shapeValueName, Shape shape)
    {
        var asInteger = shapeValueName is "shape_len_int" or "shape_area_int";
        var metricName = asInteger
            ? shapeValueName.Substring(0, shapeValueName.Length - "_int".Length)
            : shapeValueName;

        if (ShapeMetrics.GetNumericMetric(metricName, shape) is not double value)
        {
            return null;
        }

        if (asInteger)
        {
            return Math.Round(value, 0).ToString();
        }

        return metricName is "shape_len" or "shape_area" or "shape_perimeter"
            ? Math.Round(value, 2).ToPlatformNumberString()
            : value.ToPlatformNumberString();
    }

    private (string value, bool setIt)? GetGeneralAutoValue(string autoValue)
        => autoValue switch
        {
            "datetime" => (DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), true),
            "scale" => (Math.Round(_editEnvironment.CurrentMapScale, 0).ToString(), true),
            "edit_operation" => (GetEditOperationName(), true),
            "map_srefid" => (_editEnvironment.CurrentMapSrsId.ToString(CultureInfo.InvariantCulture), true),
            "edit_service_id" => (_editEnvironment.EditThemeDefinition?.ServiceId ?? String.Empty, true),
            "edit_layer_id" => (_editEnvironment.EditThemeDefinition?.LayerId ?? String.Empty, true),
            "edit_theme_id" => (_editEnvironment.EditThemeDefinition?.EditThemeId ?? String.Empty, true),
            _ => null
        };

    private string GetEditOperationName()
        => _editTask switch
        {
            EditFeatureCommand.Insert => "insert",
            EditFeatureCommand.Update => "update",
            EditFeatureCommand.Delete => "delete",
            EditFeatureCommand.MassAttribution => "mass_attribution",
            EditFeatureCommand.Transfer => "transfer",
            _ => throw new ArgumentOutOfRangeException(
                nameof(_editTask),
                _editTask,
                "Unknown edit operation")
        };

    private Shape GetShapeForCalculation(int? targetSRefId, string autoValue)
    {
        if (!targetSRefId.HasValue || targetSRefId.Value == _feature.Shape.SrsId)
        {
            return _feature.Shape;
        }

        if (_feature.Shape.SrsId <= 0)
        {
            throw new ArgumentException(
                $"Autovalue {autoValue}: Source SRefId is not set");
        }

        return _feature.Shape.TransformedCopy(
            targetSRefId.Value,
            _editEnvironment.Bridge.GeometryTransformer);
    }

    private static bool TryParseShapeAutoValue(
        string autoValue,
        out string shapeValueName,
        out int? targetSRefId)
    {
        var parts = autoValue.Split(':', 2);
        shapeValueName = parts[0];
        targetSRefId = null;

        if (!ShapeMetrics.IsNumericMetric(shapeValueName)
            && shapeValueName is not ("shape_len_int" or "shape_area_int")
            && !IsShapeValueWithoutSRefId(shapeValueName))
        {
            return false;
        }

        if (parts.Length == 2)
        {
            if (IsShapeValueWithoutSRefId(shapeValueName))
            {
                throw new ArgumentException(
                    $"Autovalue {autoValue}: Target SRefId is not supported");
            }

            if (!Int32.TryParse(parts[1], out var parsedSRefId) || parsedSRefId <= 0)
            {
                throw new ArgumentException(
                    $"Autovalue {autoValue}: Invalid target SRefId");
            }

            targetSRefId = parsedSRefId;
        }

        return true;
    }

    private static bool IsShapeValueWithoutSRefId(string shapeValueName)
        => shapeValueName is "shape_vertex_count" or "shape_part_count" or "shape_type" or "shape_srefid";

    private static string GetShortLogin(string username)
        => username
            .RemoveUserIdentificationNamespace()
            .RemoveUserIdentificationDomain();

    private async Task<(string value, bool setIt)> GetDbSelectAutoValueAsync(string autoValue)
    {
        if (autoValue.ToLower() == "db_select_on_insert" && _editTask != EditFeatureCommand.Insert)
        {
            return (value: String.Empty, setIt: false);
        }

        if (String.IsNullOrEmpty(_custom1))
        {
            throw new ArgumentException("Autovalue db_select: ConnectionString not set! Set ConnectionString in autovalue_custom1!");
        }
        if (String.IsNullOrEmpty(_custom2))
        {
            throw new ArgumentException("Autovalue db_select: SQL Statement not set! Set SQL Select Statement in autovalue_custom2!");
        }

        try
        {
            var sql = _custom2;
            sql = _editEnvironment.Bridge.ReplaceUserAndSessionDependentFilterKeys(
                sql,
                startingBracket: "{{",
                endingBracket: "}}");
            var sqlKeyParameters = Globals.KeyParameters(
                sql,
                startingBracket: "{{",
                endingBracket: "}}") ?? Array.Empty<string>();

            if (_editTask == EditFeatureCommand.MassAttribution)
            {
                int containedAttributesCount = sqlKeyParameters
                    .Where(parameter => _feature.Attributes[parameter] != null)
                    .Count();
                if (containedAttributesCount == 0)
                {
                    return (value: null, setIt: false);
                }
                else if (containedAttributesCount < sqlKeyParameters.Distinct().Count())
                {
                    var missingAttributeNames = sqlKeyParameters
                        .Where(parameter => _feature.Attributes[parameter] == null)
                        .ToArray();
                    throw new Exception(
                        $"Nicht alle notewendigen Felder wurden übergeben. Folgende Attribute sind nicht in der Massenattributierung enthalten: [{String.Join(", ", missingAttributeNames)}]");
                }
            }

            if (_custom1.IsValidHttpUrl())
            {
                return await GetDataLinqAutoValueAsync(sqlKeyParameters);
            }

            using (var dbFactory = new DBFactory(_custom1))
            using (var connection = dbFactory.GetConnection())
            using (var command = dbFactory.GetCommand(connection))
            {
                int index = 0;
                foreach (var sqlKeyParameter in sqlKeyParameters)
                {
                    var parameterName = dbFactory.ParaName($"p{index++}");
                    command.Parameters.Add(
                        dbFactory.GetParameter(
                            parameterName,
                            _feature.Attributes[sqlKeyParameter]?.Value));
                    sql = sql.Replace("{{" + sqlKeyParameter + "}}", parameterName);
                }

                command.CommandText = sql;
                await connection.OpenAsync();
                var value = await command.ExecuteScalarAsync();

                return (value: value?.ToString(), setIt: true);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"{_targetFieldName}: Autovalue - db_select: {ex.Message}", ex);
        }
    }

    private async Task<(string value, bool setIt)> GetDataLinqAutoValueAsync(
        string[] sqlKeyParameters)
    {
        var url = _custom1;
        var query = _custom2;

        if (!String.IsNullOrEmpty(query))
        {
            foreach (var sqlKeyParameter in sqlKeyParameters)
            {
                query = query.Replace(
                    $"{{{{{sqlKeyParameter}}}}}",
                    System.Web.HttpUtility.UrlEncode(
                        _feature.Attributes[sqlKeyParameter]?.Value ?? String.Empty));
            }

            url += $"{(url.Contains("?") ? "&" : "?")}{query}";
        }

        var jsonResult = await _editEnvironment.Bridge.HttpService.GetStringAsync(url);

        var firstElement = JSerializer.Deserialize<object[]>(jsonResult).FirstOrDefault();
        string firstElementValue = null;

        if (firstElement != null)
        {
            string labelProperty = "name";
            firstElementValue = JSerializer.GetJsonElementValue(firstElement, labelProperty)?.ToString();
        }

        return (value: firstElementValue, setIt: true);
    }

    // Legacy semantics: keys are joined with ';' and split again, so ';' inside brackets separates keys.
    private static string[] ExtractKeyParameters(string commandLine)
    {
        var parameters = TemplateScanner.Scan(commandLine)
            .Aggregate(
                String.Empty,
                (current, parameter) => current == String.Empty
                    ? parameter.Key
                    : current + ";" + parameter.Key);

        return parameters != String.Empty
            ? parameters.Split(';')
            : null;
    }
}
