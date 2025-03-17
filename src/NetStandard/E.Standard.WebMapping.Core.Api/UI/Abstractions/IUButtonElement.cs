namespace E.Standard.WebMapping.Core.Api.UI.Abstractions;

public interface IButtonUIElement : IUIElementText, IUIElementIcon
{
    string buttontype { get; set; }
    string buttoncommand { get; set; }
    string buttoncommand_argument { get; set; }
}