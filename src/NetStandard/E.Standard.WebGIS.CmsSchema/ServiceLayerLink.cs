using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.WebGIS.CMS;
using System;
using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public class ServiceLayerLink : SchemaNodeLink, IPersistable, IEditable
{
    private string _aliasName = String.Empty;

    public ServiceLayerLink()
    {
        this.Locked = false;
        this.Legend = true;
    }

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
        get; set;
    }
    [DisplayName("#locked")]
    [Category("#category_locked")]
    public bool Locked
    {
        get; set;
    }

    [DisplayName("#legend")]
    [Category("#category_legend")]
    public bool Legend
    {
        get; set;
    }

    [DisplayName("#legend_aliasname")]
    [Category("#category_legend_aliasname")]
    public string LegendAliasname { get; set; }

    [DisplayName("#metadata_link")]
    [Category("~#category_metadata_link")]

    public string MetadataLink
    {
        get; set;
    }

    [DisplayName("#metadata_target")]
    [Category("#category_metadata_target")]
    public BrowserWindowTarget2 MetadataTarget { get; set; }

    [DisplayName("#metadata_title")]
    [Category("#category_metadata_title")]
    public string MetadataTitle { get; set; }

    [DisplayName("#metadata_link_button_style")]
    [Category("#category_metadata_link_button_style")]
    public MetadataButtonStyle MetadataLinkButtonStyle { get; set; }

    [DisplayName("#meta_data_format")]
    [Category("#category_meta_data_format")]
    [Browsable(false)]  // unsed
    public string MetaDataFormat
    {
        get; set;
    }

    //[DisplayName("Refenzmaßstab anwenden")]
    //[Category("Allgemein")]
    //public bool RefScale
    //{
    //    get;set;
    //}
    //[DisplayName("Download erlauben")]
    //[Description("Daten dieses Themas können heruntergeladen werden.")]
    //[Category("~Download")]
    //public bool Downloadable
    //{
    //    get;set;
    //}
    //[DisplayName("Themenbeschriftung")]
    //[Description("Themenbeschriftung kann durch den User geändert werden. Nur für IMS Dienste.")]
    //[Category("~Labeling (nicht mehr unterstützt)")]
    //public ThemeLabeling ThemeLabeling
    //{
    //    get;set;
    //}

    [DisplayName("#ogc_id")]
    [Category("~#category_ogc_id")]
    public string OgcId
    {
        get; set;
    }

    [DisplayName("#abstract")]
    [Description("")]
    [Category("~#category_abstract")]
    [Editor(typeof(TypeEditor.MultilineStringEditor), typeof(TypeEditor.ITypeEditor))]
    public string Abstract
    {
        get; set;
    }

    #endregion

    #region IPersistable Member

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.AliasName = (string)stream.Load("aliasname", String.Empty);
        //this.MetaData = (string)stream.Load("metadata", String.Empty);
        this.MetaDataFormat = (string)stream.Load("metadataformat", "text/html");
        this.Visible = (bool)stream.Load("visible", this.Visible);
        this.Locked = (bool)stream.Load("locked", this.Locked);
        this.OgcId = (string)stream.Load("ogcid", String.Empty);
        this.Abstract = (string)stream.Load("abstract", String.Empty);
        //_ogcGroup = (string)stream.Load("ogcgroup", String.Empty);

        this.Legend = (bool)stream.Load("legend", this.Legend);
        this.LegendAliasname = (string)stream.Load("legendaliasname", String.Empty);
        //_refscale = (bool)stream.Load("refscale", _refscale);
        //_downloadable = (bool)stream.Load("downloadable", _downloadable);

        this.MetadataLink = (string)stream.Load("metadata", String.Empty);
        this.MetadataTarget = (BrowserWindowTarget2)(int)stream.Load("metadata_target", (int)BrowserWindowTarget2.tab);
        this.MetadataTitle = (string)stream.Load("metadata_title", String.Empty);
        this.MetadataLinkButtonStyle = (MetadataButtonStyle)stream.Load("metadata_button_style", (int)MetadataButtonStyle.i_button);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("aliasname", _aliasName);
        //stream.SaveOrRemoveIfEmpty("metadata", this.MetaData);
        stream.SaveOrRemoveIfEmpty("metadataformat", this.MetaDataFormat);
        stream.Save("visible", this.Visible);
        stream.Save("locked", this.Locked);
        stream.SaveOrRemoveIfEmpty("ogcid", this.OgcId);
        stream.SaveOrRemoveIfEmpty("abstract", this.Abstract);
        //if (!String.IsNullOrEmpty(_ogcGroup))
        //    stream.Save("ogcgroup", _ogcGroup);

        stream.Save("legend", this.Legend);
        stream.Save("legendaliasname", this.LegendAliasname ?? String.Empty);
        //stream.Save("refscale", _refscale);
        //stream.Save("downloadable", _downloadable);

        stream.SaveOrRemoveIfEmpty("metadata", this.MetadataLink);
        if (this.MetadataTarget != BrowserWindowTarget2.tab)
        {
            stream.Save("metadata_target", (int)this.MetadataTarget);
        }
        stream.SaveOrRemoveIfEmpty("metadata_title", this.MetadataTitle);
        if (this.MetadataLinkButtonStyle != MetadataButtonStyle.i_button)
        {
            stream.Save("metadata_button_style", (int)this.MetadataLinkButtonStyle);
        }
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
            this.Visible = (bool)stream.Load("visible", true);
        }
    }
}
