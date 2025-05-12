using E.Standard.Json.Converters;
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

    [Theory]
    [InlineData("""{ "Id": "1729" }""")]
    [InlineData("""{ "Id": 1729 }""")]
    //[InlineData("""{ "id": 1729.0 }""")]
    public void DeserializeNummerAsString(string json)
    {
        JSerializer.SetEngine(JsonEngine.SytemTextJson);
        var deserializedObject1 = JSerializer.Deserialize<DtoWithString>(json)!;
        JSerializer.SetEngine(JsonEngine.NewtonSoft);
        var deserializedObject2 = JSerializer.Deserialize<DtoWithString>(json)!;

        // Assert
        Assert.Equal(deserializedObject1.Id, "1729");
        Assert.Equal(deserializedObject2.Id, "1729");
    }

    #region Classes

    private class DtoWithString
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(StringConverter))] 
        public string Id { get; set; }
    }

    #endregion
}