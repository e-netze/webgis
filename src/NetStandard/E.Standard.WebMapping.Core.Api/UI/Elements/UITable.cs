using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UITable : UIElement
{
    public UITable()
        : base("table")
    {
        this.InsertTypeValue = TableInsertType.Normal;
    }
    public UITable(UITableRow[] rows)
        : this()
    {
        this.elements = rows;
    }

    public UITable(UITableRow row)
        : this()
    {
        if (row != null)
        {
            this.elements = new UITableRow[] { row };
        }
    }

    public void AddRow(UITableRow row)
    {
        List<IUIElement> rows = this.elements == null ? new List<IUIElement>() : new List<IUIElement>(this.elements);
        rows.Add(row);
        this.elements = rows.ToArray();
    }

    #region Properties

    public enum TableInsertType
    {
        Normal,
        Append,
        Replace
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public TableInsertType InsertTypeValue { get; set; }

    [JsonProperty("insert_type", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("insert_type")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string InsertType
    {
        get
        {
            if (this.InsertTypeValue == TableInsertType.Normal)
            {
                return null;
            }

            return this.InsertTypeValue.ToString().ToLower();
        }
    }

    #endregion
}
