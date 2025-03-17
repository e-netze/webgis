using System.Threading.Tasks;

namespace E.Standard.Caching.Abstraction;

public interface ICacheClearable
{
    Task<bool> Clear();
}
