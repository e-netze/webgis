using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security;
using E.Standard.CMS.Core.Security.Reflection;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.Schema.Reflection;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class VisFilter : CopyableXml, ICreatable, IEditable, IUI, IDisplayName
{
    private string _layernames = String.Empty;
    private string _filter = String.Empty;
    private VisFilterType _type = VisFilterType.visible;
    private bool _setlayervis = false;
    private List<GdiProperties> _gdi_properties = null;
    private List<LookupTable> _lookupTables = null;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public VisFilter(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;
    }

    #region Properties

    [Browsable(true)]
    [DisplayName("Betroffene Layer")]
    [Category("Filter")]
    [Editor(typeof(TypeEditor.SelectLayersEditor), typeof(TypeEditor.ITypeEditor))]
    public string LayerNames
    {
        get
        {
            return _layernames;
        }
        set
        {
            _layernames = value;
        }
    }

    [Browsable(true)]
    [DisplayName("SQL Filter")]
    [Category("Filter")]
    public string Filter
    {
        get
        {
            return _filter;
        }
        set
        {
            _filter = value;
        }
    }

    [Browsable(true)]
    [DisplayName("Anzeigetyp")]
    [Category("Filter")]
    [Description("Gibt an, ob der Filter im Werkzeugdialog angezeigt wird. Gesperrte Filter (locked) werden nicht angezeigt und sind automatisch immer gesetzt (der Benutzer kann den Filter nicht zurücksetzen). Unsichtbare (invisible) Filter werde nicht im Werkzeugdialog angezeigt und sind auch nicht automisch gesetzt. Diese Option ist nur sinnvoll, wenn der Filter z.B. nur als Abfragefilter verwendet werden soll.")]
    public VisFilterType Type
    {
        get
        {
            return _type;
        }
        set
        {
            _type = value;
        }
    }

    [Browsable(true)]
    [DisplayName("Betroffene Layer sichtbar schalten")]
    [Category("Filter")]
    [Description("Gibt an, ob die betroffen Layer beim Setzen des Filters automatisch sichtbar geschalten werden sollen. Dieser Parameter wird nur berücksichtigt, wenn der Anwender den Filter in der Benutzeroberfläche setzt. Wird der Filter als Url-Parameter übergeben, muss die Sichtbarkeit der Layer extra über Url-Parameter (sichtbar, darstelllungsvariante) gesetzt werden.")]
    public bool SetLayerVisibility
    {
        get { return _setlayervis; }
        set { _setlayervis = value; }
    }

    [Category("~~Nur wenn Dienst mit Gdi verwendet wird")]
    [AuthorizablePropertyArray("gdiproperties")]
    [Editor(typeof(TypeEditor.GdiPropertiesVisFilterEditor), typeof(TypeEditor.ITypeEditor))]
    [ObsoleteCmsPropeperty]
    public GdiProperties[] GdiPropertyArray
    {
        get
        {
            return _gdi_properties == null ? null : _gdi_properties.ToArray();
        }
        set
        {
            _gdi_properties = (value == null ? null : new List<GdiProperties>(value));
        }
    }

    [Browsable(true)]
    [DisplayName("Auswahlliste")]
    [Description("Hier kann für jeden Suchbegriff eine Auswahlliste definiert werden. Liefert das SQL Statement als Spalten 'VALUE' und 'NAME' zurück (SELECT f1 as NAME, f2 as VALUE FROM ....) werden diese für die Auswahlliste verwendet. Ansonsten wird die erste Spalte, sowohl für 'VALUE' und 'NAME' verwendet. Die Werte für VALUE aus der angeführten Abfrage müssen für Auswahllisten eindeutig sein.")]
    [Category("~Auswahlliste")]
    [Editor(typeof(TypeEditor.VisFilterLookupEditor), typeof(TypeEditor.ITypeEditor))]
    public LookupTable[] LookupTables
    {
        get { return _lookupTables == null ? null : _lookupTables.ToArray(); }
        set
        {
            _lookupTables = (value == null ? null : new List<LookupTable>(value));
        }
    }

    [Browsable(true)]
    [DisplayName("Sql Injektion Whitelist")]
    [Category("~Sicherheit")]
    [Description("Hier kann ein String mit Zeichen angegeben werden, die von der SQL-Injektion überprüfung ignoriert werden. zB: ><&'\"")]
    public string SqlInjectionWhiteList
    {
        get; set;
    }

    [Browsable(true)]
    [DisplayName("Optional: Lookup Layer")]
    [Category("~Auswahlliste")]
    [Editor(typeof(TypeEditor.SelectLayersEditor), typeof(TypeEditor.ITypeEditor))]
    [Description("Werden die Lookup-Werte nicht über eine Datenbank oder eine DataLinq Query abgeohlt, sondern direkt von einem Layer des Dienstes, dann dieser Layer hier eingestellt werden. Bei der Auswahlliste muss dann nur noch ein '#' als Connection String eingestellt werden. Die Angabe dieses Layers ist nur notwendig, wenn die betroffenen Layer für diesesn Filter sich auf mehrere Themen beziehen. Wird über diesen Filter nur ein Layer gefilter, ist dieser auch automatisch der Layer für die Lookup Tabelle, wenn hier nichts anderes angeführt wurde.")]
    public string LookupLayer
    {
        get; set;
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

    #region IPersistable Member

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _layernames = (string)stream.Load("layers", String.Empty);
        _filter = (string)stream.Load("filter", String.Empty);
        _type = (VisFilterType)stream.Load("type", (int)VisFilterType.visible);

        _setlayervis = (bool)stream.Load("setlayervis", false);

        _lookupTables = null;
        int lookupCount = (int)stream.Load("lookupCount", 0);
        if (lookupCount > 0)
        {
            _lookupTables = new List<LookupTable>();
            for (int i = 0; i < lookupCount; i++)
            {
                LookupTable lt = new LookupTable();
                lt.Load(stream, i);
                _lookupTables.Add(lt);
            }
        }

        this.SqlInjectionWhiteList = (string)stream.Load("sqlinjectionwhitelist", String.Empty);
        this.LookupLayer = (string)stream.Load("lookup_layer", String.Empty);

        AuthorizableArray a = new AuthorizableArray("gdiproperties", this.GdiPropertyArray, typeof(GdiProperties));
        a.Load(stream);
        if (a.Array != null && a.Array.Length > 0)
        {
            _gdi_properties = new List<GdiProperties>();
            foreach (object o in a.Array)
            {
                if (o is GdiProperties)
                {
                    _gdi_properties.Add((GdiProperties)o);
                }
            }
        }
        else
        {
            _gdi_properties = null;
        }
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("layers", _layernames);
        stream.Save("filter", _filter);
        stream.Save("type", (int)_type);

        stream.Save("setlayervis", _setlayervis);

        if (_lookupTables != null && _lookupTables.Count > 0)
        {
            stream.Save("lookupCount", _lookupTables.Count);
            for (int i = 0; i < _lookupTables.Count; i++)
            {
                _lookupTables[i].Save(stream, i);
            }
        }

        stream.SaveOrRemoveIfEmpty("sqlinjectionwhitelist", this.SqlInjectionWhiteList?.Trim());
        stream.SaveOrRemoveIfEmpty("lookup_layer", this.LookupLayer);

        AuthorizableArray a = new AuthorizableArray("gdiproperties", this.GdiPropertyArray, typeof(GdiProperties));
        a.Save(stream);
    }
    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        base.Create = create;

        IInitParameter ip = new NewVisFilterControl(_servicePack, this);
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

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Darstellungsfilter"; }
    }

    #region Helper Classes

    public class GdiProperties : AuthorizableArrayItem
    {
        private string _gdi_groupname = String.Empty;

        #region Properties

        public string GdiGroupName
        {
            get { return _gdi_groupname; }
            set { _gdi_groupname = value; }
        }

        #endregion

        public override string ToString()
        {
            return "Gdi: (Group: " + GdiGroupName + ")";
        }

        public override void Load()
        {
            this.GdiGroupName = (string)this.StreamLoad("gdi_groupname", String.Empty);
        }

        public override void Save()
        {
            this.StreamSave("gdi_groupname", this.GdiGroupName);
        }
    }

    public class LookupTable
    {
        private Lookuptype _type = Lookuptype.ComboBox;
        private string _key = String.Empty;
        private LookUp _lookup = null;

        public Lookuptype LookupType { get { return _type; } set { _type = value; } }

        [DisplayName("Key-Feld")]
        public string Key { get { return _key; } set { _key = value; } }

        [DisplayName("Auswahlliste")]
        [Description("Auswahlliste für dieses Key-Feld")]
        [Editor(typeof(TypeEditor.LookUpEditor), typeof(TypeEditor.ITypeEditor))]
        public LookUp LookUp
        {
            get { return _lookup; }
            set { _lookup = value; }
        }

        #region IPersistable Member

        public void Load(IStreamDocument stream, int index)
        {
            _type = (Lookuptype)stream.Load("type" + index, (int)Lookuptype.ComboBox);
            _key = (string)stream.Load("key" + index, String.Empty);
            string lookup_connectionstring = (string)stream.Load("lookup_connectionstring" + index, String.Empty);
            string lookup_sqlclause = (string)stream.Load("lookup_sqlclause" + index, String.Empty);
            if (!String.IsNullOrEmpty(lookup_connectionstring) ||
                !String.IsNullOrEmpty(lookup_sqlclause))
            {
                _lookup = new LookUp(lookup_connectionstring, lookup_sqlclause);
            }
            else
            {
                _lookup = null;
            }
        }

        public void Save(IStreamDocument stream, int index)
        {
            stream.Save("type" + index, (int)_type);
            stream.Save("key" + index, _key);
            if (_lookup != null)
            {
                stream.Save("lookup_connectionstring" + index, _lookup.ConnectionString);
                stream.Save("lookup_sqlclause" + index, _lookup.SqlClause);
            }
        }

        #endregion
    }
    #endregion
}

public class VisFilterGroup : NameUrl, IUI, ICreatable, IDisplayName, IEditable
{
    public VisFilterGroup()
    {
        base.StoreUrl = false;
        base.ValidateUrl = true;
    }

    #region Properties

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        //IInitParameter ip = new TocGroupControl();
        //ip.InitParameter = this;

        //return ip;

        base.Create = create;

        IInitParameter ip = new NameUrlControl();
        //((NameUrlControl)ip).UrlIsVisible = false;

        ip.InitParameter = this;
        return ip;
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            //return Crypto.GetID() + @"/.general";
            return this.Url + @"/.general";
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
    }
    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);
    }
}

public class VisFilterLink : SchemaNodeLink, IEditable
{
    public VisFilterLink()
    {
    }

    [Browsable(true)]
    public string Url
    {
        get
        {
            string url = this.RelativePath.Replace("\\", "/");
            if (url.LastIndexOf("/") != -1)
            {
                return url.Substring(url.LastIndexOf("/") + 1, url.Length - url.LastIndexOf("/") - 1);
            }
            return String.Empty;
        }
    }
    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);
    }
    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);
    }
}
