using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.WebGIS.CMS;
using System;
using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public class QueryLink : SchemaNodeLink, IEditable
{
    private FeatureTableType _featureTableType = FeatureTableType.Default;
    private bool _geoJuhu = false;
    private string _geoJuhuSchema = String.Empty;
    private string _filterUrl = String.Empty;

    #region Properties
    [DisplayName("Suchergebnis Darstellung")]
    [Category("Darstellung")]
    public FeatureTableType FeatureTableType
    {
        get { return _featureTableType; }
        set { _featureTableType = value; }
    }

    [DisplayName("Abfrage nimmt an GeoJuhu teil")]
    [Category("GeoJuhu")]
    [Editor(typeof(TypeEditor.RegExTypeEditor), typeof(TypeEditor.ITypeEditor))]
    public bool GeoJuhu
    {
        get { return _geoJuhu; }
        set { _geoJuhu = value; }
    }

    [DisplayName("GeoJuhu Schema")]
    [Category("GeoJuhu")]
    [Description("Hier können mehrere Schematas mit Beistrich getrennt eingeben werden. Der Wert wird nur berücksichtigt, wenn ein GeoJuhu Schema in der Aufruf-Url übergeben wird. * (Stern) kann angeben werden, wenn eine Thema in jedem Schema abgefragt werden soll.")]
    public string GeoJuhuSchema
    {
        get { return _geoJuhuSchema; }
        set { _geoJuhuSchema = value; }
    }

    [DisplayName("Filter Url")]
    [Category("Filter")]
    [Editor(typeof(TypeEditor.SelectMapVisFiler), typeof(TypeEditor.ITypeEditor))]
    [Description("Eine Abfrage kann mit einem Filter verbunden werden. Bei den Abfrageergebnissen erscheint dann ein Filter-Symbol mit dem man genau dieses Feature filtern kann.")]
    public string FilterUrl
    {
        get { return _filterUrl; }
        set { _filterUrl = value; }
    }
    #endregion

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _featureTableType = (FeatureTableType)stream.Load("ftabtype", (int)FeatureTableType.Default);
        _geoJuhu = (bool)stream.Load("geojuhu", _geoJuhu);
        _geoJuhuSchema = (string)stream.Load("geojuhuschema", String.Empty);
        _filterUrl = (string)stream.Load("filter", _filterUrl);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("ftabtype", (int)_featureTableType);
        stream.Save("geojuhu", _geoJuhu);
        stream.Save("geojuhuschema", _geoJuhuSchema);
        stream.Save("filter", _filterUrl);
    }
}
