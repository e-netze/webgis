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
    [DisplayName("#feature_table_type")]
    [Category("#category_feature_table_type")]
    public FeatureTableType FeatureTableType
    {
        get { return _featureTableType; }
        set { _featureTableType = value; }
    }

    [DisplayName("#geo_juhu")]
    [Category("#category_geo_juhu")]
    [Editor(typeof(TypeEditor.RegExTypeEditor), typeof(TypeEditor.ITypeEditor))]
    public bool GeoJuhu
    {
        get { return _geoJuhu; }
        set { _geoJuhu = value; }
    }

    [DisplayName("#geo_juhu_schema")]
    [Category("#category_geo_juhu_schema")]
    public string GeoJuhuSchema
    {
        get { return _geoJuhuSchema; }
        set { _geoJuhuSchema = value; }
    }

    [DisplayName("#filter_url")]
    [Category("#category_filter_url")]
    [Editor(typeof(TypeEditor.SelectMapVisFiler), typeof(TypeEditor.ITypeEditor))]
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
