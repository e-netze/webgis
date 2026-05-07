using System.Threading.Tasks;

using E.Standard.CMS.Core.IO.Abstractions;

namespace E.Standard.CMS.Core.Schema.Abstraction;

public interface IRefreshable : IPersistable
{
    Task<bool> RefreshAsync(string FullName, int level);
}
