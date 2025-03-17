using Xunit.Abstractions;

namespace E.Standard.Json.Test;

public class JsonDeserializerTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public JsonDeserializerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData(typeof(int?))]
    [InlineData(typeof(float?))]
    [InlineData(typeof(double?))]
    [InlineData(typeof(int[]))]
    [InlineData(typeof(float[]))]
    [InlineData(typeof(double[]))]
    [InlineData(typeof(string))]
    public void DeserialzeTypeFromEmptyString(Type type)
    {
        // Act
        JSerializer.SetEngine(JsonEngine.SytemTextJson);
        var deserializedObject1 = JSerializer.Deserialize("", type);
        JSerializer.SetEngine(JsonEngine.NewtonSoft);
        var deserializedObject2 = JSerializer.Deserialize("", type);

        _testOutputHelper.WriteLine($"Deserialized {type}: {(deserializedObject1 ?? "null")}");
        _testOutputHelper.WriteLine($"Deserialized {type}: {(deserializedObject2 ?? "null")}");

        // Assert
        Assert.Equal(deserializedObject1, deserializedObject2);
    }
}