using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CMS;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class EditingMaskValidation : CopyableXml, IUI, ICreatable, IDisplayName
{
    public EditingMaskValidation()
    {
        base.StoreUrl = false;
        base.ValidateUrl = false;
    }

    #region Properties

    [DisplayName("Field Name")]
    [Description("Feld, das vor dem Speichern geprüft werden sollte")]
    public string FieldName { get; set; } = string.Empty;
    [DisplayName("Operator")]
    [Description("Methode, mit der der Feld Wert zum Validator geprüft wird: zB: Ident (=genau gleich), Equals (=gleich aber case-insensitiv), in (Feld Wert muss in der Liste der Validator Werte vorkommen), inside (einer der Feld Werte muss in einem der Validator Werte vorkommen), IN/INSIDE (wie in/inside allerderins case-sensitiv). Kommen im Feldwert oder im Validator Werte ',' oder ';' vor, werden diese als Trennzeichen verwendet und der Wert als Liste interpretiert.")]
    public MaskValidationOperators Operator { get; set; }
    [DisplayName("Validator")]
    [Description("Wert auf den geprüft wird. Hier kann auch auf eine Liste mit Trennzeichen (, oder ;) angegeben werden. Außerdem sind Platzhalter für User-Rollen möglich: role-parameter:GEMNR")]
    public string Validator { get; set; } = string.Empty;
    [DisplayName("Message")]
    [Description("Nachricht, die ausgeben wird, wenn Validation fehl schlägt")]
    public string Message { get; set; } = string.Empty;

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
        get { return "EditMask Validation"; }
    }

    #region IPersistable

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.FieldName = (string)stream.Load("fieldname", string.Empty);
        this.Operator = (MaskValidationOperators)stream.Load("operator", 0);
        this.Validator = (string)stream.Load("validator", string.Empty);
        this.Message = (string)stream.Load("message", string.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("fieldname", this.FieldName);
        stream.Save("operator", (int)this.Operator);
        stream.Save("validator", this.Validator);
        stream.Save("message", this.Message);
    }

    #endregion
}
