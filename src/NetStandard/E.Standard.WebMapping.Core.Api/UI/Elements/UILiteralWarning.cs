namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UILiteralWarning : UIDiv
{
    public UILiteralWarning(string literal) : base()
    {
        css = UICss.ToClass(new[] { "webgis-warning-item" });
        elements = new UIElement[]
        {
            new UILiteral() { literal = literal }
        };
    }
}
