using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.Extensions;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public enum ImportEditFields
{
    None,
    Fields
}

public class EditingTheme : CopyableNode, IUI, ICreatable, IEditable, IDisplayName
{
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public EditingTheme(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        this.ThemeId = NewThemeId();
        this.Visible = true;

        this.AllowInsert = this.AllowUpdate = this.AllowDelete = this.AllowEditGeometry = true;
        this.AllowMultipartGeometries = true;
        this.ShowSaveButton = this.ShowSaveAndSelectButton = true;
    }

    #region Properties

    [DisplayName("Sichtbar")]
    public bool Visible { get; set; }

    [DisplayName("Über Edit-Server verfügbar")]
    [Description("Wenn das Editthema nicht nur die Editwerkzeuge des WebGIS-Kartenviewers verfügbar sein sollten, sondern auch über den Collector (App-Builder), muss diese Option gesetzt werden.")]
    public bool EnableEditServer { get; set; }

    [DisplayName("Räumliches Bezugssystem (EPSG-Code)")]
    [Description("Hier muss das Koordinatensystem angeben werden, in dem die Daten in der Datenbank vorliegen! Wenn kein Bezugssystem angegeben wird, kann das Editthema nicht im Viewer ausgewählt werden.")]
    public int Srs { get; set; }

    [DisplayName("Tags (optional)")]
    [Description("Tags, über die ein Editthema klassifiziert werden kann. Mit Beistrich getrennte Liste anführen.")]
    public string Tags { get; set; }

    [Category("~Rechte")]
    [DisplayName("INSERT (neu anlegen) erlauben")]
    public bool AllowInsert { get; set; }

    [Category("~Rechte")]
    [DisplayName("UPDATE (bestehendes bearbeiten) erlauben")]
    public bool AllowUpdate { get; set; }

    [Category("~Rechte")]
    [DisplayName("DELETE (bestehendes löschen) erlauben")]
    public bool AllowDelete { get; set; }

    [Category("~Rechte")]
    [DisplayName("Geometrie: bearbeiten erlauben")]
    public bool AllowEditGeometry { get; set; }

    [Category("~Rechte")]
    [DisplayName("Geometrie: Multiparts erstellen erlauben")]
    public bool AllowMultipartGeometries { get; set; }

    [Category("~Rechte")]
    [DisplayName("Massenattributierung erlauben")]
    public bool AllowMassAttributation { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("Speichern Button anzeigen")]
    [Description("Gibt an, ober der 'Speichern' Button in der Erstellungsmaske angeboten wird.")]
    public bool ShowSaveButton { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("Speichern und Selektieren (Auswählen) Button anzeigen")]
    [Description("Gibt an, ober der 'Speichern und Auswählen' Button in der Erstellungsmaske angeboten wird.")]
    public bool ShowSaveAndSelectButton { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("1. Erweiterte Speicheraktion (optional)")]
    [Description("Für zusätzliche Buttons, die beim Speichern angeboten werden. Damit ein entsprechender Button angezeigt wird, muss hier eine Aktion gewählt und eine Text für den Button vergeben werden. Durch die ersten beiden Optionen (Save und SaveAndSelect) können die hier oben angeführten vordefinerten Aktionen überschreiben und mit einem anderen Button Text dargestellt werden.")]
    public EditingInsertAction InsertAction1 { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("1. Erweiterte Speicheraktion (Text)")]
    [Description("Text, der für diese Akton im Button angezeigt wird.")]
    public string InsertActionText1 { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("2. Erweiterte Speicheraktion (optional)")]
    [Description("Wie 'Erweiterte Speicheraktion 1'")]
    public EditingInsertAction InsertAction2 { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("2. Erweiterte Speicheraktion (Text)")]
    [Description("Wie 'Erweiterte Speicheraktion 1'")]
    public string InsertActionText2 { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("3. Erweiterte Speicheraktion (optional)")]
    [Description("Wie 'Erweiterte Speicheraktion 1'")]
    public EditingInsertAction InsertAction3 { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("3. Erweiterte Speicheraktion (Text)")]
    [Description("Wie 'Erweiterte Speicheraktion 1'")]
    public string InsertActionText3 { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("4. Erweiterte Speicheraktion (optional)")]
    [Description("Wie 'Erweiterte Speicheraktion 1'")]
    public EditingInsertAction InsertAction4 { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("4. Erweiterte Speicheraktion (Text)")]
    [Description("Wie 'Erweiterte Speicheraktion 1'")]
    public string InsertActionText4 { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("5. Erweiterte Speicheraktion (optional)")]
    [Description("Wie 'Erweiterte Speicheraktion 1'")]
    public EditingInsertAction InsertAction5 { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("5. Erweiterte Speicheraktion (Text)")]
    [Description("Wie 'Erweiterte Speicheraktion 1'")]
    public string InsertActionText5 { get; set; }

    [Category("~Aktionen (Insert)")]
    [DisplayName("Auto Explode Multipart Features")]
    [Description("Zeichnet der Anwender Multipart (auch Fan-Geometrie) Features, werden diese beim Speichern automatisch auf mehere Objekte aufgeteilt.")]
    public bool AutoExplodeMultipartFeatuers { get; set; }

    //[Browsable(false)]
    //[ReadOnly(true)]
    [Category("~Erweiterte Eigenschaften")]
    [DisplayName("Interne ThemeId")]
    [Description("Die ThemeId muss für eine Editthema eindeutig sein und sollte nicht mehr geändert werden, wenn ein Thema produktiv eingebunden wird. Die Vergabe einer eindeutigen Id wird beim erstellen eines Themas automatisch vergeben. Für bestimmte Aufgaben macht es Sinn, für diese Id einen sprechenden Namen zu vergeben (z.B. wenn das Editthema über eine Collector App außerhalb des Kartenviewers verwendet wird). Hier muss allerdings immer darauf geachtet werden, dass dieser Wert für alle Themen eindeutig bleibt. Dieser Wert sollte nur von versierten Administratoren geändert werden!!!")]
    public string ThemeId { get; set; }

    [Browsable(false)]
    internal string EditingThemeId { get; set; }

    [Browsable(false)]
    internal ImportEditFields AutoImportEditFields { get; set; }

    //[Browsable(false)]
    //internal JsonFeatureLayer JsonFeatureLayer { get; set; }

    #endregion

    private string NewThemeId()
    {
        return Guid.NewGuid().ToString("N").ToLower();
    }

    #region IPersistable

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.ThemeId = (string)stream.Load("themeid", NewThemeId());
        this.Visible = (bool)stream.Load("visible", true);
        this.EnableEditServer = (bool)stream.Load("editservice", false);
        this.Srs = (int)stream.Load("srs", 0);
        this.Tags = (string)stream.Load("tags", String.Empty);

        this.AllowInsert = (bool)stream.Load("allow_insert", true);
        this.AllowUpdate = (bool)stream.Load("allow_update", true);
        this.AllowDelete = (bool)stream.Load("allow_delete", true);
        this.AllowEditGeometry = (bool)stream.Load("allow_editgeometry", true);
        this.AllowMassAttributation = (bool)stream.Load("allow_massattributation", false);
        this.AllowMultipartGeometries = (bool)stream.Load("allow_multipartgeometries", true);

        this.ShowSaveButton = (bool)stream.Load("show_save_button", true);
        this.ShowSaveAndSelectButton = (bool)stream.Load("show_save_and_select_button", true);

        this.InsertAction1 = (EditingInsertAction)(int)stream.Load("insert_action1", (int)EditingInsertAction.None);
        this.InsertAction2 = (EditingInsertAction)(int)stream.Load("insert_action2", (int)EditingInsertAction.None);
        this.InsertAction3 = (EditingInsertAction)(int)stream.Load("insert_action3", (int)EditingInsertAction.None);
        this.InsertAction4 = (EditingInsertAction)(int)stream.Load("insert_action4", (int)EditingInsertAction.None);
        this.InsertAction5 = (EditingInsertAction)(int)stream.Load("insert_action5", (int)EditingInsertAction.None);

        this.InsertActionText1 = (string)stream.Load("insert_action_text1", String.Empty);
        this.InsertActionText2 = (string)stream.Load("insert_action_text2", String.Empty);
        this.InsertActionText3 = (string)stream.Load("insert_action_text3", String.Empty);
        this.InsertActionText4 = (string)stream.Load("insert_action_text4", String.Empty);
        this.InsertActionText5 = (string)stream.Load("insert_action_text5", String.Empty);

        this.AutoExplodeMultipartFeatuers = (bool)stream.Load("auto_explode_multipart_features", false);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("themeid", this.ThemeId ?? NewThemeId());
        stream.Save("visible", this.Visible);
        stream.Save("editservice", this.EnableEditServer);
        stream.Save("srs", this.Srs);
        stream.Save("tags", String.IsNullOrWhiteSpace(this.Tags)
            ? ""
            : String.Join(",", this.Tags.Split(",").Select(t => t.Trim()).Where(t => !String.IsNullOrEmpty(t)))
        );

        stream.Save("allow_insert", this.AllowInsert);
        stream.Save("allow_update", this.AllowUpdate);
        stream.Save("allow_delete", this.AllowDelete);
        stream.Save("allow_editgeometry", this.AllowEditGeometry);
        stream.Save("allow_massattributation", this.AllowMassAttributation);
        stream.Save("allow_multipartgeometries", this.AllowMultipartGeometries);

        stream.Save("show_save_button", this.ShowSaveButton);
        stream.Save("show_save_and_select_button", this.ShowSaveAndSelectButton);

        stream.Save("insert_action1", (int)this.InsertAction1);
        stream.Save("insert_action2", (int)this.InsertAction2);
        stream.Save("insert_action3", (int)this.InsertAction3);
        stream.Save("insert_action4", (int)this.InsertAction4);
        stream.Save("insert_action5", (int)this.InsertAction5);

        stream.SaveOrRemoveIfEmpty("insert_action_text1", this.InsertActionText1);
        stream.SaveOrRemoveIfEmpty("insert_action_text2", this.InsertActionText2);
        stream.SaveOrRemoveIfEmpty("insert_action_text3", this.InsertActionText3);
        stream.SaveOrRemoveIfEmpty("insert_action_text4", this.InsertActionText4);
        stream.SaveOrRemoveIfEmpty("insert_action_text5", this.InsertActionText5);

        stream.Save("auto_explode_multipart_features", this.AutoExplodeMultipartFeatuers);
    }

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NewEditThemeControl(_servicePack, this);
        ip.InitParameter = this;

        return ip;
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

    async public Task<bool> CreatedAsync(string fullName)
    {
        var serviceLayer = this.CmsManager.SchemaNodeInstances(_servicePack, this.RelativePath.TrimAndAppendSchemaNodePath(3, "Themes"), true)
            .Where(o => o is ServiceLayer && ((ServiceLayer)o).Id == this.EditingThemeId)
            .FirstOrDefault() as ServiceLayer;

        if (serviceLayer != null)  // Link to EditingTheme
        {
            var link = new Link(serviceLayer.RelativePath);

            string newLinkName = Helper.NewLinkName();
            IStreamDocument xmlStream = DocumentFactory.New(this.CmsManager.ConnectionString);
            link.Save(xmlStream);
            xmlStream.SaveDocument(this.CmsManager.ConnectionString + "/" + this.RelativePath.TrimAndAppendSchemaNodePath(1, "EditingTheme") + "/" + newLinkName);

            var objects = this.CmsManager.SchemaNodeInstances(_servicePack, this.RelativePath.TrimAndAppendSchemaNodePath(1, "EditingTheme"), true);

            if (AutoImportEditFields == ImportEditFields.Fields)
            {
                var nameurlControl = new NameUrlControl();

                var category = new EditingFieldCategory()
                {
                    Name = "Allgemein",
                    IsDefault = true
                };

                string categoryCreateAs = category.CreateAs(true).Replace("\\", "/");
                string categoryUrl = categoryCreateAs.Split('/')[0];

                xmlStream = DocumentFactory.New(this.CmsManager.ConnectionString);
                category.Save(xmlStream);
                xmlStream.SaveDocument(this.CmsManager.ConnectionString + "/" + this.RelativePath.TrimAndAppendSchemaNodePath(1, "EditingFields") + "/" + categoryCreateAs + ".xml");

                List<string> urls = new List<string>();
                foreach (var jsonFeatureField in await objects.FieldsNames(this.CmsManager, _servicePack, onlyEditable: true))
                {
                    var field = new EditingField(_servicePack)
                    {
                        Name = jsonFeatureField.Aliasname.OrTake(jsonFeatureField.Name),
                        Url = nameurlControl.NameToUrl(jsonFeatureField.Name),
                        FieldName = jsonFeatureField.Name
                    };

                    if (jsonFeatureField.HasDomain)
                    {
                        field.FieldType = EditingFieldType.Domain;
                    }

                    string fieldCreateAs = field.CreateAs(true).Replace("\\", "/");
                    xmlStream = DocumentFactory.New(this.CmsManager.ConnectionString);
                    field.Save(xmlStream);
                    xmlStream.SaveDocument(this.CmsManager.ConnectionString + "/" + this.RelativePath.TrimAndAppendSchemaNodePath(1, "EditingFields") + "/" + categoryUrl + "/" + fieldCreateAs + ".xml");

                    urls.Add(fieldCreateAs + ".xml");
                }

                var itemOrder = new ItemOrder(this.CmsManager.ConnectionString + "/" + this.RelativePath.TrimAndAppendSchemaNodePath(1, "EditingFields") + "/" + categoryUrl);
                itemOrder.Items = urls.ToArray();
                itemOrder.Save();
            }
        }

        return true;
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get
        {
            return String.IsNullOrWhiteSpace(this.Tags)
                ? $"{Name}"
                : $"{Name} ({String.Join(", ", this.Tags.Split(',').Select(t => $"#{t.Trim()}"))})";
        }
    }

    #endregion

    protected override void BeforeCopy()
    {
        base.BeforeCopy();
        this.ThemeId = NewThemeId();
    }

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Editing Theme"; }
    }
}
