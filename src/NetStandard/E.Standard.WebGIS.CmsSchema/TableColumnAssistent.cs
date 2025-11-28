using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.UI;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class TableColumnAssistent : SchemaNode, IAutoCreatable, IUI
{
    TableColumnAssistentControl _ctrl = null;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public TableColumnAssistent(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;
    }

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

    #region IAutoCreatable Member

    public bool AutoCreate()
    {
        if (_ctrl == null)
        {
            return false;
        }

        string path = this.CmsManager.ConnectionString + @"/" + this.RelativePath.TrimRightRelativeCmsPath(1).Replace(@"\", @"/");
        foreach (TableColumnAssistentControl.Field field in _ctrl.SelectedFields)
        {
            TableColumn tabCol = new TableColumn();
            tabCol.ColumnType = ColumnType.Field;
            tabCol.Data = new TableColumn.FieldDataField();
            ((TableColumn.FieldDataField)tabCol.Data).FieldName = field.FieldName;
            tabCol.Name = field.AliasName;

            string fullName = path + @"/" + tabCol.CreateAs(false) + ".xml";

            IStreamDocument xmlStream = DocumentFactory.New(path);
            tabCol.Save(xmlStream);
            xmlStream.SaveDocument(fullName);
            tabCol.CreatedAsync(fullName).Wait();
        }
        return true;
    }

    #endregion
}
