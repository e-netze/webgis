using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.Security.Reflection;
using System;
using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public class DocumentManagement : SchemaNode, IEditable
{
    private bool _use = false;
    private string _idField = string.Empty;
    private string _schema = String.Empty;
    private bool _allowupload = false;

    #region Properties
    [Browsable(true)]
    [DisplayName("#use")]
    [Category("#category_use")]
    [AuthorizablePropertyAttribute("use", false)]
    public bool Use
    {
        get { return _use; }
        set { _use = value; }
    }
    [Browsable(true)]
    [DisplayName("#id_field_name")]
    [Category("#category_id_field_name")]
    [Editor(typeof(TypeEditor.ThemeFieldsEditor), typeof(TypeEditor.ITypeEditor))]
    public string IdFieldName
    {
        get { return _idField; }
        set { _idField = value; }
    }
    [Browsable(true)]
    [DisplayName("#schema")]
    [Category("#category_schema")]
    [Editor(typeof(TypeEditor.DocumentManagementServerSchemaEditor),
        typeof(TypeEditor.ITypeEditor))]
    public string Schema
    {
        get { return _schema; }
        set { _schema = value; }
    }
    [Browsable(true)]
    [DisplayName("#allow_upload")]
    [Category("#category_allow_upload")]
    [AuthorizablePropertyAttribute("allowupload", false)]
    public bool AllowUpload
    {
        get { return _allowupload; }
        set { _allowupload = value; }
    }
    #endregion

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {
        _use = (bool)stream.Load("use", false);
        _idField = (string)stream.Load("idfield", String.Empty);
        _schema = (string)stream.Load("schema", String.Empty);
        _allowupload = (bool)stream.Load("allowupload", false);
    }

    public void Save(IStreamDocument stream)
    {
        stream.Save("use", _use, false);
        stream.Save("idfield", _idField);
        stream.Save("schema", _schema);
        stream.Save("allowupload", _allowupload, false);
    }

    #endregion
}
