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

    [DisplayName("Feld Name")]
    [Editor(typeof(TypeEditor.ThemeFieldsEditor), typeof(TypeEditor.ITypeEditor))]
    public string FieldName
    {
        get; set;
    }

    [DisplayName("Eingabe Typ")]
    [Description("Neben Text-Eingabefeldern können hier noch weitere Eingabetypen definiert werden: Auswahlliste (Domain), Datum, Fileupload, ...")]
    public EditingFieldType FieldType { get; set; }

    [DisplayName("Sichtbar")]
    [Description("Gibt an, ob das Feld für den Anwender sichtbar ist. Unsichtbare Felder können praktisch sein, wenn diese eventuell erst später über einen AutoValue berechnet werden oder bereits über Url übergeben werde und verändert werden dürfen.")]
    public bool Visible { get; set; }

    [DisplayName("Gesperrt (nicht veränderbar)")]
    [Description("Wie bei 'nicht Sichtbar'. Hier wird das Feld allergings angezeigt, kann vom Anwender aber nicht geändert werden. Locked Felder werden beim Speichern auch in die Datenbank geschreiben. Kann nützlich sein, wenn ein Wert (ID) schon über Url Aufruf übergeben wird und der Anwender diesen nicht mehr ändern sollte.")]
    public bool Locked { get; set; }

    [DisplayName("Feld bestimmt die Legende")]
    [Description("Besitzt das Editierthema in der Karte eine Legende mit unterschiedlichen Symbolen und ist das Symbol abhängig vom Wert dieses Feldes, kann diese Option gesetzt werden. Der Anwender hat dann über die Auswahlliste die Möglichkeit, neben dem Tabellenwert auch das (Legenden) Symbol auszuwählen.")]
    public bool LegendField { get; set; }

    [DisplayName("Beständig (Resistant)")]
    [Description("Der Wert des Feldes bleibt nach dem Speichern erhalten und muss nicht jedes Mal vom Anwender neu eingeben werden. Das gilt auch, wenn es das gleiche Feld in unterschiedlichen Themen gibt. ZB muss so eine Projektnummer, die auf jedem Objekt mitverspeichert wird nur einmal eingeben werden und bleibt im Formular 'beständig', bis der Anwender eine andere Projektnummer vergibt.")]
    public bool Resistant { get; set; }

    [DisplayName("Feld für Massen-Attributierung")]
    [Description("Wird Massen-Attributierung (alle ausgewählten Objekte ändern) erlaubt, kann hier angegeben werden, ob das Feld über Massen-Attributierung gesetzt werden darf.")]
    public bool MassAttributable { get; set; }

    [DisplayName("Schreibgeschützt (Readonly)")]
    [Description("Hier wird das Feld zwar angezeigt, kann vom Anwender aber nicht geändert werden. Readonly Felder werden beim Speichern NICHT in die Datenbank geschrieben und dienen nur informativen Zwecken. Ausnahme: Readonly Felder, für die ein AutoValue angegeben wird, werden beim Speichern auch in die Datenbank geschrieben.")]
    public bool Readonly { get; set; }

    [Category("~Validierung")]
    [DisplayName("Clientseitige Validierung")]
    [Description("Die Validierung erfolgt bereichts am Client bei der Eingabe bzw. spätestens beim Klick auf den Speichern Button. Dadurch ergibt sich in der Regel eine besser 'User Experience'")]
    public bool ClientsideValidation { get; set; }

    [Category("~Validierung")]
    [DisplayName("Erforderlich (requried)")]
    [Description("Gibt an, dass für dieses Feld eine Eingabe vom Anwender erfolgen muss")]
    public bool Required { get; set; }

    [Category("~Validierung")]
    [DisplayName("Minimale Eingabe-Länge")]
    [Description("Gibt die minimale Eingabe von Zeichen an, die ein Anwender eingeben muss")]
    public int MinLength { get; set; }

    [Category("~Validierung")]
    [DisplayName("Regulärer Ausdruch (Regex)")]
    [Description("Hier kann ein regulärer Ausdruck angegeben werden. Ein Objekt kann nur erstellt werden, wenn die Eingabe des Benutzers für dieses Feld, dem regulären Ausdruck entspricht")]
    public string RegexPattern { get; set; }

    [Category("~Validierung")]
    [DisplayName("Validierungsfehlermeldung")]
    [Description("Kommt es bei der Validierung eines Feldes zu einem Fehler, wird dem Anwender dieser Text angezeigt. Hier können/sollten auch Beispiele für korrekte Eingaben angeführt werden.")]

    public string ValidationErrorMessage { get; set; }

    [Category("~Autovalue")]
    [DisplayName("Auto Value")]
    public EditingFieldAutoValue AutoValue { get; set; }

    [Category("~Autovalue")]
    [DisplayName("Benuterdefinierter Auto Value (custom, db_select=ConnectionString)")]
    [Description(@"Hier kann zum Beispiel ein benutzerdefinierter AutoValue für eine räumliche Abfrage (gnr from grunstuecke) eingetragen werden. 
Aus Autovalue in der Liste muss in diesem Fall 'custom' ausgewählt werden, 'grundsuecke' wäre eine Abfrage. Verwendet man den AutoValue 'db_select', muss hier der Connection String zu Datenbank eingetragen werden.
Custom Autovalues können mit = beginnen. Der Wert wird dadurch immer gesetzt.
Mit dem Prefix mask-insert-default:: wird der Wert vom System gesetzt, sondern erscheint als Default Wert in der Eingabemaske, wenn ein neues Objekt erstellt wird.")]
    public string CustomAutoValue { get; set; }

    [Category("~Autovalue")]
    [DisplayName("Benutzerdefinerter Auto Value 2 (zb: db_select=SqlStatement")]
    [Description("Manche Autovalues benötigen weitere Parameter. zB. bei 'db_select' muss hier ein SQL Statement eingetragen werden, über das der entsprechende Wert ermittelt wird. Das Statement muss dabei so formuliert werden, dass exakt ein Ergebniss (ein Wert, ein Record) entsteht. Als Platzhalter für bestehende Felder muss {{..}} verwendet werden, zB select gnr from grst objectid={{id}}. Achtung: Um Sql Injection vorzubeugen, werden die Platzhalter im Statement in Parameter umgewandelt. Daher dürfen hier keine Hochkomma rund um die Platzhalter im Statement verwendet werden, auch wenn das entsprechende Feld ein String ist!")]
    public string CustomAutoValue2 { get; set; }

    [Category("~optional: Database Domain")]
    [DisplayName("Connection String")]
    public string DbDomainConnectionString { get; set; }
    [Category("~optional: Database Domain")]
    [DisplayName("Db-Tabelle (Table)")]
    public string DbDomainTable { get; set; }
    [Category("~optional: Database Domain")]
    [DisplayName("Db-Feld (Field)")]
    public string DbDomainField { get; set; }
    [Category("~optional: Database Domain")]
    [DisplayName("Db-Anzeige Feld (Alias)")]
    public string DbDomainAlias { get; set; }
    [Category("~optional: Database Domain")]
    [DisplayName("Db-Where Klausel (WHERE)")]
    [Description("Hier kann die Auswahlliste weiter eingeschränkt werden. Das kann über einen statischer Ausdruck erfolten (wenn die gleiche Tabelle für unterschiedliche Auswahlisten verwendet wird) oder über einen dynamische Ausdruck (XYZ='{{role-parameter:...}}', um beispielsweise eine Auswahlliste für eine Bestimmte Benutzergruppe einzuschränken.")]
    public string DbDomainWhere { get; set; }
    [Category("~optional: Database Domain")]
    [DisplayName("Db-Orderby (Field)")]
    public string DbOrderBy { get; set; }

    [Category("~optional: Domain List")]
    [DisplayName("Db-Anzeige Feld (Alias)")]
    public string DomainList { get; set; }

    [Category("~optional: Domain Behaviour (experimental)")]
    [DisplayName("Pro Behaviour")]
    [Description("Gibt an, ob die Auswahlliste ein erweitertes Verhalten haben soll. Das erweiterte Verhalten ist von der WebGIS Instanz Konfiguration abhängig. (setzt man in der custom.js: webgis.usability.select_pro_behaviour = \"select2\"; ist für diese Auswahlliste die Suche nach items möglich. Das macht bei Auswahllisten mit vielen Items die Eingabe leichter.")]
    public bool DomainProBehaviour { get; set; }


    [Category("~optional: Attribute Picker")]
    [DisplayName("Attribute Picker Abfrage")]
    [Description("Die Abfrage, von der ein Attribute geholt werden soll. Format service-id@query-id")]
    public string AttributePickerQuery { get; set; }
    [Category("~optional: Attribute Picker")]
    [DisplayName("Attribute Picker Feld")]
    [Description("Das Feld aus der Abfrage, das beim Attribute Picking übernommen werden soll.")]
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
