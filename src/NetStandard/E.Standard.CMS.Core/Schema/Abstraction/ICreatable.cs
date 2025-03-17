using E.Standard.CMS.Core.IO.Abstractions;
using System.Threading.Tasks;

namespace E.Standard.CMS.Core.Schema.Abstraction;

public interface ICreatable : IPersistable
{
    string CreateAs(bool appendRoot);
    Task<bool> CreatedAsync(string FullName);
}
