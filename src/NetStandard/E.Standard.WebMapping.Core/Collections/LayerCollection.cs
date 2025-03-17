using E.Standard.ThreadSafe;
using E.Standard.WebMapping.Core.Abstraction;
using System;

namespace E.Standard.WebMapping.Core.Collections;

public class LayerCollection : ThreadSafeList<ILayer>
{
    private readonly IMapService _service;

    private LayerCollection() { }

    public LayerCollection(IMapService service)
    {
        _service = service;
    }
    public LayerCollection(LayerCollection layers)
    {
        _service = layers._service;
        this.AddRange(layers);
    }

    public ILayer FindById(string id)
    {
        if (String.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (ILayer layer in this)
        {
            if (layer.GlobalID == id)
            {
                return layer;
            }
        }

        return null;
    }
    public ILayer FindByLayerId(string id)
    {
        if (String.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (ILayer layer in this)
        {
            if (layer.ID == id)
            {
                return layer;
            }
        }

        return null;
    }
    public ILayer FindByName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        foreach (ILayer layer in this)
        {
            if (layer.Name == name)
            {
                return layer;
            }
        }

        if (_service is IMapServiceFuzzyLayerNames && !String.IsNullOrEmpty(((IMapServiceFuzzyLayerNames)_service).FuzzyLayerNameSeperator))
        {
            string seperator = ((IMapServiceFuzzyLayerNames)_service).FuzzyLayerNameSeperator;
            foreach (ILayer layer in this)
            {
                if (ShortLayerName(layer.Name, seperator) == ShortLayerName(name, seperator))
                {
                    return layer;
                }
            }
        }

        return null;
    }

    private string ShortLayerName(string layername, string seperator)
    {
        if (!String.IsNullOrWhiteSpace(seperator) && layername.Contains(seperator))
        {
            int pos = layername.LastIndexOf(seperator);
            return layername.Substring(pos + seperator.Length, layername.Length - pos - seperator.Length);
        }

        return layername;
    }

    public bool IsEmpty
    {
        get
        {
            return base.Count == 0;
        }
    }

    static public LayerCollection Emtpy = new LayerCollection();
}
