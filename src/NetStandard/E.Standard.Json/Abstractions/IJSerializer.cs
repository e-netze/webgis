﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace E.Standard.Json.Abstractions;
internal interface IJSerializer
{
    string Serialize<TValue>(TValue value, bool pretty = false);

    T? Deserialize<T>([StringSyntax("Json")] string json);
    object? Deserialize([StringSyntax("Json")] string json, Type type);

    bool IsJsonElement(object element);
    object? GetJsonElementValue<T>(object element);
    object? GetJsonElementValue(object element, string propertyName);

    IEnumerable<string> GetJsonElementProperties(object? element);

    object? AsValueIfJsonValueType(object? element);
}
