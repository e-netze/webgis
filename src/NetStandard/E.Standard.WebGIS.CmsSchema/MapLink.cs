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
    [DisplayName("Sichtbar")]
    [Category("Allgemein")]
    public bool Visible
    {
        get { return _visible; }
        set { _visible = value; }
    }
    [DisplayName("Default Karte")]
    [Category("Allgemein")]
    public bool Default
    {
        get { return _default; }
        set { _default = value; }
    }

    [DisplayName("Viewer Template (optional)")]
    [Category("~Aufruf (optional)")]
    public string DefaultTemplate
    {
        get { return _defaultTemplate; }
        set { _defaultTemplate = value; }
    }
    [DisplayName("Viewer Layout (optional)")]
    [Category("~Aufruf (optional)")]
    public string DefaultLayout
    {
        get { return _defaultLayout; }
        set { _defaultLayout = value; }
    }
    [DisplayName("Viewer Design (optional)")]
    [Category("~Aufruf (optional)")]
    public string DefaultDesign
    {
        get { return _defaultDesign; }
        set { _defaultDesign = value; }
    }
    [DisplayName("Viewer Styles (optional)")]
    [Category("~Aufruf (optional)")]
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
