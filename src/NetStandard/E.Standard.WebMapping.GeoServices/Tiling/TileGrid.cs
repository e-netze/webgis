using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.Tiling;

public class TileGrid
{
    private List<TileGridLevel> _levels = new List<TileGridLevel>();
    private TileGridRendering _rendering = TileGridRendering.Quality;

    public TileGrid(Point origin, int tileSizeX, int tileSizeY, double dpi, TileGridOrientation orientation)
    {
        _origin = origin;
        _tileSizeX = tileSizeX;
        _tileSizeY = tileSizeY;
        _dpi = dpi;
        _orientation = orientation;
    }

    #region Properties

    private Point _origin;
    public Point Origin
    {
        get { return _origin; }
    }

    private double _dpi;
    public double Dpi
    {
        get { return _dpi; }
    }

    private TileGridOrientation _orientation;
    public TileGridOrientation Orientation
    {
        get { return _orientation; }
    }

    private int _tileSizeX;
    public int TileSizeX
    {
        get { return _tileSizeX; }
    }

    private int _tileSizeY;
    public int TileSizeY
    {
        get { return _tileSizeY; }
    }

    public int MinResolutionLevel
    {
        get
        {
            if (_levels.Count == 0)
            {
                return -1;
            }

            double min = _levels[0].Resolution;
            int l = _levels[0].Level;

            foreach (TileGridLevel level in _levels)
            {
                if (level.Resolution < min)
                {
                    l = level.Level;
                    min = level.Resolution;
                }
            }

            return l;
        }
    }

    public int MaxResolutionLevel
    {
        get
        {
            if (_levels.Count == 0)
            {
                return -1;
            }

            double max = _levels[0].Resolution;
            int l = _levels[0].Level;

            foreach (TileGridLevel level in _levels)
            {
                if (level.Resolution > max)
                {
                    l = level.Level;
                    max = level.Resolution;
                }
            }

            return l;
        }
    }

    public TileGridRendering GridRendering
    {
        get { return _rendering; }
        set { _rendering = value; }
    }

    public double[,] Resolutions
    {
        get
        {
            double[,] res = new double[_levels.Count, 2];
            for (int i = 0; i < _levels.Count; i++)
            {
                res[i, 0] = _levels[i].Level;
                res[i, 1] = _levels[i].Resolution;
            }

            return res;
        }
    }

    public double[] GridResolutions
    {
        get
        {
            double[] res = new double[_levels.Count];
            for (int i = 0; i < _levels.Count; i++)
            {
                res[i] = _levels[i].Resolution;
            }

            return res;
        }
    }

    public Envelope Extent
    {
        get
        {
            if (_levels == null || _levels.Count == 0)
            {
                return null;
            }

            double width = TileWidth(_levels[0].Resolution);
            double height = TileHeight(_levels[0].Resolution);

            if (_orientation == TileGridOrientation.LowerLeft)
            {
                return new Envelope(_origin.X, _origin.Y, _origin.X + width, _origin.Y + height);
            }

            return new Envelope(_origin.X, _origin.Y - height, _origin.X + width, _origin.Y);
        }
    }
    #endregion

    #region Members

    #region Level Members
    public void AddLevel(int level, double resolution)
    {
        _levels.Add(new TileGridLevel(level, resolution));
    }
    public void AddLevel(Point origin, int level, double resolution)
    {
        _levels.Add(new TileGridLevel(origin, level, resolution));
    }

    public int GetBestLevel(double mapResolution, double dpi)
    {
        if (_levels.Count == 0)
        {
            return -1;
        }

        double res = double.MinValue, dpm = dpi / 0.0254, mapScale = mapResolution * dpm;
        int l = -1;

        bool onScale = false;
        foreach (TileGridLevel level in _levels)
        {
            double levelScale = level.Resolution * dpm;
            double scaleComp = Math.Round(levelScale / mapScale, 5);

            if ((level.Resolution < mapResolution || scaleComp == 1.0) &&
                level.Resolution > res)
            {
                res = level.Resolution;
                l = level.Level;

                onScale = scaleComp == 1.0;
            }
        }

        // Wenn Maßstab nicht exakt getroffen wurde, muss bei Readability der nächst größere genommen werden!
        if (_rendering == TileGridRendering.Readability && onScale == false && l > 0)
        {
            l = Math.Max(l - 1, 0);
        }

        return (l >= 0) ? l : this.MinResolutionLevel;
    }

    public int GetNextLowerLevel(int level)
    {
        if (level < 0)
        {
            return -1;
        }

        int ret = -1;
        double levelRes = GetLevelResolution(level), res = double.MaxValue;
        foreach (TileGridLevel l in _levels)
        {
            if (l.Resolution > levelRes && levelRes < res)
            {
                ret = l.Level;
                res = l.Resolution;
            }
        }

        return ret;
    }

    public double GetLevelResolution(int level)
    {
        foreach (TileGridLevel l in _levels)
        {
            if (l.Level == level)
            {
                return l.Resolution;
            }
        }

        return double.MinValue;
    }
    #endregion

    #region Tile Members
    public double TileWidth(double res)
    {
        return _tileSizeX * res;
    }
    public double TileHeight(double res)
    {
        return _tileSizeY * res;
    }

    public int TileColumn(double x, double res)
    {
        return (int)Math.Floor((x - _origin.X) / TileWidth(res));
    }
    public int TileRow(double y, double res)
    {

        return (int)Math.Floor((y - _origin.Y) * (_orientation == TileGridOrientation.UpperLeft ? -1.0 : 1.0) / TileWidth(res));
    }

    public Point TileUpperLeft(int row, int col, double res)
    {
        double x = _origin.X + col * TileWidth(res);
        double y = 0.0;
        if (_orientation == TileGridOrientation.UpperLeft)
        {
            y = _origin.Y - row * TileHeight(res);
        }
        else
        {
            y = _origin.Y + (row + 1) * TileHeight(res);
        }

        return new Point(x, y);
    }
    #endregion

    #endregion

    #region Helperclasses
    private class TileGridLevel
    {
        public TileGridLevel(int level, double resolution)
        {
            _level = level;
            _resolution = resolution;
        }
        public TileGridLevel(Point origin, int level, double resolution)
            : this(level, resolution)
        {
            _origin = origin;
        }

        #region Properties
        private int _level;
        public int Level
        {
            get { return _level; }
            //set { _level = value; }
        }

        private double _resolution;
        public double Resolution
        {
            get { return _resolution; }
            //set { _resolution = value; }
        }

        private Point _origin = null;
        public Point Origin
        {
            get { return _origin; }
        }
        #endregion
    }
    #endregion

    #region Static Members
    public static int GetZoomOffset2D(double[,] mapResolutions, double[,] tileResolutions)
    {

        int mapResolutionsCount = mapResolutions.Length / 2;
        var largestTileResolution = tileResolutions[0, 1];

        int tileResolutionsCount = tileResolutions.Length / 2;
        var largestMapResolution = mapResolutions[0, 1];

        double[] mapResolutionsSingleDimensionalArray = new double[mapResolutionsCount];
        for (int mapResolutionIndex = 0; mapResolutionIndex < mapResolutionsCount; mapResolutionIndex++)
        {
            mapResolutionsSingleDimensionalArray[mapResolutionIndex] = mapResolutions[mapResolutionIndex, 1];
        }

        double[] tilesResolutionsSingleDimensionalArray = new double[tileResolutionsCount];
        for (int tileResolutionIndex = 0; tileResolutionIndex < tileResolutionsCount; tileResolutionIndex++)
        {
            tilesResolutionsSingleDimensionalArray[tileResolutionIndex] = tileResolutions[tileResolutionIndex, 1];
        }

        return GetZoomOffset(mapResolutionsSingleDimensionalArray, tilesResolutionsSingleDimensionalArray);
    }
    public static int GetZoomOffset(double[] mapResolutions, double[] tileResolutions)
    {
        int zoomOffset = 0;

        // ZOOMOFFSET GENERATION
        int mapResolutionsCount = mapResolutions.Length;
        var largestTileResolution = tileResolutions[0];

        int tileResolutionsCount = tileResolutions.Length;
        var largestMapResolution = mapResolutions[0];

        string userInfoMessage = "tilecache-resolutions don't match with map-resolutions";

        if (largestMapResolution >= largestTileResolution)
        {
            for (zoomOffset = 0; zoomOffset < mapResolutionsCount; zoomOffset++)
            {
                if (mapResolutions[zoomOffset] == largestTileResolution)
                {
                    break;
                }
            }
        }
        else
        {
            for (int tileResolutionsIndex = 0; tileResolutionsIndex < tileResolutionsCount; tileResolutionsIndex++)
            {
                if (tileResolutions[tileResolutionsIndex] == largestMapResolution)
                {
                    break;
                }

                zoomOffset--;
            }
        }

        // VALIDATION
        int comparisonIndex = 0;
        if (zoomOffset >= 0)
        {
            for (int mapResolutionsIndex = 0; mapResolutionsIndex < mapResolutionsCount; mapResolutionsIndex++)
            {
                comparisonIndex = mapResolutionsIndex + zoomOffset;
                if ((tileResolutionsCount > comparisonIndex) && (mapResolutionsCount > comparisonIndex))
                {
                    if (RoundDoubleValueCommercially(mapResolutions[comparisonIndex]) != RoundDoubleValueCommercially(tileResolutions[mapResolutionsIndex]))
                    {
                        throw new Exception(userInfoMessage);
                    }
                }
                else
                {
                    break; // NO MORE RESOLUTIONS AVAILABLE
                }
            }
        }
        else
        {
            for (int mapResolutionsIndex = 0; mapResolutionsIndex < mapResolutionsCount; mapResolutionsIndex++)
            {
                comparisonIndex = mapResolutionsIndex + (zoomOffset * (-1));
                if ((tileResolutionsCount > comparisonIndex) && (mapResolutionsCount > comparisonIndex))
                {
                    if (mapResolutions[mapResolutionsIndex] != tileResolutions[comparisonIndex])
                    {
                        throw new Exception(userInfoMessage);
                    }
                }
                else
                {
                    break; // NO MORE RESOLUTIONS AVAILABLE
                }
            }
        }

        return zoomOffset;
    }
    public static double RoundDoubleValueCommercially(double inDoubleValue)
    {   // rounding only works right with double-input
        return System.Math.Round(inDoubleValue, 8, MidpointRounding.AwayFromZero);
    }
    #endregion
}

