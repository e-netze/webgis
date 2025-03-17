using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;
using System;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class LookUpEditor : UserControl, IUITypeEditor
{
    public LookUpEditor()
    {
        this.AddControl(_txtConnectionString);
        this.AddControl(_txtSqlStatement);
    }

    #region UI

    Input _txtConnectionString = new Input("txtConnectionString") { Label = "Connection String" };
    TextArea _txtSqlStatement = new TextArea("txtSqlStatement") { Label = "SqlStatement" };

    #endregion

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        if (context.Value is LookUp)
        {
            _txtConnectionString.Value = ((LookUp)context.Value).ConnectionString;
            _txtSqlStatement.Value = ((LookUp)context.Value).SqlClause;
        }

        return this;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get
        {
            if (String.IsNullOrWhiteSpace(_txtConnectionString.Value) && String.IsNullOrWhiteSpace(_txtSqlStatement.Value))
            {
                return null;
            }

            return new LookUp(_txtConnectionString.Value, _txtSqlStatement.Value);
        }
    }
}
