namespace E.Standard.WebMapping.Core.Api.UI.Setters;

public class UICssSetter : UISetter
{
    public enum SetterType
    {
        AddClass,
        RemoveClass,
        SelectOption
    }

    public UICssSetter(SetterType setterType, string elementId, string cssClass)
        : base(elementId, cssClass)
    {
        base.name = setterType.ToString().ToLower();
    }
}
