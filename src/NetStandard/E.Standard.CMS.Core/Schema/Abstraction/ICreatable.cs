using System.Threading.Tasks;

using E.Standard.CMS.Core.IO.Abstractions;

namespace E.Standard.CMS.Core.Schema.Abstraction;

public interface ICreatable : IPersistable
{
    string CreateAs(bool appendRoot);
    Task<bool> CreatedAsync(string FullName);
}
