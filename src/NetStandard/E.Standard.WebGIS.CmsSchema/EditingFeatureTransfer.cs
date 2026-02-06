using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CMS;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class EditingFeatureTransfer : CopyableNode, ICreatable, IUI, IPersistable, IDisplayName
{
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public EditingFeatureTransfer(CmsItemTransistantInjectionServicePack servicePack)
        : base()
    {
        _servicePack = servicePack;

        this.CopyAttributes = true;
        this.Method = FeatureTransferMethod.Copy;
    }

    #region Properties

    [Browsable(true)]
    [DisplayName("#copy_attributes")]
    [Category("#category_copy_attributes")]
    public bool CopyAttributes { get; set; }

    [Browsable(true)]
    [DisplayName("#method")]
    [Category("#category_method")]
    public FeatureTransferMethod Method { get; set; }

    #endregion

    #region Overrides

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Editing Feature Transfer"; }
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
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

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NameUrlControl(_servicePack);

        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region IPersistable

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.CopyAttributes = (bool)stream.Load("copy_attributes", false);
        this.Method = (FeatureTransferMethod)stream.Load("method", (int)FeatureTransferMethod.Copy);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("copy_attributes", this.CopyAttributes);
        stream.Save("method", (int)this.Method);
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return Name; }
    }

    #endregion
}
