using System;
using System.ComponentModel;
using System.Threading.Tasks;

using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;

namespace E.Standard.WebGIS.CmsSchema;

public class TableExportFormat : CopyableXml, IUI, IEditable, IDisplayName
{
    public TableExportFormat()
    {
        base.StoreUrl = false;
        base.ValidateUrl = false;
    }

    #region Properties

    [DisplayName("#format_string")]
    [Category("#category_format_string")]
    public string FormatString { get; set; } = "";

    [DisplayName("#file_extension")]
    [Category("#category_file_extension")]
    public string FileExtension { get; set; } = "txt";

    [DisplayName("Beschreibung")]
    [Description("Wird dem Anwender im Download/Copy-To-Clipboard dialog angezeigt")]
    [Category("Allgemein")]

    public string Description { get; set; } = "";

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NameUrlControl();
        ((NameUrlControl)ip).UrlIsVisible = false;

        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region IPersistable Member

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.FormatString = (string)stream.Load("formatstring", String.Empty);
        this.FileExtension = (string)stream.Load("fileext", "txt");
        this.Description = (string)stream.Load("description", String.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("formatstring", this.FormatString);
        stream.Save("fileext", this.FileExtension);
        stream.Save("description", this.Description);
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    #region ICreatable Member

    override public string CreateAs(bool appendRoot)
    {
        return "s" + GuidEncoder.Encode(Guid.NewGuid()); //Guid.NewGuid().ToString("N");
    }

    override public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Tabellen Export Format"; }
    }
}
