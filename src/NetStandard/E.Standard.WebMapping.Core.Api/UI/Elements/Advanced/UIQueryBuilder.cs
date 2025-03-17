using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

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
        _fieldDefs.OrderBy(x => x.Name);

    [JsonProperty("show_geometry_option")]
    [System.Text.Json.Serialization.JsonPropertyName("show_geometry_option")]
    public bool ShowGeometryOption { get; set; }

    public void TryAddField(string fieldName, FieldType fieldType = FieldType.Unknown)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            return;
        }

        var fieldDef = _fieldDefs.Where(f => fieldName.Equals(f.Name)).FirstOrDefault();
        if (fieldDef == null)
        {
            fieldDef = new FieldDef(fieldName, fieldType);
            _fieldDefs.Add(fieldDef);
        }
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
                    Operators = new[] { "=", " like ", "<>", ">", ">=", "<", "<=" };
                    ValueTemplate = "'{0}'";
                    break;
                case FieldType.Interger:
                case FieldType.BigInteger:
                case FieldType.SmallInteger:
                case FieldType.Float:
                case FieldType.Double:
                    Operators = new[] { "=", "<>", ">", ">=", "<", "<=" };
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
        public string GeometryOption { get; set; }

        [JsonProperty("query_defs")]
        [System.Text.Json.Serialization.JsonPropertyName("query_defs")]
        public IEnumerable<QueryDef> QueryDefs { get; set; }

        public class QueryDef
        {
            [JsonProperty("field")]
            [System.Text.Json.Serialization.JsonPropertyName("field")]
            public string Field { get; set; }
            [JsonProperty("operator")]
            [System.Text.Json.Serialization.JsonPropertyName("operator")]
            public string Operator { get; set; }
            [JsonProperty("value")]
            [System.Text.Json.Serialization.JsonPropertyName("value")]
            public string Value { get; set; }
            [JsonProperty("value_template")]
            [System.Text.Json.Serialization.JsonPropertyName("value_template")]
            public string ValueTemplate { get; set; }
            [JsonProperty("logical_operator")]
            [System.Text.Json.Serialization.JsonPropertyName("logical_operator")]
            public string LogicalOperator { get; set; }
        }
    }

    #endregion
}
