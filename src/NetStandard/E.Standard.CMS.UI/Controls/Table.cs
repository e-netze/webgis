using E.Standard.CMS.Core.UI.Abstraction;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.CMS.UI.Controls;

public class Table : Control
{
    public Table(string name = "")
        : base(name)
    {
        this.Rows = new RowCollection(this);
    }

    //[JsonProperty(PropertyName = "rows")]
    [System.Text.Json.Serialization.JsonPropertyName("rows")]
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public RowCollection Rows { get; private set; }

    [JsonProperty(PropertyName = "headers")]
    [System.Text.Json.Serialization.JsonPropertyName("headers")]
    public IEnumerable<string> Headers { get; set; }

    [JsonProperty(PropertyName = "columnWidths")]
    [System.Text.Json.Serialization.JsonPropertyName("columnWidths")]
    public IEnumerable<string> ColumnWidths { get; set; }

    [JsonProperty(PropertyName = "controls")]
    [System.Text.Json.Serialization.JsonPropertyName("controls")]
    override public IEnumerable<IUIControl> ChildControls
    {
        get
        {
            return Rows.ToArray();
        }
    }

    #region Classes

    public class Row : Control
    {
        public Row(string name = "") : base(name)
        {
            Cells = new CellCollection(this);
        }

        //[JsonProperty(PropertyName = "cells")]
        [System.Text.Json.Serialization.JsonPropertyName("cells")]
        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public CellCollection Cells { get; private set; }

        [JsonProperty(PropertyName = "controls")]
        [System.Text.Json.Serialization.JsonPropertyName("controls")]
        override public IEnumerable<IUIControl> ChildControls
        {
            get
            {
                return Cells.ToArray();
            }
        }
    }

    public class RowCollection : List<Row>
    {
        private Table _table;

        public RowCollection(Table table)
        {
            _table = table;
        }

        new public void Add(Row row)
        {
            base.Add(row);
        }

        new public void AddRange(IEnumerable<Row> rows)
        {
            base.AddRange(rows);
        }
    }

    public class Cell : Control
    {
        public Cell(string name = "") : base(name)
        {
        }
        public Cell(IUIControl control, string name = "") : base(name)
        {
            this.AddControl(control);
        }

        public Cell(params IUIControl[] controls) : base("")
        {
            if (controls != null)
            {
                foreach (var control in controls)
                {
                    this.AddControl(control);
                }
            }
        }
    }

    public class CellCollection : List<Cell>
    {
        private Row _row;

        public CellCollection(Row row)
        {
            _row = row;
        }

        new public void Add(Cell cell)
        {
            // isDirty?
            base.Add(cell);
        }
    }

    #endregion
}
