using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IServiceDescription
{
    string ServiceDescription { get; }
    string CopyrightText { get; }
}

public interface IServiceSecuredDownload
{
    Task<byte[]> GetSecuredData(IRequestContext context, string url);
}
