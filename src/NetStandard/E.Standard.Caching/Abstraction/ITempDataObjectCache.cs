namespace E.Standard.Caching.Abstraction;

public interface ITempDataObjectCache : IKeyValueCache<object>, ICacheClearable
{
}
