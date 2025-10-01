using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public interface IUIInputField
{
    UILabel UILabel { get; }
    IUIElement Input { get; }
}

public class UIInputField<TInput> : UIDiv, IUIInputField
    where TInput : IUIElement, new()
{
    private readonly UILabel _label;
    private readonly TInput _input;

    internal UIInputField()
    {
        this.css = "webgis-inputfield";
        
        _label = new UILabel();
        _input = new TInput();

        this.AddChildren(_label, _input);
    }

    public UILabel UILabel { get { return _label; } }
    public IUIElement Input { get { return _input; } }
}

public class UIInputTextField : UIInputField<UIInputText>
{
    public UIInputTextField() : base()
    {
    }
}


