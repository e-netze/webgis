using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.Drawing;

public class ChartBridge
{
    public ChartBridge()
    {
        this.Data = new ChartData();
        this.Point = new ChartPoint();
    }

    #region Properties

    public bool SketchConnected { get; set; }

    public ChartData Data { get; private set; }

    public ChartGrid Grid { get; private set; }

    public ChartAxis Axis { get; private set; }

    public ChartPoint Point { get; private set; }

    #endregion

    #region Members

    #region Grid

    public void AddGridXLine(ChartGridData.Line line)
    {
        if (Grid == null)
        {
            this.Grid = new ChartGrid();
        }

        this.Grid.AddXLine(line);
    }

    public void AddGridYLine(ChartGridData.Line line)
    {
        if (Grid == null)
        {
            this.Grid = new ChartGrid();
        }

        this.Grid.AddYLine(line);
    }

    #endregion

    #region Axis

    public void SetAxisX(double min, double max)
    {
        if (this.Axis == null)
        {
            this.Axis = new ChartAxis();
        }

        if (this.Axis.X == null)
        {
            this.Axis.X = new ChartAxisData();
        }

        this.Axis.X.Min = min;
        this.Axis.X.Max = max;
    }

    public void SetAxisY(double min, double max)
    {
        if (this.Axis == null)
        {
            this.Axis = new ChartAxis();
        }

        if (this.Axis.Y == null)
        {
            this.Axis.Y = new ChartAxisData();
        }

        this.Axis.Y.Min = min;
        this.Axis.Y.Max = max;
    }

    #endregion

    #endregion

    #region Classes

    public class ChartData
    {
        public List<double[]> XAxis { get; set; }
        //public List<double[]> Data { get; set; }
        public Dictionary<string, List<double[]>> DataDict { get; set; }
        public List<string> Types { get; set; }
        public List<string> Colors { get; set; }
    }

    public class ChartGrid
    {
        public ChartGridData X { get; private set; }
        public ChartGridData Y { get; private set; }

        public void AddXLine(ChartGridData.Line line)
        {
            if (X == null)
            {
                X = new ChartGridData();
            }

            X.AddLine(line);
        }

        public void AddYLine(ChartGridData.Line line)
        {
            if (Y == null)
            {
                Y = new ChartGridData();
            }

            Y.AddLine(line);
        }
    }

    public class ChartGridData
    {
        private List<Line> _lines = new List<Line>();
        public Line[] Lines { get { return _lines.ToArray(); } }

        public void AddLine(Line line)
        {
            _lines.Add(line);
        }

        public class Line
        {
            public double Value { get; set; }
            public string Text { get; set; }
        }
    }

    public class ChartAxis
    {
        public ChartAxisData X { get; set; }
        public ChartAxisData Y { get; set; }
    }

    public class ChartAxisData
    {
        public double Min { get; set; }
        public double Max { get; set; }
    }

    public class ChartPoint
    {
        public bool Show { get; set; } = true;
    }

    #endregion
}
