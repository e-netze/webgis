using E.Standard.CMS.Core.Schema.Abstraction;
using Newtonsoft.Json;
using System.ComponentModel;

namespace E.Standard.CMS.Core.Schema;

public class SchemaNode : ISchemaNode
{
    private string _relativePath = string.Empty;
    private CMSManager _manager = null;

    #region ISchemaNode Member
    [Browsable(false)]
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    virtual public string RelativePath
    {
        get
        {
            return _relativePath;
        }
        set
        {
            _relativePath = value;
        }
    }
    [Browsable(false)]
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    virtual public CMSManager CmsManager
    {
        get { return _manager; }
        set { _manager = value; }
    }

    #endregion
}
