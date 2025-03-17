﻿using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class CopyrightInfo : CopyableXml, IEditable, IUI, IDisplayName
{
    public CopyrightInfo()
    {
        this.Create = true;
    }

    #region Properties

    [Browsable(true)]
    [DisplayName("Copyright Text")]
    [Category("Copyright")]
    public string Copyright
    {
        get; set;
    }

    [Browsable(true)]
    [DisplayName("Copyright Link")]
    [Category("Copyright")]
    public string CopyrightLink
    {
        get; set;
    }

    [Browsable(true)]
    [DisplayName("Copyright Link Text")]
    [Category("Copyright")]
    public string CopyrightLinkText
    {
        get; set;
    }

    [Browsable(true)]
    [DisplayName("Hinweis Text")]
    [Category("Allgemein")]
    public string Advice
    {
        get; set;
    }

    [Browsable(true)]
    [DisplayName("Url für Logo")]
    [Category("Allgemein")]
    public string Logo
    {
        get; set;
    }

    [Browsable(true)]
    [DisplayName("Logo Breite in Pixel")]
    [Category("Allgemein")]
    public int LogoWidth
    {
        get; set;
    }

    [Browsable(true)]
    [DisplayName("Logo Höhe in Pixel")]
    [Category("Allgemein")]
    public int LogoHeight
    {
        get; set;
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name + " (" + this.Url + ")"; }
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        NameUrlControl ctrl = new NameUrlControl();
        ctrl.InitParameter = this;
        ctrl.NameIsVisible = true;

        return ctrl;
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

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "CopyrightInfo"; }
    }

    #region IPersistable Member

    override public void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.Copyright = (string)stream.Load("copyright", String.Empty);
        this.CopyrightLink = (string)stream.Load("copyrightlink", String.Empty);
        this.CopyrightLinkText = (string)stream.Load("copyrightlinktext", String.Empty);

        this.Advice = (string)stream.Load("advice", String.Empty);
        this.Logo = (string)stream.Load("logo", String.Empty);
        this.LogoWidth = (int)stream.Load("logowidth", 0);
        this.LogoHeight = (int)stream.Load("logoheight", 0);
    }

    override public void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("copyright", this.Copyright ?? String.Empty);
        stream.Save("copyrightlink", this.CopyrightLink ?? String.Empty);
        stream.Save("copyrightlinktext", this.CopyrightLinkText ?? String.Empty);

        stream.Save("advice", this.Advice ?? String.Empty);
        stream.Save("logo", this.Logo ?? String.Empty);
        stream.Save("logowidth", this.LogoWidth);
        stream.Save("logoheight", this.LogoHeight);
    }

    #endregion
}
