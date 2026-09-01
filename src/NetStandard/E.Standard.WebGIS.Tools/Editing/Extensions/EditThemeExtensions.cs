using System;
using System.Collections.Generic;
using System.Linq;

using E.Standard.CMS.Core;
using E.Standard.WebGIS.CMS;

using static E.Standard.WebGIS.Tools.Editing.Environment.EditEnvironment;

namespace E.Standard.WebGIS.Tools.Editing.Extensions;

static internal class EditThemeExtensions
{
    /// <summary>
    /// Checks whether any commit action's SuccessMessage relevant for the given <paramref name="command"/>
    /// (Insert/Update/Delete/...) contains a [FIELDNAME] style placeholder.
    /// Used to decide whether features need to be queried with all attributes (QueryFields.All)
    /// instead of just their id (QueryFields.Id) before committing, so placeholders can be resolved.
    /// </summary>
    public static bool HasCommitSuccessMessagePlaceholders(this EditTheme editTheme, EditFeatureCommand command)
    {
        var relevantTimings = GetRelevantTimings(command);

        return editTheme?
            .CommitActions?
            .Where(c => relevantTimings.Contains(c.Timing))
            .Any(c => Helper.GetKeyParameters(c.SuccessMessage) != null) == true;
    }

    private static EditCommitActionTiming[] GetRelevantTimings(EditFeatureCommand command)
        => command switch
        {
            EditFeatureCommand.Insert or EditFeatureCommand.Transfer
                => new[] { EditCommitActionTiming.Before_Insert, EditCommitActionTiming.After_Insert },
            EditFeatureCommand.Update or EditFeatureCommand.MassAttribution
                => new[] { EditCommitActionTiming.Before_Update, EditCommitActionTiming.After_Update },
            EditFeatureCommand.Delete
                => new[] { EditCommitActionTiming.Before_Delete, EditCommitActionTiming.After_Delete },
            _ => Array.Empty<EditCommitActionTiming>()
        };
}
