using E.Standard.CMS.Core.Abstractions;
using System.Threading.Tasks;

namespace E.Standard.CMS.Core.IO.Abstractions;

public interface IDatabasePath
{
    void CreateDatabase();
    Task<bool> DeleteDatabase(IConsoleOutputStream outstream);
}
