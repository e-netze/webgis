using E.Standard.CMS.Core.UI.Abstraction;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.CMS.UI.Controls;

public class Control : IUIControl
{
    public Control(string name)
    {
        this.Name = name;
        this.Visible = true;
        this.Enabled = true;
    }

    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "visible")]
    [System.Text.Json.Serialization.JsonPropertyName("visible")]
    public bool Visible { get; set; }

    [JsonProperty(PropertyName = "enabled")]
    [System.Text.Json.Serialization.JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonProperty(PropertyName = "type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string TypeName
    {
        get
        {
            string typeName = this.GetType().ToString().ToLower();
            if (typeName.Contains("."))
            {
                typeName = typeName.Substring(typeName.LastIndexOf(".") + 1);
            }

            return typeName.Replace("+", ".");
        }
    }

    [JsonProperty(PropertyName = "isClickable")]
    [System.Text.Json.Serialization.JsonPropertyName("isClickable")]
    public bool? IsClickable { get; set; }

    #region IUIControl

    private List<IUIControl> _childControls = null;

    public void InsertControl(int index, IUIControl control)
    {
        if (_childControls == null)
        {
            _childControls = new List<IUIControl>();
        }

        _childControls.Insert(Math.Min(_childControls.Count, index), control);
    }

    public void AddControl(IUIControl control)
    {
        if (_childControls == null)
        {
            _childControls = new List<IUIControl>();
        }

        _childControls.Add(control);
    }

    public void AddControls(IEnumerable<IUIControl> controls)
    {
        if (_childControls == null)
        {
            _childControls = new List<IUIControl>();
        }

        _childControls.AddRange(controls);
    }

    [JsonProperty(PropertyName = "controls")]
    [System.Text.Json.Serialization.JsonPropertyName("controls")]
    virtual public IEnumerable<IUIControl> ChildControls { get { return _childControls; } }

    virtual public IUIControl GetControl(string name)
    {
        if (ChildControls == null)
        {
            return null;
        }

        foreach (var control in this.ChildControls)
        {
            if (control.Name == name)
            {
                return control;
            }

            var childControl = control.GetControl(name);
            if (childControl != null)
            {
                return childControl;
            }
        }

        return null;
    }

    public IEnumerable<IUIControl> AllControls()
    {
        List<IUIControl> controls = new List<IUIControl>();
        controls.Add(this);
        AllControls(this, controls);

        return controls;
    }

    private void AllControls(IUIControl control, List<IUIControl> controls)
    {
        if (control == null || control.ChildControls == null)
        {
            return;
        }

        foreach (var child in control.ChildControls)
        {
            controls.Add(child);
            AllControls(child, controls);
        }
    }

    public IEnumerable<IUIControl> GetDitryControls()
    {
        return this.AllControls().Where(c => c is IInputUIControl && ((IInputUIControl)c).IsDirty);
    }

    public void SetDirty(bool isDirty)
    {
        this.AllControls().Where(c => c is IInputUIControl).ToList().ForEach(c => ((IInputUIControl)c).IsDirty = isDirty);
    }



    #endregion
}
