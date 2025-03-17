using System.Threading.Tasks;

namespace E.Standard.Caching.Abstraction;

public interface ICacheClearableService : ICacheClearable
{
    Task<object> GetCacheObject();
}
