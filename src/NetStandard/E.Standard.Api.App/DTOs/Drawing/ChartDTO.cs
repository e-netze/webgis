using E.Standard.WebMapping.Core.Api.Drawing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace E.Standard.Api.App.DTOs.Drawing;

public sealed class ChartDTO
{
    public ChartDTO()
    {
        this.data = new ChartData();
        this.point = new ChartPoint();
    }

    public ChartDTO(ChartBridge chartBridge)
        : this()
    {
        #region Data

        this.data.xs = new ExpandoObject();
        IDictionary<string, object> xsDict = (IDictionary<string, object>)this.data.xs;

        this.data.types = new ExpandoObject();
        IDictionary<string, object> typesDict = (IDictionary<string, object>)this.data.types;

        this.data.colors = new ExpandoObject();
        IDictionary<string, object> colorsDict = (IDictionary<string, object>)this.data.colors;

        //if (chartBridge.Data.XAxis.Count != chartBridge.Data.Data.Count)
        if (chartBridge.Data.XAxis.Count != chartBridge.Data.DataDict.FirstOrDefault().Value.Count)
        {
            throw new ArgumentException("Invalid Chart");
        }

        List<object> cols = new List<object>();
        for (int i = 0; i < chartBridge.Data.XAxis.Count; i++)
        {
            cols.Add(ToObjectArray("x" + (i + 1), chartBridge.Data.XAxis[i]));

            foreach (var item in chartBridge.Data.DataDict.Select((Entry, Index) => new { Entry, Index }))
            {
                xsDict.Add(item.Entry.Key, "x" + (i + 1));
                if (chartBridge.Data.Types != null && chartBridge.Data.Types.Count > i)
                {
                    typesDict.Add(item.Entry.Key, chartBridge.Data.Types[i]);
                }

                if (chartBridge.Data.Colors != null && chartBridge.Data.Colors.Count > i)
                {
                    //colorsDict.Add(item.Key, chartBridge.Data.Colors[i]);
                    colorsDict.Add(item.Entry.Key, chartBridge.Data.Colors[item.Index]);
                }

                cols.Add(ToObjectArray(item.Entry.Key, item.Entry.Value[0]));
            }
        }

        this.data.columns = cols.ToArray();
        this.point.show = chartBridge.Point.Show;

        #endregion

        #region Grid

        if (chartBridge.Grid != null)
        {
            this.grid = new ChartGrid();
            if (chartBridge.Grid.X != null)
            {
                this.grid.x = new ChartGridData();
                foreach (var l in chartBridge.Grid.X.Lines)
                {
                    this.grid.x.AddLine(new ChartGridData.Line()
                    {
                        value = l.Value,
                        text = l.Text
                    });
                }
            }
            if (chartBridge.Grid.Y != null)
            {
                this.grid.y = new ChartGridData();
                foreach (var l in chartBridge.Grid.Y.Lines)
                {
                    this.grid.y.AddLine(new ChartGridData.Line()
                    {
                        value = l.Value,
                        text = l.Text
                    });
                }
            }
        }

        #endregion

        #region Axis

        if (chartBridge.Axis != null)
        {
            this.axis = new ChartAxis();
            if (chartBridge.Axis.X != null)
            {
                this.axis.x = new ChartAxisData()
                {
                    min = chartBridge.Axis.X.Min > 0 ? chartBridge.Axis.X.Min : null,
                    max = chartBridge.Axis.X.Max > 0 ? chartBridge.Axis.Y.Max : null,
                };
            }
            if (chartBridge.Axis.Y != null)
            {
                this.axis.y = new ChartAxisData()
                {
                    min = chartBridge.Axis.Y.Min > 0 ? chartBridge.Axis.Y.Min : null,
                    max = chartBridge.Axis.Y.Max > 0 ? chartBridge.Axis.Y.Max : null,
                };
            }
        }

        #endregion

        this.sketchconnected = chartBridge.SketchConnected;
    }

    public bool sketchconnected { get; set; }

    public ChartData data { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ChartGrid grid { get; set; }

    public ChartAxis axis { get; set; }

    public ChartPoint point { get; set; }

    #region Helper

    public object[] ToObjectArray(object firstElement, double[] doubleArray)
    {
        List<object> objectArray = new List<object>();

        if (firstElement != null)
        {
            objectArray.Add(firstElement);
        }

        foreach (double d in doubleArray)
        {
            objectArray.Add(d);
        }

        return objectArray.ToArray();
    }

    #endregion

    #region Classes

    public class ChartData
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public object xs { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public object types { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public object colors { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public object[] columns { get; set; }


    }

    public class ChartGrid
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public ChartGridData x { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public ChartGridData y { get; set; }
    }

    public class ChartGridData
    {
        private List<Line> _lines = new List<Line>();

        public void AddLine(Line line)
        {
            _lines.Add(line);
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public Line[] lines { get { return _lines.ToArray(); } }

        public class Line
        {
            public double value { get; set; }
            public string text { get; set; }
        }
    }

    public class ChartAxis
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public ChartAxisData x { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public ChartAxisData y { get; set; }
    }

    public class ChartAxisData
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double? min { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double? max { get; set; }
    }

    public class ChartPoint
    {
        public bool show { get; set; }
    }

    #endregion
}