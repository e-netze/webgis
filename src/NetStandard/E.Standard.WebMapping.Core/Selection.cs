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
    private bool _isDirty = true;
    private bool _drawSpatialFilter = false;

    public Selection(ArgbColor color, string name)
    {
        _color = color;
        _name = name;
        _layer = null;
        _filter = null;
    }
    public Selection(ArgbColor color, string name, ILayer layer, QueryFilter query)
    {
        _color = color;
        _name = name;
        _layer = layer;
        _filter = query;
    }
    public Selection(ArgbColor color, string name, ILayer layer, QueryFilter query, bool drawSpatialFilter)
        : this(color, name, layer, query)
    {
        _drawSpatialFilter = drawSpatialFilter;
    }

    public ArgbColor Color
    {
        get { return _color; }
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
            clone = new BufferSelection(_color, _name);
        }
        else
        {
            clone = new Selection(_color, _name);
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
    public BufferSelection(ArgbColor color, string name)
        : base(color, name)
    {
    }
    public BufferSelection(ArgbColor color, string name, ILayer layer, QueryFilter query)
        : base(color, name, layer, query)
    {
    }
}
