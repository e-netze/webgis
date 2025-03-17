namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIOptionList : UIElement
{
    public UIOptionList()
        : base("option-list")
    {

    }

    #region Options

    public class Option : UIElement
    {
        public Option()
            : base("option-list-item")
        {

        }
    }

    #endregion
}
