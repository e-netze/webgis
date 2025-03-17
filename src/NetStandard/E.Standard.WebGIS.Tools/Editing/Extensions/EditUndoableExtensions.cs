using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebMapping.Core.Editing;
using System;

namespace E.Standard.WebGIS.Tools.Editing.Extensions;

static internal class EditUndoableExtensions
{
    static public string ToTitle(this EditUndoableDTO undoable, EditEnvironment.EditTheme editTheme, string commandTitle = "")
    {
        if (String.IsNullOrEmpty(commandTitle))
        {
            switch (undoable.Command)
            {
                case SqlCommand.insert:
                    commandTitle = "Einfügen";
                    break;
                case SqlCommand.update:
                    commandTitle = "Bearbeiten";
                    break;
                case SqlCommand.delete:
                    commandTitle = "Löschen";
                    break;
            }
        }

        return $"{commandTitle}: {undoable.FeatureCount} x {editTheme?.Name}";
    }
}
