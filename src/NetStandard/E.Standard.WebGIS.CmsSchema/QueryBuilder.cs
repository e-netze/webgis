using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Reflection;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CmsSchema.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class QueryBuilderField : CopyableXml, IDisplayName
{
    public QueryBuilderField()
    {
        base.Url = Guid.NewGuid().ToString("N").ToLowerInvariant();
    }

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

    [CmsPersistable("alias")]
    public string Aliasname { get; set; }

    [Browsable(false)]
    public string DisplayName => this.Name;

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "QueryBuilderField"; }
    }
}

public class QueryBuilderFieldAssistent : SchemaNode, IAutoCreatable, IUI
{
    private TableColumnAssistentControl _ctrl = null;
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public QueryBuilderFieldAssistent(CmsItemTransistantInjectionServicePack servicePack)
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

        string path = this.CmsManager.ConnectionString + @"/" + Helper.TrimPathRight(this.RelativePath, 1);

        List<String> existingFieldnames = new();
        foreach (var item in DocumentFactory.PathInfo(path).GetFiles())
        {
            IStreamDocument xmlStream = DocumentFactory.Open(item.FullName);
            var existingField = new QueryBuilderField();
            existingField.Load(xmlStream);
            if (String.IsNullOrEmpty(existingField.Name)) continue;

            existingFieldnames.Add(existingField.Name);
        }

        foreach (TableColumnAssistentControl.Field field in _ctrl.SelectedFields)
        {
            if(existingFieldnames.Contains(field.FieldName))
            {
                continue;
            }

            QueryBuilderField qbField = new QueryBuilderField();
            qbField.Name = field.FieldName;
            qbField.Aliasname = field.FieldName == field.AliasName 
                ? ""
                : field.AliasName;

            string fullName = path + @"/" + qbField.CreateAs(false) + ".xml";

            IStreamDocument xmlStream = DocumentFactory.New(path);
            qbField.Save(xmlStream);
            xmlStream.SaveDocument(fullName);
            qbField.CreatedAsync(fullName).Wait();
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