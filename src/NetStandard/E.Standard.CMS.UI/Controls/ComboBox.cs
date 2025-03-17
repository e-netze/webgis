using E.Standard.CMS.Core.UI.Abstraction;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.CMS.UI.Controls;

public class ComboBox : ControlLabel, IInputUIControl, IClickUIControl
{
    public ComboBox(string name) : base(name)
    {
        _options = new OptionCollection(this);
    }

    public ComboBox(string name, Type enumType)
        : this(name)
    {
        foreach (var value in Enum.GetValues(enumType))
        {
            _options.Add(new Option(((int)value).ToString(), Enum.GetName(enumType, value)));
        }
    }

    [JsonProperty(PropertyName = "value")]
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public string Value
    {
        get
        {
            if (this.SelectedIndex >= this._options.Count || this.SelectedIndex < 0)
            {
                return null;
            }

            var option = this.Options[this.SelectedIndex];
            return option.Value;
        }
        set
        {
            var option = this.Options.Where(o => o.Value == value).FirstOrDefault();
            if (option == null)
            {
                option = new Option(value);
                this.Options.Add(option);
            }

            this.SelectedIndex = this.Options.IndexOf(option);
            FireChange();
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsDirty { get; set; }

    [JsonProperty(PropertyName = "required")]
    [System.Text.Json.Serialization.JsonPropertyName("required")]
    public bool Required { get; set; }

    private int _selectedIndex = 0;
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public int SelectedIndex
    {
        get
        {
            return _selectedIndex;
        }
        set
        {
            _selectedIndex = value;
            FireChange();
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object SelectedItem { get { return this.Value; } }

    private OptionCollection _options = null;
    [JsonProperty(PropertyName = "options")]
    [System.Text.Json.Serialization.JsonPropertyName("options")]
    public OptionCollection Options => _options;

    public void FireChange()
    {
        this.OnChange?.Invoke(this, new EventArgs());
    }
    public event EventHandler OnChange;

    // => Client on change event triggerd

    [JsonProperty("triggerOnChange")]
    [System.Text.Json.Serialization.JsonPropertyName("triggerOnChange")]
    public bool TriggerClientChangeEvent
    {
        get
        {
            return OnClick != null;
        }
        set { }
    }

    public event EventHandler OnClick;
    public void FireClick()
    {
        OnClick?.Invoke(this, new EventArgs());
    }

    #region Classes

    public class Option
    {
        public Option(string value)
        {
            this.Label = this.Value = value;
        }

        public Option(string value, string label)
        {
            this.Value = value;
            this.Label = label;
        }

        [JsonProperty(PropertyName = "label")]
        [System.Text.Json.Serialization.JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonProperty(PropertyName = "value")]
        [System.Text.Json.Serialization.JsonPropertyName("value")]
        public string Value { get; set; }
    }

    public class OptionCollection : List<Option>
    {
        private ComboBox _combo;

        public OptionCollection(ComboBox combo)
        {
            _combo = combo;
        }

        new public void Add(Option option)
        {
            _combo.IsDirty = true;
            base.Add(option);
        }

        new public void AddRange(IEnumerable<Option> options)
        {
            _combo.IsDirty = true;
            base.AddRange(options);
        }

        new public void Clear()
        {
            _combo.SelectedIndex = 0;
            _combo.IsDirty = true;
            base.Clear();
        }

        new public void Insert(int index, Option option)
        {
            _combo.IsDirty = true;
            base.Insert(index, option);
        }
    }

    #endregion
}
