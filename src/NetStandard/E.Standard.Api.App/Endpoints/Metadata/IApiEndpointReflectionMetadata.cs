#nullable enable

using System;

namespace E.Standard.Api.App.Endpoints.Metadata;

public interface IApiEndpointReflectionMetadata
{
    T? GetAttribute<T>() where T : Attribute;
    T[] GetAttributes<T>() where T : Attribute;

    Attribute[] GetAllAttributes();
}
