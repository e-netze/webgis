using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.CMS.Core.Abstractions;

public interface ICustomCmsDocumentAclProviderService
{
    Task<IDictionary<string, CmsDocument.AuthNode>> CmsAuthNodes(string cmsName);
}
