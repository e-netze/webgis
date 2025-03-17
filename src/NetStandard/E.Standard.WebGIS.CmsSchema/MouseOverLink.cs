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
    [DisplayName("Minimaler Maßstab 1:")]
    public double MinScale
    {
        get { return _minScale; }
        set { _minScale = value; }
    }

    [Browsable(true)]
    [DisplayName("Maximaler Maßstab 1:")]
    public double MaxScale
    {
        get { return _maxScale; }
        set { _maxScale = value; }
    }

    [Browsable(true)]
    [DisplayName("Mouseover anwenden, wenn")]
    [Description("Gibt an, ob Mouseover angezeigt wird, wenn das jeweilige Thema in der Karte sichtbar ist oder die Abfrage (falls in der Karte vorhanden) Abfragethema ist.")]
    public MouseOverMode Mode
    {
        get { return _mode; }
        set { _mode = value; }
    }

    [Browsable(true)]
    [DisplayName("Als Karten Tipp darstellen")]
    [Category("Karten Tipps")]
    public bool IsMapInfo
    {
        get { return _isMapInfo; }
        set { _isMapInfo = value; }
    }
    [Browsable(true)]
    [DisplayName("Symbol")]
    [Category("Karten Tipps")]
    [Editor(typeof(TypeEditor.GeoRssMarkerEditor),
        typeof(TypeEditor.ITypeEditor))]
    public string MapInfoSymbol
    {
        get { return _mapInfoSymbol; }
        set { _mapInfoSymbol = value; }
    }
    [Browsable(true)]
    [DisplayName("beim Start sichtbar")]
    [Category("Karten Tipps")]
    public bool MapInfoVisible
    {
        get { return _mapInfoVisible; }
        set { _mapInfoVisible = value; }
    }
    [Browsable(true)]
    [DisplayName("Mit dem Thema über TOC mitschalten")]
    [Category("Karten Tipps")]
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
