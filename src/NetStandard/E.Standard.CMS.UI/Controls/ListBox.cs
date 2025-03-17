using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.CMS.UI.Controls;

public class ListBox : ControlLabel, IInputUIControl
{
    public ListBox(string name) : base(name)
    {
        _options = new OptionCollection(this);
        this.MultiSelect = true;
    }

    [JsonProperty(PropertyName = "value")]
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public string Value
    {
        get
        {
            return JSerializer.Serialize(this.SelectedItems ?? new string[0]);
        }
        set
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                this.SelectedItems = null;
            }
            else
            {
                this.SelectedItems = JSerializer.Deserialize<string[]>(value);
            }

            FireChange();
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsDirty { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string[] SelectedItems
    {
        get; private set;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public IEnumerable<Option> SelectedOptions
    {
        get
        {
            if (_options == null)
            {
                return new Option[0];
            }

            return _options.Where(o => SelectedItems.Contains(o.Value));
        }
    }

    private OptionCollection _options = null;
    [JsonProperty(PropertyName = "options")]
    [System.Text.Json.Serialization.JsonPropertyName("options")]
    public OptionCollection Options => _options;

    public void FireChange()
    {
        this.OnChange?.Invoke(this, new EventArgs());
    }
    public event EventHandler OnChange;

    [JsonProperty(PropertyName = "multiSelect")]
    [System.Text.Json.Serialization.JsonPropertyName("multiSelect")]
    public bool MultiSelect { get; set; }

    [JsonProperty(PropertyName = "selectAndCommit", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("selectAndCommit")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? SelectAndCommit { get; set; }

    [JsonProperty(PropertyName = "required")]
    [System.Text.Json.Serialization.JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonProperty(PropertyName = "height")]
    [System.Text.Json.Serialization.JsonPropertyName("height")]
    public int Height { get; set; }

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

        [JsonProperty(PropertyName = "selected", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("selected")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public bool? Selected { get; set; }
    }

    public class OptionCollection : List<Option>
    {
        private ListBox _combo;

        public OptionCollection(ListBox combo)
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
            _combo.SelectedItems = null;
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
