using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace E.Standard.CMS.UI.Controls;

public class InputAutoComplete : Input
{
    public InputAutoComplete(string name)
        : base(name)
    {
        this.IsPassword = false;
    }

    public AutoCompleteItems FireAutocomplete(string cmsId, string userId)
    {
        var args = new AutoCompleteEventArgs(cmsId, userId);

        OnAutoComplete?.Invoke(this, args);

        return args.Items;
    }
    public event EventHandler OnAutoComplete;

    #region Classes

    public class AutoCompleteEventArgs : EventArgs
    {
        public AutoCompleteEventArgs(string cmsId, string userId)
        {
            this.CmsId = cmsId;
            this.UserId = userId;
            this.Items = new AutoCompleteItems();
        }

        public string CmsId { get; private set; }
        public string UserId { get; private set; }
        public AutoCompleteItems Items { get; private set; }
    }
    public class AutoCompleteItems : List<AutoCompleteItems.Item>
    {
        public class Item
        {
            private string _value;

            [JsonProperty("value")]
            [System.Text.Json.Serialization.JsonPropertyName("value")]
            public string Value
            {
                get
                {
                    if (!String.IsNullOrEmpty(this._value))
                    {
                        return _value;
                    }

                    return SuggestedText;
                }
                set { _value = value; }
            }

            [JsonProperty("thumbnail")]
            [System.Text.Json.Serialization.JsonPropertyName("thumbnail")]
            public string Thumbnail { get; set; }
            [JsonProperty("suggested_text")]
            [System.Text.Json.Serialization.JsonPropertyName("suggested_text")]
            public string SuggestedText { get; set; }
            [JsonProperty("subtext")]
            [System.Text.Json.Serialization.JsonPropertyName("subtext")]
            public string SubText { get; set; }
        }
    }

    #endregion
}
