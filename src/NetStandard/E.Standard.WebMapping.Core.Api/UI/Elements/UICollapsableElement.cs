using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UICollapsableElement : UIElement
{
    public UICollapsableElement(string elementType)
        : base(elementType)
    {
        this.CollapseState = CollapseStatus.NotCollapsable;
        this.ExpandBehavior = ExpandBehaviorMode.Normal;
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string title { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string collapsestate
    {
        get
        {
            if (this.CollapseState != CollapseStatus.NotCollapsable)
            {
                return this.CollapseState.ToString().ToLower();
            }
            return null;
        }
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string expandbahavior
    {
        get
        {
            if (this.ExpandBehavior != ExpandBehaviorMode.Normal)
            {
                return this.ExpandBehavior.ToString().ToLower();
            }
            return null;
        }
    }

    public bool iscollapsable
    {
        get
        {
            return this.CollapseState != CollapseStatus.NotCollapsable;
        }
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public CollapseStatus CollapseState
    {
        get; set;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public ExpandBehaviorMode ExpandBehavior
    {
        get;
        set;
    }

    #region Enums

    public enum CollapseStatus
    {
        NotCollapsable = 0,
        Collapsed = 1,
        Expanded = 2
    }

    public enum ExpandBehaviorMode
    {
        Normal = 0,
        Exclusive = 1
    }

    #endregion
}
