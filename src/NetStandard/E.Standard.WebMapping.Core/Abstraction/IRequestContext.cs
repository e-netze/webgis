using E.Standard.Web.Abstractions;

namespace E.Standard.WebMapping.Core.Abstraction;
public interface IRequestContext
{
    IHttpService Http { get; }
    bool Trace { get; }
    T GetRequiredService<T>();
}
