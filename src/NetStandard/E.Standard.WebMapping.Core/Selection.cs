using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Filters;
using gView.GraphicsEngine;
using System;

namespace E.Standard.WebMapping.Core;

public class Selection : Dependency, IClone<Selection, IMap>
{
    private string _name = String.Empty;
    private ILayer _layer;
    private QueryFilter _filter;
    private ArgbColor _color;
    private ArgbColor _fillColor;
    private bool _isDirty = true;
    private bool _drawSpatialFilter = false;

    public Selection(ArgbColor color, ArgbColor? fillColor, string name, ILayer layer, QueryFilter query)
    {
        _color = color;
        _fillColor = fillColor ?? ArgbColor.FromArgb(color.A / 3, color);
        _name = name;
        _layer = layer;
        _filter = query;
    }

    public Selection(ArgbColor color, ArgbColor? fillColor, string name)
       : this(color, fillColor, name, null, null)
    {
    }

    public Selection(ArgbColor color, ArgbColor? fillColor, string name, ILayer layer, QueryFilter query, bool drawSpatialFilter)
        : this(color, fillColor, name, layer, query)
    {
        _drawSpatialFilter = drawSpatialFilter;
    }

    public ArgbColor Color
    {
        get { return _color; }
    }

    public ArgbColor FillColor
    {
        get { return _fillColor; }
    }

    public string Name
    {
        get { return _name; }
    }

    public ILayer Layer
    {
        get { return _layer; }
        set { _layer = value; }
    }

    public QueryFilter Filter
    {
        get { return _filter; }
        set { _filter = value; }
    }

    public bool IsDirty
    {
        get { return _isDirty; }
        set { _isDirty = value; }
    }

    public bool DrawSpatialFilter
    {
        get { return _drawSpatialFilter; }
        set { _drawSpatialFilter = value; }
    }

    #region IClone Member

    public Selection Clone(IMap parent)
    {
        if (parent is null)
        {
            return null;
        }

        Selection clone = null;
        if (this is BufferSelection)
        {
            clone = new BufferSelection(_color, _fillColor, _name);
        }
        else
        {
            clone = new Selection(_color, _fillColor, _name);
        }

        clone._drawSpatialFilter = _drawSpatialFilter;

        if (_layer != null)
        {
            clone._layer = parent.LayerById(_layer.GlobalID);
        }

        if (_filter != null)
        {
            clone._filter = _filter.Clone();
        }

        foreach (string dependency in _dependencies)
        {
            clone._dependencies.Add(dependency);
        }

        return clone;
    }

    #endregion
}

public class BufferSelection : Selection
{
    public BufferSelection(ArgbColor color, ArgbColor? fillColor, string name)
        : base(color, fillColor, name)
    {
    }
    public BufferSelection(ArgbColor color, ArgbColor? fillColor, string name, ILayer layer, QueryFilter query)
        : base(color, fillColor, name, layer, query)
    {
    }
}
