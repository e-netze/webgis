using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CmsSchema.Extensions;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class Labelling : NameUrl, ICreatable, IUI, IDisplayName
{
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public Labelling(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        this.ValidateUrl = false;
        this.StoreUrl = false;
    }

    [Browsable(false)]
    public string LabellingThemeId { get; set; }

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return Crypto.GetID() + @"/.general";
        }
        else
        {
            return ".general";
        }
    }

    public Task<bool> CreatedAsync(string FullName)
    {
        var serviceLayer = this.CmsManager.SchemaNodeInstances(_servicePack, this.RelativePath.TrimAndAppendSchemaNodePath(3, "Themes"), true)
            .Where(o => o is ServiceLayer && ((ServiceLayer)o).Id == this.LabellingThemeId)
            .FirstOrDefault() as ServiceLayer;

        if (serviceLayer != null)  // Link to QueryTheme
        {
            var link = new Link(serviceLayer.RelativePath);

            string newLinkName = Helper.NewLinkName();
            IStreamDocument xmlStream = DocumentFactory.New(this.CmsManager.ConnectionString);
            link.Save(xmlStream);
            xmlStream.SaveDocument(this.CmsManager.ConnectionString + "/" + this.RelativePath.TrimAndAppendSchemaNodePath(1, "LabellingTheme") + "/" + newLinkName);
        }

        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NewLabelingControl(_servicePack, this);
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
}

public class LabellingTheme : Link
{
}

public class LabellingField : CopyableXml, IUI, IEditable, IDisplayName
{
    private string _fieldName = String.Empty;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public LabellingField(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        this.ValidateUrl = false;
        this.StoreUrl = false;
    }

    #region Properties
    [Category("Feld")]
    [DisplayName("Feld Name")]
    [Editor(typeof(TypeEditor.ThemeFieldsEditor), typeof(TypeEditor.ITypeEditor))]
    public string FieldName
    {
        get { return _fieldName; }
        set { _fieldName = value; }
    }
    #endregion

    #region IPersistable
    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _fieldName = (string)stream.Load("fieldname", String.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("fieldname", _fieldName);
    }
    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NewLabelingFieldControl(_servicePack, this);
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region ICreatable Member

    override public string CreateAs(bool appendRoot)
    {
        return "f" + GuidEncoder.Encode(Guid.NewGuid()); //Guid.NewGuid().ToString("N");
    }

    override public Task<bool> CreatedAsync(string FullName)
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

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Beschriftungsfeld"; }
    }
}

public class LabellingFieldAssistent : SchemaNode, IAutoCreatable, IUI
{
    TableColumnAssistentControl _ctrl = null;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public LabellingFieldAssistent(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;
    }

    #region IAutoCreatable Member

    public bool AutoCreate()
    {
        if (_ctrl == null)
        {
            return false;
        }

        string path = this.CmsManager.ConnectionString + @"/" + this.RelativePath.TrimRightRelativeCmsPath(1);
        foreach (TableColumnAssistentControl.Field field in _ctrl.SelectedFields)
        {
            LabellingField lField = new LabellingField(_servicePack);
            lField.Name = field.AliasName;
            lField.FieldName = field.FieldName;

            string fullName = path + @"/" + lField.CreateAs(false) + ".xml";

            IStreamDocument xmlStream = DocumentFactory.New(path);
            lField.Save(xmlStream);
            xmlStream.SaveDocument(fullName);
            lField.CreatedAsync(fullName).Wait();
        }
        return true;
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        return "Assistent";
    }

    public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {

    }

    public void Save(IStreamDocument stream)
    {

    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        _ctrl = new TableColumnAssistentControl(_servicePack, this);

        return _ctrl;
    }

    #endregion
}

public class LabellingGroup : NameUrl, IUI, ICreatable, IDisplayName
{
    public LabellingGroup()
    {
        base.StoreUrl = false;
        base.ValidateUrl = false;
    }

    #region Properties

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
            return Crypto.GetID() + @"/.general";
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

public class LabellingLink : SchemaNodeLink, IEditable
{
    public LabellingLink()
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
