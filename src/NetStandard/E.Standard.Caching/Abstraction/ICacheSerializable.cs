namespace E.Standard.Caching.Abstraction;

public interface ICacheSerializable
{
    byte[] Serialize();
    int Expires { get; set; }  // Seconds
}
public interface ICacheSerializable<T> : ICacheSerializable
{

    T Deserialize(byte[] data);
}
