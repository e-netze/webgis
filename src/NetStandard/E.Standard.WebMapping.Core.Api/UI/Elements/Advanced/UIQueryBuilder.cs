#nullable enable

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIQueryBuilder : UICollapsableElement
{
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

    [JsonProperty("whereclause_parts", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("whereclause_parts")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[]? WhereClauseParts { get; private set; }

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
                    break;
                case FieldType.Interger:
                case FieldType.BigInteger:
                case FieldType.SmallInteger:
                case FieldType.Float:
                case FieldType.Double:
                    Operators = new[] { "=", " in ", "<>", ">", ">=", "<", "<=" };
                    ValueTemplate = "{0}";
                    break;
                case FieldType.Boolean:
                    Operators = new[] { "=", "<>" };
                    ValueTemplate = "{0}"; // ???
                    break;
                default:
                    Operators = new[] { "=" };
                    ValueTemplate = "'{0}'";
                    break;
            }
        }

        [JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("alias", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("alias")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Aliasname { get; set;  }

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
