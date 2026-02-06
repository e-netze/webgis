using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace E.Standard.WebGIS.CmsSchema;

public class MapConfig : SchemaNode, IEditable
{
    private string _zoomScales = String.Empty;
    private string _refScales = String.Empty;
    private int _refScale = 0;
    private string _mapExtentUrl = String.Empty;
    private bool _useScaleConstraints = false;

    #region Properties
    [Browsable(true)]
    [DisplayName("Vordefinierte Ma�st�be")]
    [Category("Ma�stab")]
    public int[] Scales
    {
        get
        {
            List<int> scales = new List<int>();
            foreach (string s in _zoomScales.Split(';'))
            {
                int r = 0;
                if (!int.TryParse(s, out r))
                {
                    continue;
                }

                scales.Add(r);
            }
            return scales.ToArray();
        }
        set
        {
            StringBuilder sb = new StringBuilder();
            foreach (int s in value)
            {
                if (sb.Length > 0)
                {
                    sb.Append(";");
                }

                sb.Append(s.ToString());
            }
            _zoomScales = sb.ToString();
        }
    }

    [Browsable(true)]
    [DisplayName("#use_scale_constraints")]
    [Category("#category_use_scale_constraints")]
    public bool UseScaleConstraints
    {
        get { return _useScaleConstraints; }
        set { _useScaleConstraints = value; }
    }

    [Browsable(true)]
    [DisplayName("Vordefinierte Referenzma�st�be")]
    [Category("Referenzma�stab")]
    public int[] RefScales
    {
        get
        {
            List<int> scales = new List<int>();
            foreach (string s in _refScales.Split(';'))
            {
                int r = 0;
                if (!int.TryParse(s, out r))
                {
                    continue;
                }

                scales.Add(r);
            }
            return scales.ToArray();
        }
        set
        {
            StringBuilder sb = new StringBuilder();
            foreach (int s in value)
            {
                if (sb.Length > 0)
                {
                    sb.Append(";");
                }

                sb.Append(s.ToString());
            }
            _refScales = sb.ToString();
        }
    }

    [Browsable(true)]
    [DisplayName("#ref_scale")]
    [Category("#category_ref_scale")]
    public int RefScale
    {
        get { return _refScale; }
        set { _refScale = value; }
    }

    [Browsable(true)]
    [DisplayName("#map_extent_url")]
    [Category("#category_map_extent_url")]
    [Editor(typeof(TypeEditor.MapExtentsEditor), typeof(TypeEditor.ITypeEditor))]
    public string MapExtentUrl
    {
        get { return _mapExtentUrl; }
        set { _mapExtentUrl = value; }
    }
    #endregion

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {
        _zoomScales = (string)stream.Load("zoomscales", String.Empty);
        _useScaleConstraints = (bool)stream.Load("usescaleconstraints", false);

        _refScales = (string)stream.Load("refscales", String.Empty);
        _refScale = (int)stream.Load("refscale", 0);
        _mapExtentUrl = (string)stream.Load("mapextenturl", String.Empty);
    }

    public void Save(IStreamDocument stream)
    {
        stream.Save("zoomscales", _zoomScales);
        stream.Save("usescaleconstraints", _useScaleConstraints);

        stream.Save("refscales", _refScales);
        stream.Save("refscale", _refScale);
        stream.Save("mapextenturl", _mapExtentUrl);
    }

    #endregion
}
