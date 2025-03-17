using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class Extent : CopyableXml, IEditable, IUI
{
    public double _minx = 0.0;
    public double _miny = 0.0;
    public double _maxx = 0.0;
    public double _maxy = 0.0;

    public int _projId = -1;
    private double[] _resolutions;
    private double _originX, _originY;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public Extent(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;
        this.Create = true;
    }

    #region Properties

    [Category("Ausdehnung")]
    [DisplayName("Minimaler Rechtswert (MinX)")]
    public double MinX
    {
        get { return _minx; }
        set { _minx = value; }
    }

    [Category("Ausdehnung")]
    [DisplayName("Minimaler Hochwert (MinY)")]
    public double MinY
    {
        get { return _miny; }
        set { _miny = value; }
    }

    [Category("Ausdehnung")]
    [DisplayName("Maximaler Rechtswert (MaxX)")]
    public double MaxX
    {
        get { return _maxx; }
        set { _maxx = value; }
    }

    [Category("Ausdehnung")]
    [DisplayName("Maximaler Hochwert (MaxY)")]
    public double MaxY
    {
        get { return _maxy; }
        set { _maxy = value; }
    }

    [DisplayName("Kartenprojektion")]
    [Category("Koordinatensystem")]
    //[Editor(typeof(TypeEditor.Proj4TypeEditor), typeof(TypeEditor.ITypeEditor))]
    public int ProjId
    {
        get { return _projId; }
        set { _projId = value; }
    }

    [DisplayName("Auflösungen (Resolutions)")]
    [Category("Ebenen")]
    public double[] Resolutions
    {
        get { return _resolutions; }
        set { _resolutions = value; }
    }

    [DisplayName("Ursprung Rechtswert (OriginX)")]
    [Category("TileCache")]
    public double OriginX
    {
        get { return _originX; }
        set { _originX = value; }
    }

    [DisplayName("Ursprung Hochwert (OriginY)")]
    [Category("TileCache")]
    public double OriginY
    {
        get { return _originY; }
        set { _originY = value; }
    }


    #endregion

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _minx = (double)stream.Load("minx", 0.0);
        _miny = (double)stream.Load("miny", 0.0);
        _maxx = (double)stream.Load("maxx", 0.0);
        _maxy = (double)stream.Load("maxy", 0.0);
        _projId = (int)stream.Load("projid", -1);

        int i = 0;
        double? res = null;
        List<double> resList = new List<double>();
        while ((res = (double?)stream.Load("res" + i, null)) != null)
        {
            resList.Add((double)res);
            i++;
        }
        _resolutions = resList.ToArray();

        _originX = (double)stream.Load("originx", 0.0);
        _originY = (double)stream.Load("originy", 0.0);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("minx", _minx);
        stream.Save("miny", _miny);
        stream.Save("maxx", _maxx);
        stream.Save("maxy", _maxy);
        stream.Save("projid", _projId);

        for (int i = 0; i < 99; i++)
        {
            if (stream.Remove("res" + i) == false)
            {
                break;
            }
        }

        if (_resolutions != null)
        {
            int i = 0;
            foreach (double res in _resolutions)
            {
                stream.Save("res" + i, res);
                i++;
            }
        }

        stream.Save("originx", _originX);
        stream.Save("originy", _originY);
    }

    #endregion

    #region ICreatable Member

    override public string CreateAs(bool appendRoot)
    {
        return this.Url;
    }

    override public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        NewExtentControl ctrl = new NewExtentControl(_servicePack, this);
        ctrl.InitParameter = this;

        return ctrl;
    }

    #endregion

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Rechtecksausdehnung"; }
    }
}

public class ExtentLink : SchemaNodeLink, IEditable
{
    public ExtentLink()
    {
    }

    [Browsable(true)]
    public string Url
    {
        get
        {
            string url = this.RelativePath.Replace("\\", "/");
            if (url.LastIndexOf("/") != -1)
            {
                return url.Substring(url.LastIndexOf("/") + 1, url.Length - url.LastIndexOf("/") - 1);
            }
            return String.Empty;
        }
    }
    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);
    }
    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);
    }
}
