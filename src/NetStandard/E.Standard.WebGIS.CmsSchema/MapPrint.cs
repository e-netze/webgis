using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema.Abstraction;
using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public class MapPrintFormats : IEditable
{
    private bool _a4port = true;
    private bool _a4land = true;
    private bool _a3port = true;
    private bool _a3land = true;
    private bool _a2port = false;
    private bool _a2land = false;
    private bool _a1port = false;
    private bool _a1land = false;
    private bool _a0port = false;
    private bool _a0land = false;

    private bool _a4a3 = false;
    private bool _a4a2 = false;
    private bool _a4a1 = false;
    private bool _a4a0 = false;
    private bool _a3a2 = false;
    private bool _a3a1 = false;
    private bool _a2a1 = false;

    #region Properties
    [Browsable(true)]
    [DisplayName("#a4_portrait")]
    [Category("#category_a4_portrait")]
    public bool A4Portrait
    {
        get { return _a4port; }
        set { _a4port = value; }
    }
    [Browsable(true)]
    [DisplayName("#a4_landscape")]
    [Category("#category_a4_landscape")]
    public bool A4Landscape
    {
        get { return _a4land; }
        set { _a4land = value; }
    }
    [Browsable(true)]
    [DisplayName("#a3_portrait")]
    [Category("#category_a3_portrait")]
    public bool A3Portrait
    {
        get { return _a3port; }
        set { _a3port = value; }
    }
    [Browsable(true)]
    [DisplayName("#a3_landscape")]
    [Category("#category_a3_landscape")]
    public bool A3Landscape
    {
        get { return _a3land; }
        set { _a3land = value; }
    }
    [Browsable(true)]
    [DisplayName("#a2_portrait")]
    [Category("#category_a2_portrait")]
    public bool A2Portrait
    {
        get { return _a2port; }
        set { _a2port = value; }
    }
    [Browsable(true)]
    [DisplayName("#a2_landscape")]
    [Category("#category_a2_landscape")]
    public bool A2Landscape
    {
        get { return _a2land; }
        set { _a2land = value; }
    }
    [Browsable(true)]
    [DisplayName("#a1_portrait")]
    [Category("#category_a1_portrait")]
    public bool A1Portrait
    {
        get { return _a1port; }
        set { _a1port = value; }
    }
    [Browsable(true)]
    [DisplayName("#a1_landscape")]
    [Category("#category_a1_landscape")]
    public bool A1Landscape
    {
        get { return _a1land; }
        set { _a1land = value; }
    }
    [Browsable(true)]
    [DisplayName("#a0_portrait")]
    [Category("#category_a0_portrait")]
    public bool A0Portrait
    {
        get { return _a0port; }
        set { _a0port = value; }
    }
    [Browsable(true)]
    [DisplayName("#a0_landscape")]
    [Category("#category_a0_landscape")]
    public bool A0Landscape
    {
        get { return _a0land; }
        set { _a0land = value; }
    }

    [Browsable(true)]
    [DisplayName("#a4_a3")]
    [Category("#category_a4_a3")]
    public bool A4A3
    {
        get { return _a4a3; }
        set { _a4a3 = value; }
    }
    [Browsable(true)]
    [DisplayName("#a4_a2")]
    [Category("#category_a4_a2")]
    public bool A4A2
    {
        get { return _a4a2; }
        set { _a4a2 = value; }
    }
    [Browsable(true)]
    [DisplayName("#a4_a1")]
    [Category("#category_a4_a1")]
    public bool A4A1
    {
        get { return _a4a1; }
        set { _a4a1 = value; }
    }
    [Browsable(true)]
    [DisplayName("#a4_a0")]
    [Category("#category_a4_a0")]
    public bool A4A0
    {
        get { return _a4a0; }
        set { _a4a0 = value; }
    }

    [Browsable(true)]
    [DisplayName("#a3_a2")]
    [Category("#category_a3_a2")]
    public bool A3A2
    {
        get { return _a3a2; }
        set { _a3a2 = value; }
    }
    [Browsable(true)]
    [DisplayName("#a3_a1")]
    [Category("#category_a3_a1")]
    public bool A3A1
    {
        get { return _a3a1; }
        set { _a3a1 = value; }
    }

    [Browsable(true)]
    [DisplayName("#a2_a1")]
    [Category("#category_a2_a1")]
    public bool A2A1
    {
        get { return _a2a1; }
        set { _a2a1 = value; }
    }
    #endregion

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {
        _a4port = (bool)stream.Load("a4port", true);
        _a4land = (bool)stream.Load("a4land", true);
        _a3port = (bool)stream.Load("a3port", true);
        _a3land = (bool)stream.Load("a3land", true);
        _a2port = (bool)stream.Load("a2port", true);
        _a2land = (bool)stream.Load("a2land", true);
        _a1port = (bool)stream.Load("a1port", true);
        _a1land = (bool)stream.Load("a1land", true);
        _a0port = (bool)stream.Load("a0port", false);
        _a0land = (bool)stream.Load("a0land", false);

        _a4a3 = (bool)stream.Load("a4a3", false);
        _a4a2 = (bool)stream.Load("a4a2", false);
        _a4a1 = (bool)stream.Load("a4a1", false);
        _a4a0 = (bool)stream.Load("a4a0", false);

        _a3a2 = (bool)stream.Load("a3a2", false);
        _a3a1 = (bool)stream.Load("a3a1", false);

        _a2a1 = (bool)stream.Load("a2a1", false);
    }

    public void Save(IStreamDocument stream)
    {
        stream.Save("a4port", _a4port);
        stream.Save("a4land", _a4land);
        stream.Save("a3port", _a3port);
        stream.Save("a3land", _a3land);
        stream.Save("a2port", _a2port);
        stream.Save("a2land", _a2land);
        stream.Save("a1port", _a1port);
        stream.Save("a1land", _a1land);
        stream.Save("a0port", _a0port);
        stream.Save("a0land", _a0land);

        stream.Save("a4a3", _a4a3);
        stream.Save("a4a2", _a4a2);
        stream.Save("a4a1", _a4a1);
        stream.Save("a4a0", _a4a1);

        stream.Save("a3a2", _a3a2);
        stream.Save("a3a1", _a3a1);

        stream.Save("a2a1", _a2a1);
    }

    #endregion
}
