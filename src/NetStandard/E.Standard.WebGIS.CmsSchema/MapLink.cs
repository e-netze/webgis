using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using System;
using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public class MapLink : Link, IEditable
{
    private bool _visible = true;
    private bool _default = false;

    private string _defaultTemplate = String.Empty;
    private string _defaultLayout = String.Empty;
    private string _defaultDesign = String.Empty;
    private string _defaultStyles = String.Empty;

    #region Properties
    [DisplayName("#visible")]
    [Category("#category_visible")]
    public bool Visible
    {
        get { return _visible; }
        set { _visible = value; }
    }
    [DisplayName("#default")]
    [Category("#category_default")]
    public bool Default
    {
        get { return _default; }
        set { _default = value; }
    }

    [DisplayName("#default_template")]
    [Category("~#category_default_template")]
    public string DefaultTemplate
    {
        get { return _defaultTemplate; }
        set { _defaultTemplate = value; }
    }
    [DisplayName("#default_layout")]
    [Category("~#category_default_layout")]
    public string DefaultLayout
    {
        get { return _defaultLayout; }
        set { _defaultLayout = value; }
    }
    [DisplayName("#default_design")]
    [Category("~#category_default_design")]
    public string DefaultDesign
    {
        get { return _defaultDesign; }
        set { _defaultDesign = value; }
    }
    [DisplayName("#default_styles")]
    [Category("~#category_default_styles")]
    public string DefaultStyles
    {
        get { return _defaultStyles; }
        set { _defaultStyles = value; }
    }
    #endregion

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _visible = (bool)stream.Load("visible", true);
        _default = (bool)stream.Load("default", false);

        _defaultDesign = (string)stream.Load("defaultdesign", String.Empty);
        _defaultStyles = (string)stream.Load("defaultstyles", String.Empty);
        _defaultLayout = (string)stream.Load("defaultlayout", String.Empty);
        _defaultTemplate = (string)stream.Load("defaulttemplate", String.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("visible", _visible);
        stream.Save("default", _default);

        stream.Save("defaultdesign", _defaultDesign);
        stream.Save("defaultstyles", _defaultStyles);
        stream.Save("defaultlayout", _defaultLayout);
        stream.Save("defaulttemplate", _defaultTemplate);
    }
}
