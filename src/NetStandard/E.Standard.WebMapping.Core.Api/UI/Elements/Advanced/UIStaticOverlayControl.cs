using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIStaticOverlayControl : UIElement
{
    public UIStaticOverlayControl()
        : base("static-overlay-control")
    {

    }

    public IEnumerable<CommandButton> command_buttons { get; set; }

    #region Classes

    public class CommandButton
    {
        public CommandButton() { }

        public CommandButton(string command, string icon)
        {
            this.command = command;
            this.icon = icon;
        }

        public string command { get; set; }
        public string icon { get; set; }
    }

    #endregion
}
