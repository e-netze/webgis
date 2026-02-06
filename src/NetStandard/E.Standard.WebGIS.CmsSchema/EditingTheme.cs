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

    [DisplayName("#visible")]
    public bool Visible { get; set; }

    [DisplayName("#enable_edit_server")]
    public bool EnableEditServer { get; set; }

    [DisplayName("#srs")]
    public int Srs { get; set; }

    [DisplayName("#tags")]
    public string Tags { get; set; }

    [Category("~#category_allow_insert")]
    [DisplayName("#allow_insert")]
    public bool AllowInsert { get; set; }

    [Category("~#category_allow_update")]
    [DisplayName("#allow_update")]
    public bool AllowUpdate { get; set; }

    [Category("~#category_allow_delete")]
    [DisplayName("#allow_delete")]
    public bool AllowDelete { get; set; }

    [Category("~#category_allow_edit_geometry")]
    [DisplayName("#allow_edit_geometry")]
    public bool AllowEditGeometry { get; set; }

    [Category("~#category_allow_multipart_geometries")]
    [DisplayName("#allow_multipart_geometries")]
    public bool AllowMultipartGeometries { get; set; }

    [Category("~#category_allow_mass_attributation")]
    [DisplayName("#allow_mass_attributation")]
    public bool AllowMassAttributation { get; set; }

    [Category("~#category_show_save_button")]
    [DisplayName("#show_save_button")]
    public bool ShowSaveButton { get; set; }

    [Category("~#category_show_save_and_select_button")]
    [DisplayName("#show_save_and_select_button")]
    public bool ShowSaveAndSelectButton { get; set; }

    [Category("~#category_insert_action1")]
    [DisplayName("#insert_action1")]
    public EditingInsertAction InsertAction1 { get; set; }

    [Category("~#category_insert_action_text1")]
    [DisplayName("#insert_action_text1")]
    public string InsertActionText1 { get; set; }

    [Category("~#category_insert_action2")]
    [DisplayName("#insert_action2")]
    public EditingInsertAction InsertAction2 { get; set; }

    [Category("~#category_insert_action_text2")]
    [DisplayName("#insert_action_text2")]
    public string InsertActionText2 { get; set; }

    [Category("~#category_insert_action3")]
    [DisplayName("#insert_action3")]
    public EditingInsertAction InsertAction3 { get; set; }

    [Category("~#category_insert_action_text3")]
    [DisplayName("#insert_action_text3")]
    public string InsertActionText3 { get; set; }

    [Category("~#category_insert_action4")]
    [DisplayName("#insert_action4")]
    public EditingInsertAction InsertAction4 { get; set; }

    [Category("~#category_insert_action_text4")]
    [DisplayName("#insert_action_text4")]
    public string InsertActionText4 { get; set; }

    [Category("~#category_insert_action5")]
    [DisplayName("#insert_action5")]
    public EditingInsertAction InsertAction5 { get; set; }

    [Category("~#category_insert_action_text5")]
    [DisplayName("#insert_action_text5")]
    public string InsertActionText5 { get; set; }

    [Category("~#category_auto_explode_multipart_featuers")]
    [DisplayName("#auto_explode_multipart_featuers")]
    public bool AutoExplodeMultipartFeatuers { get; set; }

    //[Browsable(false)]
    //[ReadOnly(true)]
    [Category("~#category_theme_id")]
    [DisplayName("#theme_id")]
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
                var nameurlControl = new NameUrlControl(_servicePack);

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
