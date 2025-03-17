using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebMapping.Core.Editing;

namespace E.Standard.WebGIS.Tools.Editing.Extensions;

static internal class WorkspaceExtensions
{
    static public bool ValidationRecommended(this IFeatureWorkspace ws, string field, EditEnvironment.EditFeatureCommand command)
    {
        if (command == EditEnvironment.EditFeatureCommand.Insert)  // always check on INSERT
        {
            return true;
        }

        try  // no sure, if CurrentFeatureHasAttribute() is implementet correctly in all FeatureWorkspace implementations
        {
            bool statmentContainsField = ws.CurrentFeatureHasAttribute(field);

            if (!statmentContainsField)  // if field will not be set on (UPDATE/DELETE/MASS) irgnore Validation
            {
                return false;
            }
        }
        catch { }

        return true;
    }
}
