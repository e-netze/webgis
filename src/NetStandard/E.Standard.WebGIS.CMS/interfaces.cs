using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebGIS.CMS;

public interface IServiceCreator
{
    IMapService CreateServiceInstance(CmsDocument cms, CmsDocument.UserIdentification cmsui, CmsLink serviceLink);
}
