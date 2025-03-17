using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public enum TocGroupCheckMode
{
    CheckBox = 0,
    OptionBox = 1,
    Lock = 2
}
public class TocGroup : CopyableNode, IUI, ICreatable, IDisplayName, IEditable
{
    private TocGroupCheckMode _mode = TocGroupCheckMode.CheckBox;
    private bool _visible = true, _collapsed = false;
    private bool _dropdownable = true;
    //private bool _checked = true;
    private string _metadata = String.Empty, _ogcId = String.Empty;

    public TocGroup()
    {
        base.StoreUrl = false;
        base.NameUrlIdentically = true;
        base.ValidateUrl = false;
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

    [Browsable(true)]
    [DisplayName("Sichtbar")]
    [Category("Allgemein")]
    [Description("Gibt den default Schaltzustand einer Gruppe an. Dieser ist nur bei 'CheckMode=Lock' relevant!")]
    public bool Visible
    {
        get { return _visible; }
        set { _visible = value; }
    }

    [DisplayName("Link auf Metadaten")]
    [Category("Allgemein")]
    public string MetaData
    {
        get { return _metadata; }
        set { _metadata = value; }
    }

    [Browsable(true)]
    [DisplayName("Erweiterbar (Dropdownable)")]
    [Category("Verhalten")]
    public bool DropDownable
    {
        get { return _dropdownable; }
        set { _dropdownable = value; }
    }

    [Browsable(true)]
    [DisplayName("Erweitert (beim Start)")]
    [Category("Verhalten")]
    public bool Collapsed
    {
        get { return _collapsed; }
        set { _collapsed = value; }
    }

    [DisplayName("Ogc Id")]
    [Description("Id für dieses Thema, das für WMS export verwendet wird (nur relevant, wenn die Gruppe für das 'Aufklappen' gesprerrt ist). Wenn hier nichts angeben wird, enspricht diese Id dem Namen.")]
    [Category("~WebGIS 5 OGC")]
    public string OgcId
    {
        get { return _ogcId; }
        set { _ogcId = value; }
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new TocGroupControl();
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return Crypto.GetID() + @"\.general";
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

        _mode = (TocGroupCheckMode)stream.Load("checkmode", (int)TocGroupCheckMode.CheckBox);
        _visible = (bool)stream.Load("visible", true);
        _dropdownable = (bool)stream.Load("dropdownable", true);
        _metadata = (string)stream.Load("metadata", String.Empty);
        _collapsed = (bool)stream.Load("collapsed", false);

        _ogcId = (string)stream.Load("ogcid", String.Empty);
    }
    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("checkmode", (int)_mode);
        stream.Save("visible", _visible);
        stream.Save("dropdownable", _dropdownable);
        stream.Save("metadata", _metadata);
        stream.Save("collapsed", _collapsed);

        if (!String.IsNullOrEmpty(_ogcId))
        {
            stream.Save("ogcid", _ogcId);
        }
    }

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Gruppe"; }
    }
}
