using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CMS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class VisFilterLookupEditor : UserControl, IUITypeEditor
{
    public VisFilterLookupEditor()
    {

    }

    #region Overrides 

    public override IUIControl GetControl(string name)
    {
        var control = base.GetControl(name);
        if (control != null)
        {
            return control;
        }

        if (name.StartsWith("txt_"))
        {
            var txtControl = new Input(name);
            this.AddControl(txtControl);
            return txtControl;
        }
        if (name.StartsWith("cmb_"))
        {
            var cmbControl = new ComboBox(name);
            this.AddControl(cmbControl);
            return cmbControl;
        }

        return null;
    }

    #endregion

    #region IUITypeEditor

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        int index = 0;

        var keyFieldOptions = new List<ComboBox.Option>();
        keyFieldOptions.Add(new ComboBox.Option(String.Empty));

        if (context.Instance is VisFilter && !String.IsNullOrWhiteSpace(((VisFilter)context.Instance).Filter))
        {
            foreach (Match match in Regex.Matches(((VisFilter)context.Instance).Filter, @"\[(.*?)\]"))
            {
                var matchKey = match.Value.Substring(1, match.Value.Length - 2);
                if (!String.IsNullOrWhiteSpace(matchKey))
                {
                    keyFieldOptions.Add(new ComboBox.Option(matchKey));
                }
            }
        }

        if (context.Value is VisFilter.LookupTable[])
        {
            var lookupTables = (VisFilter.LookupTable[])context.Value;

            foreach (var lookupTable in lookupTables)
            {
                GroupBox gb = new GroupBox() { Label = "Auswahlliste: " + lookupTable.Key, Collapsed = true, ItemUrl = lookupTable.Key };

                var comboKey = new ComboBox("cmb_Key_" + index) { Label = "Key-Field *" };
                comboKey.Options.AddRange(keyFieldOptions);
                comboKey.Value = lookupTable.Key;
                gb.AddControl(comboKey);

                gb.AddControl(new Input("txt_ConnectionString_" + index) { Label = "Connectin String", Value = lookupTable.LookUp?.ConnectionString });
                gb.AddControl(new TextArea("txt_SqlStatement_" + index) { Label = "Sql Statement", Value = lookupTable.LookUp?.SqlClause });

                var comboLookupType = new ComboBox("cmb_Type_" + index)
                {
                    Label = "Lookup Type"
                };
                foreach (var lookupType in Enum.GetNames(typeof(Lookuptype)))
                {
                    comboLookupType.Options.Add(new ComboBox.Option(lookupType));
                }
                comboLookupType.Value = lookupTable.LookupType.ToString();

                gb.AddControl(comboLookupType);

                this.AddControl(gb);
                index++;
            }
        }

        GroupBox @new = new GroupBox() { Label = "Neue Auswahlliste", Collapsed = index > 0 };

        var newComboKey = new ComboBox("cmb_Key_") { Label = "Key-Field *" };
        newComboKey.Options.AddRange(keyFieldOptions);
        newComboKey.Value = String.Empty;
        @new.AddControl(newComboKey);

        @new.AddControl(new Input("txt_ConnectionString_") { Label = "Connectin String" });
        @new.AddControl(new TextArea("txt_SqlStatement_") { Label = "Sql Statement" });

        var comboLookupTypeNew = new ComboBox("cmb_Type_")
        {
            Label = "Lookup Type"
        };
        foreach (var lookupType in Enum.GetNames(typeof(Lookuptype)))
        {
            comboLookupTypeNew.Options.Add(new ComboBox.Option(lookupType));
        }
        @new.AddControl(comboLookupTypeNew);
        this.AddControl(@new);

        if (index > 0)
        {
            this.AddControl(new InfoText()
            {
                Text = "Tipp: Um bestehende Editthemen zu löschen, im jeweiligen Thema das Feld 'Key-Field' leeren und diesen Dialog mit 'Übernehmen' bestätigen."
            });
        }

        return this;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get
        {
            var lookupTables = new List<VisFilter.LookupTable>();

            int index = 0;
            while (true)
            {
                if (this.ChildControls.Where(c => c.Name == "cmb_Key_" + index).FirstOrDefault() == null)
                {
                    break;
                }

                string key = ((IInputUIControl)GetControl("cmb_Key_" + index))?.Value;
                string connectinoString = ((IInputUIControl)GetControl("txt_ConnectionString_" + index))?.Value;
                string sqlStatement = ((IInputUIControl)GetControl("txt_SqlStatement_" + index))?.Value;
                Lookuptype lookupType = (Lookuptype)Enum.Parse(typeof(Lookuptype), ((IInputUIControl)GetControl("cmb_Type_" + index))?.Value);

                if (!String.IsNullOrWhiteSpace(key))
                {
                    lookupTables.Add(new VisFilter.LookupTable()
                    {
                        Key = key,
                        LookupType = lookupType,
                        LookUp = new LookUp()
                        {
                            ConnectionString = connectinoString,
                            SqlClause = sqlStatement
                        }
                    });
                }

                index++;
            }

            // Neues Objekt übernehmen
            string newKey = ((IInputUIControl)GetControl("cmb_Key_"))?.Value;
            string newConnectinoString = ((IInputUIControl)GetControl("txt_ConnectionString_"))?.Value;
            string newSqlStatement = ((IInputUIControl)GetControl("txt_SqlStatement_"))?.Value;
            Lookuptype newLookupType =
                String.IsNullOrWhiteSpace(((IInputUIControl)GetControl("cmb_Type_"))?.Value) ?
                Lookuptype.ComboBox :
                (Lookuptype)Enum.Parse(typeof(Lookuptype), ((IInputUIControl)GetControl("cmb_Type_"))?.Value);

            if (!String.IsNullOrWhiteSpace(newKey))
            {
                lookupTables.Add(new VisFilter.LookupTable()
                {
                    Key = newKey,
                    LookupType = newLookupType,
                    LookUp = new LookUp()
                    {
                        ConnectionString = newConnectinoString,
                        SqlClause = newSqlStatement
                    }
                });
            }

            return lookupTables.Count > 0 ? lookupTables.ToArray() : null;
        }
    }

    #endregion
}
