using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class TableExportFormat : CopyableXml, IUI, IEditable, IDisplayName
{
    private string _formatstring = String.Empty;
    private string _fileextension = "txt";

    public TableExportFormat()
    {
        base.StoreUrl = false;
        base.ValidateUrl = false;
    }

    #region Properties
    [DisplayName("Format String")]
    [Description("String f�r die Formatierung einer Zeile. Ein Zeilenumbruch entspricht \\r\\n. Platzhalter f�r Spalten in eckigen Klammern schreiben. (Beispiel: [KG]+[NR]\\r\\n)")]
    [Category("Allgemein")]
    public string FormatString
    {
        get { return _formatstring; }
        set { _formatstring = value; }
    }
    [DisplayName("Datei Erweiterung")]
    [Description("Die fertig erstellte Exportdatei hat die hier angegebene Datei Erweiterung")]
    [Category("Allgemein")]
    public string FileExtension
    {
        get { return _fileextension; }
        set { _fileextension = value; }
    }
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

        _formatstring = (string)stream.Load("formatstring", String.Empty);
        _fileextension = (string)stream.Load("fileext", "txt");
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("formatstring", _formatstring);
        stream.Save("fileext", _fileextension);
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
