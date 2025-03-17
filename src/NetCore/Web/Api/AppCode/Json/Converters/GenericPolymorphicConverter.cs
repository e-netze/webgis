using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Core.AppCode.Json.Converters;

public class GenericPolymorphicConverter<T> : JsonConverter<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            JsonSerializer.Serialize(writer, (object)null!, options);
            return;
        }

        var type = value.GetType();
        JsonSerializer.Serialize(writer, value, type, options);
    }

}
