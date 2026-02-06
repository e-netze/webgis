using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public class TocTheme : SchemaNodeLink, IPersistable, IDisplayName, IEditable
{
    private string _aliasName = String.Empty, _metadata = String.Empty, _metadataFormat = "text/html";
    private bool _visible = true, _locked = false, _refscale = true, _legend = true, _downloadable = false;
    private ThemeLabeling _labeling = null;
    private GdiPropertiesEditThemeArray _gdi_properties = null;
    private string _ogcId = String.Empty, _abstract/*, _ogcGroup = String.Empty*/;

    #region Properties
    [DisplayName("#alias_name")]
    [Category("#category_alias_name")]
    public string AliasName
    {
        get { return _aliasName; }
        set
        {
            _aliasName = value;
            if (_aliasName.Contains("\\"))
            {
                int pos = _aliasName.LastIndexOf("\\");
                _aliasName = _aliasName.Substring(pos + 1, _aliasName.Length - pos - 1);
            }
        }
    }

    [DisplayName("#visible")]
    [Category("#category_visible")]
    public bool Visible
    {
        get { return _visible; }
        set { _visible = value; }
    }
    [DisplayName("#locked")]
    [Category("#category_locked")]
    public bool Locked
    {
        get { return _locked; }
        set { _locked = value; }
    }
    [DisplayName("#legend")]
    [Category("#category_legend")]
    public bool Legend
    {
        get { return _legend; }
        set { _legend = value; }
    }

    [DisplayName("#legend_aliasname")]
    [Category("#category_legend_aliasname")]
    public string LegendAliasname { get; set; }

    [DisplayName("#ref_scale")]
    [Category("#category_ref_scale")]
    public bool RefScale
    {
        get { return _refscale; }
        set { _refscale = value; }
    }
    [DisplayName("#downloadable")]
    [Category("~#category_downloadable")]
    public bool Downloadable
    {
        get { return _downloadable; }
        set { _downloadable = value; }
    }
    [DisplayName("#theme_labeling")]
    [Category("~#category_theme_labeling")]
    public ThemeLabeling ThemeLabeling
    {
        get { return _labeling; }
        set { _labeling = value; }
    }

    [Category("Nur wenn Dienst mit Gdi verwendet wird")]
    [AuthorizablePropertyArray("gdiproperties")]
    [Editor(typeof(TypeEditor.GdiPropertiesEditThemeEditor),
            typeof(TypeEditor.ITypeEditor))]
    [AuthorizableProperty("gdiproperties", null)]
    public GdiPropertiesEditTheme[] GdiPropertyArray
    {
        get { return _gdi_properties == null ? null : _gdi_properties.ToArray(); }
        set
        {
            _gdi_properties = (value == null ? null : new GdiPropertiesEditThemeArray(this, value));
        }
    }

    [DisplayName("#ogc_id")]
    [Category("~#category_ogc_id")]
    public string OgcId
    {
        get { return _ogcId; }
        set { _ogcId = value; }
    }

    [DisplayName("#abstract")]
    [Description("")]
    [Category("~#category_abstract")]
    [Editor(typeof(TypeEditor.MultilineStringEditor), typeof(TypeEditor.ITypeEditor))]
    public string Abstract
    {
        get { return _abstract; }
        set { _abstract = value; }
    }

    [DisplayName("#meta_data")]
    [Category("~#category_meta_data")]
    public string MetaData
    {
        get { return _metadata; }
        set { _metadata = value; }
    }
    [DisplayName("#meta_data_format")]
    [Category("~#category_meta_data_format")]
    public string MetaDataFormat
    {
        get { return _metadataFormat; }
        set { _metadataFormat = value; }
    }

    //[DisplayName("Metadaten Format")]
    //[Description("application/xml oder text/xml oder text/html")]
    //[Category("~WebGIS 5 OGC")]
    //public string OgcGroup
    //{
    //    get { return _ogcGroup; }
    //    set { _ogcGroup = value; }
    //}

    #endregion

    #region IPersistable Member

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.AliasName = (string)stream.Load("aliasname", String.Empty);
        _metadata = (string)stream.Load("metadata", String.Empty);
        _metadataFormat = (string)stream.Load("metadataformat", "text/html");
        _visible = (bool)stream.Load("visible", _visible);
        _locked = (bool)stream.Load("locked", _locked);
        _ogcId = (string)stream.Load("ogcid", String.Empty);
        _abstract = (string)stream.Load("abstract", String.Empty);
        //_ogcGroup = (string)stream.Load("ogcgroup", String.Empty);

        _legend = (bool)stream.Load("legend", _legend);
        this.LegendAliasname = (string)stream.Load("legendaliasname", String.Empty);
        _refscale = (bool)stream.Load("refscale", _refscale);
        _downloadable = (bool)stream.Load("downloadable", _downloadable);

        string lfields = (string)stream.Load("labeling_fields", String.Empty);
        string lfieldsAlias = (string)stream.Load("labeling_fieldsalias", String.Empty);

        if (!String.IsNullOrEmpty(lfields) &&
            !String.IsNullOrEmpty(lfieldsAlias))
        {
            _labeling = new ThemeLabeling(lfields, lfieldsAlias);
        }
        else
        {
            _labeling = null;
        }

        _gdi_properties = GdiPropertiesEditThemeArray.Load(this, stream, "gdiproperties");
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("aliasname", _aliasName);
        stream.Save("metadata", _metadata);
        stream.Save("metadataformat", _metadataFormat);
        stream.Save("visible", _visible);
        stream.Save("locked", _locked);
        if (!String.IsNullOrEmpty(_ogcId))
        {
            stream.Save("ogcid", _ogcId);
        }

        if (!String.IsNullOrEmpty(_abstract))
        {
            stream.Save("abstract", _abstract);
        }
        //if (!String.IsNullOrEmpty(_ogcGroup))
        //    stream.Save("ogcgroup", _ogcGroup);

        stream.Save("legend", _legend);
        stream.Save("legendaliasname", this.LegendAliasname ?? String.Empty);
        stream.Save("refscale", _refscale);
        stream.Save("downloadable", _downloadable);

        stream.Save("labeling_fields", (_labeling != null ? _labeling.Fields : String.Empty));
        stream.Save("labeling_fieldsalias", (_labeling != null ? _labeling.FieldsAlias : String.Empty));

        GdiPropertiesEditThemeArray.Save(stream, "gdiproperties", _gdi_properties);
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return _aliasName; }
    }

    #endregion

    public override void LoadParent(IStreamDocument stream)
    {
        base.LoadParent(stream);

        if (stream == null)
        {
            return;
        }

        if (String.IsNullOrEmpty(_aliasName))
        {
            this.AliasName = (string)stream.Load("name", String.Empty);
            _visible = (bool)stream.Load("visible", true);
        }
    }

    public override bool HasAdditionalLinks(string rootDirectory, string parentPath)
    {
        _additionalGroup = null;
        if (parentPath.ToLower().StartsWith("services/miscellaneous/servicecollection/"))
        {
            List<string> equalNameThemes = new List<string>();
            //List<string> equalIdThemes = new List<string>();

            string[] p = parentPath.Split('/');
            string servicesPath = rootDirectory + @"\" + p[0] + @"\" + p[1] + @"\" + p[2] + @"\" + p[3] + @"\services";
            ItemOrder servicesOrder = new ItemOrder(servicesPath, false);
            foreach (string service in servicesOrder.Items)
            {
                IStreamDocument xmlStream = DocumentFactory.Open(servicesPath + @"\" + service);
                Link link = new Link();
                link.Load(xmlStream);
                if (String.IsNullOrEmpty(link.LinkUri))
                {
                    continue;
                }

                string themesPath = rootDirectory + @"\" + link.LinkUri + @"\themes";
                ItemOrder themesOrder = new ItemOrder(themesPath);
                foreach (string theme_ in themesOrder.Items)
                {
                    string theme = theme_;
                    if (theme.ToLower().EndsWith(".xml"))
                    {
                        theme = theme.Substring(0, theme.Length - 4);
                    }
                    else if (theme.ToLower().EndsWith(".link"))
                    {
                        theme = theme.Substring(0, theme.Length - 5);
                    }

                    xmlStream = DocumentFactory.Open(themesPath + @"\" + theme_);
                    string aliasName = (string)xmlStream.Load("name", String.Empty);
                    string id = (string)xmlStream.Load("id", String.Empty);

                    if (aliasName.ToLower() == _aliasName.ToLower())
                    {
                        equalNameThemes.Add(link.LinkUri + "/themes/" + theme);
                    }
                }
            }

            _additionalLinks = new List<Link>();
            if (equalNameThemes.Count > 1)
            {
                throw new Exception("In den eingebunden Diensten existieren mehrere Themen mit gleichem Namen");
                //FormEqualItems dlg = new FormEqualItems(_aliasName, equalNameThemes.ToArray());
                //dlg.HeaderText = "In den eingebunden Diensten existieren mehrere Themen mit gleichem Namen. Sollen diese Themen auch eingef�gt werden?";
                //dlg.ShowSetVisible = dlg.ShowGroup = true;
                //if (dlg.ShowDialog() == DialogResult.OK)
                //{
                //    foreach (string item in dlg.CheckedItems)
                //    {
                //        TocTheme additionalTheme = new TocTheme();
                //        additionalTheme.LinkUri = item;
                //        additionalTheme.AliasName = _aliasName;
                //        additionalTheme.Visible = dlg.SetVisible;
                //        _additionalLinks.Add(additionalTheme);
                //    }

                //    if (dlg.GroupElements)
                //    {
                //        _additionalGroup = new TocGroup();
                //        _additionalGroup.Name = dlg.GroupName;
                //        _additionalGroup.DropDownable = false;
                //    }
                //    return true;
                //}
            }
            return false;
        }
        else
        {
            return false;
        }
    }

    List<Link> _additionalLinks = null;
    public override List<Link> AdditionalLinks(string rootDirectory, string parentPath)
    {
        return _additionalLinks;
    }

    TocGroup _additionalGroup = null;
    public override ICreatable GroupForAdditianalLinks()
    {
        return _additionalGroup;
    }
}

public class ThemeLabeling
{
    private string _fields = String.Empty;
    private string _fieldsAlias = String.Empty;

    public ThemeLabeling() { }
    public ThemeLabeling(string fields, string fieldsalias)
    {
        _fields = fields;
        _fieldsAlias = fieldsalias;
    }

    public string Fields
    {
        get { return _fields; }
        set
        {
            if (value == null)
            {
                _fields = String.Empty;
            }
            else
            {
                _fields = value;
            }
        }
    }
    public string FieldsAlias
    {
        get { return _fieldsAlias; }
        set
        {
            if (value == null)
            {
                _fieldsAlias = String.Empty;
            }
            else
            {
                _fieldsAlias = value;
            }
        }
    }
}
