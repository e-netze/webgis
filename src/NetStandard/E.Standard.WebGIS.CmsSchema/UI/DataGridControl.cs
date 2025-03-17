//using E.Standard.CMS.UI.Controls;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Text;

//namespace E.Standard.WebGIS.CmsSchema.UI
//{
//    public class DataGridControl : Control
//    {
//        public DataGridControl(string name) : base(name) { }

//        [JsonIgnore]
//        [System.Text.Json.Serialization.JsonIgnore]
//        public DataTable DataSource
//        {
//            set
//            {
//                List<Column> columns = new List<Column>();
//                List<Row> rows = new List<Row>();

//                foreach(DataColumn dataColumn in value.Columns)
//                {
//                    columns.Add(new Column()
//                    {
//                        Name = dataColumn.ColumnName
//                    });

//                    int columnCount = columns.Count;
//                    foreach(DataRow row in value.Rows)
//                    {
//                        List<object> cells = new List<object>();
//                        foreach (DataColumn dc in value.Columns)
//                        {
//                            cells.Add(row[dc]);
//                        }
//                        rows.Add(new Row()
//                        {
//                            Cells = cells.ToArray()
//                        });
//                    }

//                    this.Columns = columns.ToArray();
//                    this.Rows = rows.ToArray();
//                }
//            }
//        }

//        [JsonProperty(PropertyName = "columns")]
//        [System.Text.Json.Serialization.JsonPropertyName("columns")]
//        public Column[] Columns { get; private set;  }

//        [JsonProperty(PropertyName = "rows")]
//        [System.Text.Json.Serialization.JsonPropertyName("rows")]
//        public Row[] Rows { get; private set;  }

//        #region Classes

//        public class Column
//        {
//            [JsonProperty(PropertyName = "name")]
//            [System.Text.Json.Serialization.JsonPropertyName("name")]
//            public string Name { get; set; }
//            [JsonProperty(PropertyName = "width")]
//            [System.Text.Json.Serialization.JsonPropertyName("width")]
//            public int Width { get; set; }
//        }

//        public class Row
//        {
//            [JsonProperty(PropertyName = "cells")]
//            [System.Text.Json.Serialization.JsonPropertyName("cells")]
//            public object[] Cells { get; set; }
//        }

//        #endregion
//    }
//}
