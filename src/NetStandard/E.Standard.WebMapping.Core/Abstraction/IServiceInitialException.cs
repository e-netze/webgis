using E.Standard.WebMapping.Core.ServiceResponses;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IServiceInitialException
{
    ErrorResponse InitialException { get; }
}
