using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using System;
using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public enum MouseOverMode
{
    ThemeIsVisible = 0,
    QueryIsActive = 1
}

class MouseOverLink : SchemaNodeLink, IEditable
{
    private double _minScale = 0, _maxScale = 0;
    private MouseOverMode _mode = MouseOverMode.ThemeIsVisible;
    private bool _isMapInfo = false, _mapInfoVisible = true, _setVisWithTheme = false;
    private string _mapInfoSymbol = String.Empty;

    #region Properties
    [Browsable(true)]
    [DisplayName("#min_scale")]
    public double MinScale
    {
        get { return _minScale; }
        set { _minScale = value; }
    }

    [Browsable(true)]
    [DisplayName("#max_scale")]
    public double MaxScale
    {
        get { return _maxScale; }
        set { _maxScale = value; }
    }

    [Browsable(true)]
    [DisplayName("#mode")]
    public MouseOverMode Mode
    {
        get { return _mode; }
        set { _mode = value; }
    }

    [Browsable(true)]
    [DisplayName("#is_map_info")]
    [Category("#category_is_map_info")]
    public bool IsMapInfo
    {
        get { return _isMapInfo; }
        set { _isMapInfo = value; }
    }
    [Browsable(true)]
    [DisplayName("#map_info_symbol")]
    [Category("#category_map_info_symbol")]
    [Editor(typeof(TypeEditor.GeoRssMarkerEditor),
        typeof(TypeEditor.ITypeEditor))]
    public string MapInfoSymbol
    {
        get { return _mapInfoSymbol; }
        set { _mapInfoSymbol = value; }
    }
    [Browsable(true)]
    [DisplayName("#map_info_visible")]
    [Category("#category_map_info_visible")]
    public bool MapInfoVisible
    {
        get { return _mapInfoVisible; }
        set { _mapInfoVisible = value; }
    }
    [Browsable(true)]
    [DisplayName("#set_visible_with_theme")]
    [Category("#category_set_visible_with_theme")]
    public bool SetVisibleWithTheme
    {
        get { return _setVisWithTheme; }
        set { _setVisWithTheme = value; }
    }
    #endregion

    #region IPersistable Member

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _minScale = (double)stream.Load("minscale", 0.0);
        _maxScale = (double)stream.Load("maxscale", 0.0);
        _mode = (MouseOverMode)stream.Load("mode", (int)MouseOverMode.ThemeIsVisible);

        _isMapInfo = (bool)stream.Load("ismapinfo", false);
        _mapInfoSymbol = (string)stream.Load("mapinfosymbol", String.Empty);
        _mapInfoVisible = (bool)stream.Load("mapinfovisible", true);
        _setVisWithTheme = (bool)stream.Load("viswiththeme", false);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("minscale", _minScale);
        stream.Save("maxscale", _maxScale);
        stream.Save("mode", (int)_mode);

        stream.Save("ismapinfo", _isMapInfo);
        stream.Save("mapinfosymbol", _mapInfoSymbol);
        stream.Save("mapinfovisible", _mapInfoVisible);
        stream.Save("viswiththeme", _setVisWithTheme);
    }

    #endregion
}
