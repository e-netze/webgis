#nullable enable

using System;
using System.Linq;

namespace E.Standard.Api.App.Endpoints.Metadata;

public class ApiEndpointReclectionMetadata : IApiEndpointReflectionMetadata
{
    private Attribute[]? _attributes;

    public void Add(Attribute attribute)
    {
        _attributes = _attributes == null
            ? new[] { attribute }
            : _attributes.Append(attribute).ToArray();
    }

    public Attribute[] GetAllAttributes() => _attributes ?? [];

    public T? GetAttribute<T>() where T : Attribute
    {
        return (T?)_attributes?.Where(a => a?.GetType() == typeof(T)).FirstOrDefault();
    }

    public T[] GetAttributes<T>() where T : Attribute
    {
        return _attributes?
            .Where(a => a?.GetType() == typeof(T))
            .Select(a => (T)a)
            .ToArray() ?? [];
    }
}