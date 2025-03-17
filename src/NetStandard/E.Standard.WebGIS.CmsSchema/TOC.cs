using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class TOC : CopyableNode, IUI, ICreatable, IDisplayName, IEditable
{
    private TocGroupCheckMode _mode = TocGroupCheckMode.Lock;

    public TOC()
    {
        this.StoreUrl = false;
        this.NameUrlIdentically = true;
    }

    #region Properties
    [Browsable(true)]
    [DisplayName("Auswahlmethode")]
    [Category("Verhalten")]
    public TocGroupCheckMode CheckMode
    {
        get { return _mode; }
        set { _mode = value; }
    }
    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (String.IsNullOrEmpty(this.Url))
        {
            this.Url = this.Name;
        }

        if (appendRoot)
        {
            return this.Url + @"\.general";
        }
        else
        {
            return ".general";
        }
    }

    public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        base.Create = create;

        IInitParameter ip = new NameUrlControl();
        ((NameUrlControl)ip).UrlIsVisible = false;

        ip.InitParameter = this;
        return ip;
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _mode = (TocGroupCheckMode)stream.Load("checkmode", (int)TocGroupCheckMode.Lock);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("checkmode", (int)_mode);
    }

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Inhaltsverzeichnis"; }
    }
}
