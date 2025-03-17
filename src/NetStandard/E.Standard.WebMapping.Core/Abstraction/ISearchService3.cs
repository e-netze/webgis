using E.Standard.Web.Abstractions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface ISearchService3 : ISearchService2
{
    Task<SearchServiceItems> Query3Async(IHttpService httpService, string term, int rows, IEnumerable<string> categories, Envelope queryBBox, int targetProjId = 4326);
}
