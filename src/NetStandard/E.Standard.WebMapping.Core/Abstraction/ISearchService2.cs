using System.Collections.Generic;
using System.Threading.Tasks;

using E.Standard.Web.Abstractions;
using E.Standard.WebMapping.Core.Models;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface ISearchService2 : ISearchService
{
    Task<IEnumerable<SearchServiceAggregationBucket>> TypesAsync(IHttpService httpService);

    Task<SearchTypeMetadata> GetTypeMetadataAsync(IHttpService httpService, string metaId);
    Task<IEnumerable<SearchTypeMetadata>> GetTypesMetadataAsync(IHttpService httpService);

    Task<SearchServiceItems> Query2Async(IHttpService httpService, string term, int rows, IEnumerable<string> categories, int targetProjId = 4326);
}
