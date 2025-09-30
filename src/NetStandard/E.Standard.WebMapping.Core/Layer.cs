using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Filters;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.WebMapping.Core;

public abstract class Layer : ILayer
{
    private readonly string _name;
    private string _id;
    protected IMapService _service;
    private readonly LayerType _type;
    private FieldCollection _fields;
    private bool _visible = true;
    private double _minScale = 0, _maxScale = 0;
    private string _filter;
    private string _idFieldName = String.Empty;
    private string _shapeFieldName = String.Empty;

    public Layer(string name, string id, IMapService service, bool queryable)
    {
        _name = name;
        _id = id;
        _service = service;

        _fields = new FieldCollection();
        _filter = String.Empty;

        this.Queryable = queryable;
    }
    public Layer(string name, string id, LayerType type, IMapService service, bool queryable)
        : this(name, id, service, queryable)
    {
        _type = type;
    }

    #region ILayer Member

    public string Name
    {
        get { return _name; }
    }

    public string ID
    {
        get { return _id; }
    }

    public string GlobalID
    {
        get
        {
            if (_service != null)
            {
                return _service.ID + ":" + _id;
            }

            return String.Empty;
        }
    }

    public LayerType Type
    {
        get { return _type; }
    }

    public bool Visible
    {
        get
        {
            return _visible;
        }
        set
        {
            _visible = value;
        }
    }

    public bool Queryable { get; private set; }

    public LayerTimeInfo TimeInfo { get; set; }

    public double MinScale
    {
        get
        {
            return _minScale;
        }
        set { _minScale = value; }
    }

    public double MaxScale
    {
        get
        {
            return _maxScale;
        }
        set { _maxScale = value; }
    }

    public FieldCollection Fields
    {
        get { return _fields; }
    }

    public string Filter
    {
        get { return _filter; }
        set { _filter = value; }
    }

    public string Description { get; set; }

    public abstract Task<bool> GetFeaturesAsync(QueryFilter query, FeatureCollection result, IRequestContext requestContext);

    virtual public string IdFieldName
    {
        get
        {
            if (String.IsNullOrEmpty(_idFieldName))
            {
                _idFieldName = GetFieldNameByType(FieldType.ID);
            }

            return _idFieldName;
        }
    }

    public string ShapeFieldName
    {
        get
        {
            if (String.IsNullOrEmpty(_shapeFieldName))
            {
                _shapeFieldName = GetFieldNameByType(FieldType.Shape);
            }

            return _shapeFieldName;
        }
    }

    public IMapService Service { get { return _service; } }

    #endregion

    internal IMapService _Service
    {
        get { return _service; }
        set { _service = value; }
    }

    private string GetFieldNameByType(FieldType type)
    {
        foreach (IField field in _fields)
        {
            if (field == null)
            {
                continue;
            }

            if (field.Type == type)
            {
                return field.Name;
            }
        }
        return String.Empty;
    }

    #region IClone Member

    abstract public ILayer Clone(IMapService parent);

    #endregion

    protected void ClonePropertiesFrom(ILayer layer)
    {
        if (layer == null)
        {
            return;
        }

        this.Filter = layer.Filter;
        this.Visible = layer.Visible;
        this.MaxScale = layer.MaxScale;
        this.MinScale = layer.MinScale;
        this._idFieldName = layer.IdFieldName;
        this._shapeFieldName = layer.ShapeFieldName;
        this._fields = layer.Fields;

        this.Description = layer.Description;
        this.Queryable = layer.Queryable;
        this.TimeInfo = layer.TimeInfo;
    }
    public static IMapService ServiceByLayer(IMap map, ILayer layer)
    {
        if (map == null || layer == null)
        {
            return null;
        }

        foreach (IMapService service in map.Services)
        {
            if (service.Layers.FindById(layer.GlobalID) != null)
            {
                return service;
            }
        }
        return null;
    }

    protected string IdFieldNameSetter
    {
        set { _idFieldName = value; }
    }
    protected string ShapeFieldNameSetter
    {
        set { _shapeFieldName = value; }
    }

    protected string IDSetter
    {
        set { _id = value; }
    }

    public void ChangeFieldType(Field field, FieldType type)
    {
        if (field != null && _fields.Contains(field))
        {
            field.ChangeFieldType(type);
            if (type == FieldType.ID)
            {
                _idFieldName = field.Name;
            }
        }
    }

    #region Static Members

    static public void TrySetIdField(IMap map, IMapService service, Layer layer)
    {
        try
        {
            string idFieldName = String.Empty;

            FileInfo fi = new FileInfo(map.Environment.UserString("EtcPath") + @"/ims/ims_metainfo.xml");
            if (fi.Exists)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fi.FullName);
                XmlNode idfield = doc.SelectSingleNode("ims_metainfo/services/service[@name='" + service.Service + "']/layer[@id='" + layer.ID + "']/idfield[@name]");
                if (idfield == null)
                {
                    idfield = doc.SelectSingleNode("ims_metainfo/services/service[@name='" + service.Service + "']/layer[@name='" + layer.Name + "']/idfield[@name]");
                }

                if (!String.IsNullOrWhiteSpace(service.ServiceShortname))
                {
                    if (idfield == null)
                    {
                        idfield = doc.SelectSingleNode("ims_metainfo/services/service[@name='" + service.ServiceShortname + "']/layer[@id='" + layer.ID + "']/idfield[@name]");
                    }

                    if (idfield == null)
                    {
                        idfield = doc.SelectSingleNode("ims_metainfo/services/service[@name='" + service.ServiceShortname + "']/layer[@name='" + layer.Name + "']/idfield[@name]");
                    }
                }
                if (idfield != null)
                {
                    idFieldName = (idfield.Attributes["name"].Value);
                }
                else
                {
                    var servicesNode = doc.SelectSingleNode("ims_metainfo/services[@default]");
                    if (servicesNode != null)
                    {
                        idFieldName = servicesNode.Attributes["default"].Value;
                    }
                }
                if (!String.IsNullOrWhiteSpace(idFieldName))
                {
                    Field field = layer.Fields.FindField(idFieldName) as Field;
                    if (field != null)
                    {
                        layer.ChangeFieldType(field, FieldType.ID);
                    }
                }
            }
        }
        catch { }
    }

    #endregion
}
