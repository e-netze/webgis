using E.Standard.WebGIS.Tools.Editing.Environment;

namespace E.Standard.WebGIS.Tools.Editing.Extensions;

static internal class EditOperationExtensions
{
    static public bool IsMassOrTransfer(this EditOperation editOperation) =>
        editOperation == EditOperation.MassAttributation || editOperation == EditOperation.FeatureTransfer;
}
