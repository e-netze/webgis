using E.Standard.Cms.Services;
using E.Standard.CMS.Core.Abstractions;

namespace E.Standard.Cms.Abstraction;
public interface ICmsTool
{
    bool Run(CmsToolContext context, IConsoleOutputStream console);
}
