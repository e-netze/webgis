using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Reflection;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

[CmsUI(PrimaryDisplayProperty = "FieldName")]
public class EditingField : CopyableXml, IUI, IEditable, IDisplayName
{
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public EditingField(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        this.Visible = true;
    }

    #region Properties

    [DisplayName("#field_name")]
    [Editor(typeof(TypeEditor.ThemeFieldsEditor), typeof(TypeEditor.ITypeEditor))]
    public string FieldName
    {
        get; set;
    }

    [DisplayName("#field_type")]
    public EditingFieldType FieldType { get; set; }

    [DisplayName("#visible")]
    public bool Visible { get; set; }

    [DisplayName("#locked")]
    public bool Locked { get; set; }

    [DisplayName("#legend_field")]
    public bool LegendField { get; set; }

    [DisplayName("#resistant")]
    public bool Resistant { get; set; }

    [DisplayName("#mass_attributable")]
    public bool MassAttributable { get; set; }

    [DisplayName("#readonly")]
    public bool Readonly { get; set; }

    [Category("~#category_clientside_validation")]
    [DisplayName("#clientside_validation")]
    public bool ClientsideValidation { get; set; }

    [Category("~#category_required")]
    [DisplayName("#required")]
    public bool Required { get; set; }

    [Category("~#category_min_length")]
    [DisplayName("#min_length")]
    public int MinLength { get; set; }

    [Category("~#category_regex_pattern")]
    [DisplayName("#regex_pattern")]
    public string RegexPattern { get; set; }

    [Category("~#category_validation_error_message")]
    [DisplayName("#validation_error_message")]

    public string ValidationErrorMessage { get; set; }

    [Category("~#category_auto_value")]
    [DisplayName("#auto_value")]
    public EditingFieldAutoValue AutoValue { get; set; }

    [Category("~#category_custom_auto_value")]
    [DisplayName("#custom_auto_value")]
    [Description(@"Hier kann zum Beispiel ein benutzerdefinierter AutoValue für eine räumliche Abfrage (gnr from grunstuecke) eingetragen werden. 
Aus Autovalue in der Liste muss in diesem Fall 'custom' ausgewählt werden, 'grundsuecke' wäre eine Abfrage. Verwendet man den AutoValue 'db_select', muss hier der Connection String zu Datenbank eingetragen werden.
Custom Autovalues können mit = beginnen. Der Wert wird dadurch immer gesetzt.
Mit dem Prefix mask-insert-default:: wird der Wert vom System gesetzt, sondern erscheint als Default Wert in der Eingabemaske, wenn ein neues Objekt erstellt wird.")]
    public string CustomAutoValue { get; set; }

    [Category("~#category_custom_auto_value2")]
    [DisplayName("#custom_auto_value2")]
    public string CustomAutoValue2 { get; set; }

    [Category("~#category_db_domain_connection_string")]
    [DisplayName("#db_domain_connection_string")]
    public string DbDomainConnectionString { get; set; }
    [Category("~#category_db_domain_table")]
    [DisplayName("#db_domain_table")]
    public string DbDomainTable { get; set; }
    [Category("~#category_db_domain_field")]
    [DisplayName("#db_domain_field")]
    public string DbDomainField { get; set; }
    [Category("~#category_db_domain_alias")]
    [DisplayName("#db_domain_alias")]
    public string DbDomainAlias { get; set; }
    [Category("~#category_db_domain_where")]
    [DisplayName("#db_domain_where")]
    public string DbDomainWhere { get; set; }
    [Category("~#category_db_order_by")]
    [DisplayName("#db_order_by")]
    public string DbOrderBy { get; set; }

    [Category("~#category_domain_list")]
    [DisplayName("#domain_list")]
    public string DomainList { get; set; }

    [Category("~#category_domain_pro_behaviour")]
    [DisplayName("#domain_pro_behaviour")]
    public bool DomainProBehaviour { get; set; }


    [Category("~#category_attribute_picker_query")]
    [DisplayName("#attribute_picker_query")]
    public string AttributePickerQuery { get; set; }
    [Category("~#category_attribute_picker_field")]
    [DisplayName("#attribute_picker_field")]
    public string AttributePickerField { get; set; }

    //[Category("~optional: Default Value")]
    //[DisplayName("Default Value")]
    //[Description("Ein Defaultwert, der in das Formular beim Erstellen einen neuen Objektes (INSERT) eingetragen wird. Der Anwender kann sich so sparen, jedes Feld zu befüllen. Dieses Option sollte micht nicht zusätzlich mit Autovalues kombinieren.")]
    //public string DefaultValue { get; set; }

    #endregion

    #region IPersistable

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.FieldName = (string)stream.Load("field", String.Empty);
        this.FieldType = (EditingFieldType)(int)stream.Load("type", (int)EditingFieldType.Text);
        this.Visible = (bool)stream.Load("visible", true);
        this.Readonly = (bool)stream.Load("readonly", false);
        this.Locked = (bool)stream.Load("locked", false);
        this.LegendField = (bool)stream.Load("legendfield", false);
        this.Resistant = (bool)stream.Load("resistant", false);
        this.MassAttributable = (bool)stream.Load("massattributable", false);

        this.Required = (bool)stream.Load("required", false);
        this.MinLength = (int)stream.Load("minlen", 0);
        this.RegexPattern = (string)stream.Load("regex", String.Empty);
        this.ValidationErrorMessage = (string)stream.Load("validation_error", String.Empty);
        this.ClientsideValidation = (bool)stream.Load("clientside_validation", false);

        this.AutoValue = (EditingFieldAutoValue)(int)stream.Load("autovalue", (int)EditingFieldAutoValue.none);
        this.CustomAutoValue = (string)stream.Load("customautovalue", String.Empty);
        this.CustomAutoValue2 = (string)stream.Load("customautovalue2", String.Empty);

        this.DbDomainConnectionString = (string)stream.Load("db_connectionstring", String.Empty);
        this.DbDomainTable = (string)stream.Load("db_table", String.Empty);
        this.DbDomainField = (string)stream.Load("db_valuefield", String.Empty);
        this.DbDomainAlias = (string)stream.Load("db_aliasfield", String.Empty);
        this.DbDomainWhere = (string)stream.Load("db_where", String.Empty);
        this.DbOrderBy = (string)stream.Load("db_orderby", String.Empty);

        this.DomainList = (string)stream.Load("domain_list", String.Empty);

        this.DomainProBehaviour = (bool)stream.Load("domain_pro_behaviour", false);

        this.AttributePickerQuery = (string)stream.Load("attribute_picker_query", String.Empty);
        this.AttributePickerField = (string)stream.Load("attribute_picker_field", String.Empty);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("field", this.FieldName ?? String.Empty);
        stream.Save("type", (int)this.FieldType);
        stream.Save("visible", this.Visible);
        stream.Save("readonly", this.Readonly);
        stream.Save("locked", this.Locked);
        stream.Save("legendfield", this.LegendField);
        stream.Save("resistant", this.Resistant);
        stream.Save("massattributable", this.MassAttributable);

        stream.Save("required", this.Required);
        stream.Save("minlen", this.MinLength);
        stream.Save("regex", this.RegexPattern ?? String.Empty);
        stream.Save("validation_error", this.ValidationErrorMessage ?? String.Empty);
        stream.Save("clientside_validation", this.ClientsideValidation);

        stream.Save("autovalue", (int)this.AutoValue);
        stream.Save("customautovalue", this.CustomAutoValue ?? String.Empty);
        stream.Save("customautovalue2", this.CustomAutoValue2 ?? String.Empty);

        stream.Save("db_connectionstring", this.DbDomainConnectionString ?? String.Empty);
        stream.Save("db_table", this.DbDomainTable ?? String.Empty);
        stream.Save("db_valuefield", this.DbDomainField ?? String.Empty);
        stream.Save("db_aliasfield", this.DbDomainAlias ?? String.Empty);
        stream.Save("db_where", this.DbDomainWhere ?? String.Empty);
        stream.Save("db_orderby", this.DbOrderBy ?? String.Empty);

        stream.Save("domain_list", this.DomainList ?? String.Empty);

        if (this.DomainProBehaviour == true)
        {
            this.DomainProBehaviour = stream.Save("domain_pro_behaviour", this.DomainProBehaviour);
        }

        stream.SaveOrRemoveIfEmpty("attribute_picker_query", this.AttributePickerQuery);
        stream.SaveOrRemoveIfEmpty("attribute_picker_field", this.AttributePickerField);
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NewEditingFieldControl(_servicePack, this);
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
        get { return "Editingfeld"; }
    }
}
