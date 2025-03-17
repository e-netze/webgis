using E.Standard.Json;

namespace E.Standard.Configuration;

public class JsonAppConfiguration : AppConfiguration
{
    public JsonAppConfiguration(string configFile = "")
        : base(configFile)
    {

    }

    public T Deserialize<T>()
    {
        return JSerializer.Deserialize<T>(System.IO.File.ReadAllText(_configFile));
    }
}
