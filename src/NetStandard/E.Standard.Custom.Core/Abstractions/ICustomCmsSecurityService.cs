using E.Standard.CMS.Core;
using E.Standard.Custom.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomCmsSecurityService
{
    string SecurityMethod { get; }
    string AdviceMessage { get; }

    IEnumerable<CustomSecurityPrefix> GetCustomSecurityPrefixes();

    Task<IEnumerable<AutoCompleteItem>> AutoCompleteValues(string term, string prefix, string cmsId = "", string subscriberId = "");

    Task BeforeSaveAclFile(string cmsId, string path, CMSManager.NodeAuthorization nodeAuth, object closestInstance = null, string closestInstancePath = "");
    Task OnAclFileLoaded(string cmsId, string path, CMSManager.NodeAuthorization nodeAuth, string inheritPath);
}
