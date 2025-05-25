using E.Standard.WebMapping.Core.ServiceResponses;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IMapServiceInitialException
{
    ErrorResponse InitialException { get; }
}
