using System.Threading.Tasks;

using E.Standard.CMS.Core.Abstractions;

namespace E.Standard.CMS.Core.IO.Abstractions;

public interface IDatabasePath
{
    void CreateDatabase();
    Task<bool> DeleteDatabase(IConsoleOutputStream outstream);
}
