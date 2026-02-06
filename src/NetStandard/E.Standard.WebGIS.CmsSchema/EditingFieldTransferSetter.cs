using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class EditingFieldTransferSetter : NameUrl, IUI, ICreatable, IDisplayName, IEditable
{
    public EditingFieldTransferSetter()
    {
        base.StoreUrl = false;
        //base.ValidateUrl = false;
    }

    #region Properties

    [DisplayName("#field")]
    [Category("#category_field")]
    public string Field { get; set; }

    [DisplayName("#value_expression")]
    [Category("#category_value_expression")]
    public string ValueExpression { get; set; }

    [DisplayName("#is_default_value")]
    [Category("#category_is_default_value")]
    public bool IsDefaultValue { get; set; }

    [DisplayName("#is_required")]
    [Category("#category_is_required")]
    public bool IsRequired { get; set; }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NameUrlControl();
        //((NameUrlControl)ip).UrlIsVisible = false;

        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        //return $"s{GuidEncoder.Encode(Guid.NewGuid())}";
        return this.Url;
    }

    public Task<bool> CreatedAsync(string FullName)
    {
        this.Field = this.Name;

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

    #region IPersistable

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.Field = (string)stream.Load("field", String.Empty);
        this.ValueExpression = (string)stream.Load("value_expression", String.Empty);

        this.IsDefaultValue = (bool)stream.Load("is_defaultvalue", false);
        this.IsRequired = (bool)stream.Load("is_required", false);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.SaveOrRemoveIfEmpty("field", this.Field);
        stream.SaveOrRemoveIfEmpty("value_expression", this.ValueExpression);

        stream.Save("is_defaultvalue", this.IsDefaultValue);
        stream.Save("is_required", this.IsRequired);
    }

    #endregion
}
