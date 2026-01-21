using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Extensions;

namespace E.Standard.WebMapping.Core.Api.UI.Setters;

public class UIApplyPersistentParametersSetter : UISetter
{
    public UIApplyPersistentParametersSetter(IApiButton tool)
        : base("_webgis_setter_persistent_parameters_", tool.GetType().ToToolId())
    {
        if (tool is IApiToolPersistenceContext)
        {
            this.val = ((IApiToolPersistenceContext)tool).PersistenceContextTool.ToToolId();
        }
    }
}

public class UIUpdatePersistentParametersSetter : UISetter
{
    public UIUpdatePersistentParametersSetter(IApiButton tool)
        : base("_webgis_setter_update_persistent_parameters_", tool.GetType().ToToolId())
    {
        if (tool is IApiToolPersistenceContext)
        {
            this.val = ((IApiToolPersistenceContext)tool).PersistenceContextTool.ToToolId();
        }
    }
}

