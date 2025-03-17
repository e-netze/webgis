namespace E.Standard.Caching.Abstraction;

public interface ITempDataByteCache : IKeyValueCache<byte[]>, ICacheClearable
{
}
