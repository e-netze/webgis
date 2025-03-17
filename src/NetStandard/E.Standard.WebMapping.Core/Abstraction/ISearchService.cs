using E.Standard.Web.Abstractions;
using E.Standard.WebMapping.Core.Models;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface ISearchService
{
    string Name { get; set; }
    string Id { get; set; }
    string CopyrightId { get; set; }
    Task<SearchServiceItems> QueryAsync(IHttpService httpService, string term, int rows, int targetProjId = 4326);
}
