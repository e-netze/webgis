namespace E.Standard.CMS.Core.UI.Abstraction;

public interface IInputUIControl : IEditableUIControl
{
    string Value { get; set; }

    bool IsDirty { get; set; }

    bool Required { get; set; }
}
