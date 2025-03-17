namespace E.Standard.WebMapping.Core.Abstraction;

public interface IDynamicService : IMapService
{
    ServiceDynamicPresentations CreatePresentationsDynamic { get; set; }
    ServiceDynamicQueries CreateQueriesDynamic { get; set; }
}
