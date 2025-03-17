using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.UI.Abstractions;

public interface IUIElement
{
    string id { get; set; }
    string css { get; set; }
    string type { get; set; }

    object value { get; set; }

    string[] ParameterServerCommands { get; set; }

    ICollection<IUIElement> elements { get; set; }

    string target { get; set; }
    string targettitle { get; set; }

    string targetonclosetype { get; set; }
    string targetonclosecommand { get; set; }

    VisibilityDependency VisibilityDependency { get; set; }
}
