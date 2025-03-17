using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Extensions;

namespace E.Standard.WebMapping.Core.Api.UI.Setters;

public class UIPersistentParametersSetter : UISetter
{
    public UIPersistentParametersSetter(IApiButton tool)
        : base("_webgis_setter_persistent_parameters_", tool.GetType().ToToolId())
    {
        if (tool is IApiToolPersistenceContext)
        {
            this.val = ((IApiToolPersistenceContext)tool).PersistenceContextTool.ToToolId();
        }
    }
}
