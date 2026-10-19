#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using E.Standard.WebMapping.Core.Abstraction;

using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIQueryBuilder : UICollapsableElement
{
    public const int AutocompleteMaxValues = 20;
    public const int MaxDistinctComboValues = 25;
    public const string DateValueTemplate = "DATE '{0}'";

    private List<FieldDef> _fieldDefs = new List<FieldDef>();

    public UIQueryBuilder()
        : base("query-builder")
    {
    }

    [JsonProperty("field_defs")]
    [System.Text.Json.Serialization.JsonPropertyName("field_defs")]
    public IEnumerable<object> FieldDefs =>
        _fieldDefs/*.OrderBy(x => x.Name)*/;

    [JsonProperty("show_geometry_option")]
    [System.Text.Json.Serialization.JsonPropertyName("show_geometry_option")]
    public bool ShowGeometryOption { get; set; }

    [JsonProperty("callback_tool_id", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("callback_tool_id")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? CallbackToolId { get; set; }

    [JsonProperty("callback_argument", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("callback_argument")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? CallbackArgument { get; set; }

    /// <summary>
    /// The callback tool needs the current display filters (eg. callback tool is not part of the map tools)
    /// </summary>
    [JsonProperty("callback_visfilter_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("callback_visfilter_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? CallbackVisFilterDependent { get; set; }

    /// <summary>
    /// Optional: shows a button, that submits an empty definition to the callback tool (remove all conditions).
    /// Null => no button
    /// </summary>
    [JsonProperty("remove_button_label", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("remove_button_label")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? RemoveButtonLabel { get; set; }

    [JsonProperty("whereclause_parts", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("whereclause_parts")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[]? WhereClauseParts { get; private set; }

    /// <summary>
    /// Url (tool method source) for value suggestions. The client appends field, mode (autocomplete|distinct) and term.
    /// Null => no live values, only type dependent inputs.
    /// </summary>
    [JsonProperty("values_source", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("values_source")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? ValuesSource { get; set; }

    public void TryAddField(string fieldName, FieldType fieldType = FieldType.Unknown, string? alias = null)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return;
        }

        var fieldDef = _fieldDefs.Where(f => fieldName.Equals(f.Name)).FirstOrDefault();
        if (fieldDef == null)
        {
            fieldDef = new FieldDef(fieldName, fieldType)
            {
                Aliasname = alias
            };
            _fieldDefs.Add(fieldDef);
        }
    }

    // Optional: only for reference – not strictly needed below
    //private static readonly string[] possibleOperators =
    //    { "=", " like ", " in ", "<>", ">", ">=", "<", "<=" };

    public void TrySetWhereClause(string whereClause)
    {
        if (string.IsNullOrWhiteSpace(whereClause))
        {
            return;
        }

        // The order of alternatives matters (longer/more complex ones first)
        const string pattern =
            // logical operators
            @"\bAND\b|\bOR\b" +
            // comparison operators
            @"|<>|>=|<=|=|>|<" +
            // LIKE / IN
            @"|\bLIKE\b|\bIN\b" +
            // IN-lists in parentheses
            @"|\([^\)]*\)" +
            // date/timestamp literals: DATE '2024-01-31'
            @"|\b(?:DATE|TIMESTAMP)\s*'(?:[^']|'')*'" +
            // string literals with escaped ''
            @"|'(?:[^']|'')*'" +
            // identifiers
            @"|[A-Za-z_][A-Za-z0-9_]*" +
            // numeric literals (simplified)
            @"|\d+(?:\.\d+)?";

        var tokens = new List<string>();
        foreach (Match m in Regex.Matches(whereClause, pattern, RegexOptions.IgnoreCase))
        {
            string t = m.Value;

            if (Regex.IsMatch(t, @"^(?:AND|OR)$", RegexOptions.IgnoreCase))
            {
                tokens.Add(t.ToLowerInvariant()); // normalize to "and"/"or"
                continue;
            }

            if (Regex.IsMatch(t, @"^(?:LIKE|IN)$", RegexOptions.IgnoreCase))
            {
                // add with surrounding spaces: " like " / " in "
                tokens.Add($" {t.ToLowerInvariant()} ");
                continue;
            }

            if (t.StartsWith("(") && t.EndsWith(")") && t.Length >= 2)
            {
                // IN-list: remove parentheses, keep content as one token
                tokens.Add(string.Join(",",
                               t.Substring(1, t.Length - 2)         // remove "(" and ")"
                                .Split(',')                         // split by ","
                                .Select(v => v.Trim().Trim('\''))   // remove whitespace and outer quotes
                            )
                            .Trim());
                continue;
            }

            var dateLiteral = Regex.Match(t, @"^(?:DATE|TIMESTAMP)\s*'(.*)'$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (dateLiteral.Success)
            {
                tokens.Add(dateLiteral.Groups[1].Value.Replace("''", "'"));
                continue;
            }

            if (t.Length >= 2 && t[0] == '\'' && t[^1] == '\'')
            {
                // string literal without outer quotes, '' -> '
                string inner = t.Substring(1, t.Length - 2).Replace("''", "'");
                tokens.Add(inner);
                continue;
            }

            // identifiers, numbers, or comparison operators
            tokens.Add(t);
        }

        WhereClauseParts = tokens.ToArray();
    }

    /// <summary>
    /// Validates a client result against the (allowed) layer fields: field must exist, operator must fit the field type,
    /// value template is taken from the server side field type and values are escaped/validated.
    /// Throws an ArgumentException for invalid definitions.
    /// </summary>
    static public Result Sanitize(Result? result, IEnumerable<IField> allowedFields)
    {
        var queryDefs = new List<Result.QueryDef>();

        foreach (var queryDef in result?.QueryDefs ?? [])
        {
            var field = allowedFields.FirstOrDefault(f => f.Name.Equals(queryDef.Field, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"The field '{queryDef.Field}' is not allowed");

            var fieldDef = new FieldDef(field.Name, field.Type);

            var @operator = fieldDef.Operators.FirstOrDefault(o => o.Equals(queryDef.Operator, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"Invalid operator for field {field.Name}: {queryDef.Operator}");

            var logicalOperator = (queryDef.LogicalOperator ?? "").Trim().ToLowerInvariant();
            if (logicalOperator != "" && logicalOperator != "and" && logicalOperator != "or")
            {
                throw new ArgumentException($"Invalid logical operator: {queryDef.LogicalOperator}");
            }

            var values = @operator == " in "
                ? (queryDef.Value ?? "").Split(',').Select(v => v.Trim().Trim('\''))
                : new[] { queryDef.Value ?? "" };

            values = values.Select(v => fieldDef.ValueType switch
            {
                "number" => double.TryParse(v.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                                ? v.Trim()
                                : throw new ArgumentException($"Invalid number value for field {field.Name}: {v}"),
                "boolean" => v.Trim().ToLowerInvariant() switch
                {
                    "true" or "false" or "0" or "1" => v.Trim(),
                    _ => throw new ArgumentException($"Invalid boolean value for field {field.Name}: {v}")
                },
                _ => v.Replace("'", "''")
            }).ToArray();

            queryDefs.Add(new Result.QueryDef()
            {
                Field = field.Name,
                Operator = @operator,
                Value = String.Join(",", values),
                ValueTemplate = fieldDef.ValueTemplate,
                LogicalOperator = logicalOperator
            });
        }

        if (queryDefs.Count > 0)
        {
            queryDefs[^1].LogicalOperator = "";
        }

        return new Result()
        {
            GeometryOption = result?.GeometryOption,
            QueryDefs = queryDefs
        };
    }

    #region Classes

    private class FieldDef
    {
        public FieldDef(string fieldName, FieldType fieldType)
        {
            Name = fieldName;

            switch (fieldType)
            {
                case FieldType.String:
                    Operators = new[] { "=", " like ", " in ", "<>", ">", ">=", "<", "<=" };
                    ValueTemplate = "'{0}'";
                    ValueType = "string";
                    break;
                case FieldType.Interger:
                case FieldType.BigInteger:
                case FieldType.SmallInteger:
                case FieldType.Float:
                case FieldType.Double:
                    Operators = new[] { "=", " in ", "<>", ">", ">=", "<", "<=" };
                    ValueTemplate = "{0}";
                    ValueType = "number";
                    break;
                case FieldType.Date:
                    Operators = new[] { "=", "<>", ">", ">=", "<", "<=" };
                    ValueTemplate = DateValueTemplate;
                    ValueType = "date";
                    break;
                case FieldType.Boolean:
                    Operators = new[] { "=", "<>" };
                    ValueTemplate = "{0}"; // ???
                    ValueType = "boolean";
                    break;
                default:
                    Operators = new[] { "=" };
                    ValueTemplate = "'{0}'";
                    ValueType = "unknown";
                    break;
            }
        }

        [JsonProperty("field_type")]
        [System.Text.Json.Serialization.JsonPropertyName("field_type")]
        public string ValueType { get; set; }

        [JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("alias", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("alias")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Aliasname { get; set; }

        [JsonProperty("operators")]
        [System.Text.Json.Serialization.JsonPropertyName("operators")]
        public IEnumerable<string> Operators { get; set; }

        [JsonProperty("value_template")]
        [System.Text.Json.Serialization.JsonPropertyName("value_template")]
        public string ValueTemplate { get; set; }
    }

    public class Result
    {
        [JsonProperty("geometry_option")]
        [System.Text.Json.Serialization.JsonPropertyName("geometry_option")]
        public string? GeometryOption { get; set; }

        [JsonProperty("query_defs")]
        [System.Text.Json.Serialization.JsonPropertyName("query_defs")]
        public IEnumerable<QueryDef>? QueryDefs { get; set; }

        public class QueryDef
        {
            [JsonProperty("field")]
            [System.Text.Json.Serialization.JsonPropertyName("field")]
            public string Field { get; set; } = "";
            [JsonProperty("operator")]
            [System.Text.Json.Serialization.JsonPropertyName("operator")]
            public string Operator { get; set; } = "";
            [JsonProperty("value")]
            [System.Text.Json.Serialization.JsonPropertyName("value")]
            public string? Value { get; set; }
            [JsonProperty("value_template")]
            [System.Text.Json.Serialization.JsonPropertyName("value_template")]
            public string? ValueTemplate { get; set; }
            [JsonProperty("logical_operator")]
            [System.Text.Json.Serialization.JsonPropertyName("logical_operator")]
            public string? LogicalOperator { get; set; }
        }
    }

    #endregion
}
