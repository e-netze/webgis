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

    [DisplayName("Feld")]
    [Category("Allgemein")]
    [Description("Name des Feldes im Ziel")]
    public string Field { get; set; }

    [DisplayName("Value Expression")]
    [Category("Allgemein")]
    [Description("Wert der geschrieben werden sollte")]
    public string ValueExpression { get; set; }

    [DisplayName("Ist Default Value")]
    [Category("Allgemein")]
    [Description("Der hier angegebene Wert ist ein Defaultwert und wird nur übernommen, wenn das Feld in der Quell Featureklasse leer ist. Ist das Feld in der Quellfeatureklasse gesetzt, wird dieser übernommen. Ist dieser Wert auf 'false' gesetzt, werden die hier angeführte Wert immer gesetzt => Der aus der Quellfeatureklasse wird überschrieben.")]
    public bool IsDefaultValue { get; set; }

    [DisplayName("Ist im Ziel erforderlich")]
    [Category("Allgemein")]
    [Description("Des Ergebnis aus der Value-Expression muss einen Wert ergeben, das Ergebnis darf nicht leer sein. Außerdem muss das Feld in der Ziel Featureklasse vorhanden sein, ansonsten wird ein Fehler ausgegeben.")]
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
