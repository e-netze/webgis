using E.Standard.CMS.Core.IO.Abstractions;
using System.Threading.Tasks;

namespace E.Standard.CMS.Core.Schema.Abstraction;

public interface IRefreshable : IPersistable
{
    Task<bool> RefreshAsync(string FullName, int level);
}
