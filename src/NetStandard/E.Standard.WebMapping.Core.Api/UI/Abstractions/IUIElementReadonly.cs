namespace E.Standard.WebMapping.Core.Api.UI.Abstractions;

public interface IUIElementReadonly : IUIElement
{
    bool @readonly { get; set; }
}
