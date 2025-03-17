using Newtonsoft.Json;
using System.ComponentModel;

namespace E.Standard.CMS.Core.Schema;

public class Creatable : SchemaNode
{
    private bool _create = false;

    [Browsable(false)]
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Create
    {
        get { return _create; }
        protected set { _create = value; }
    }
}
