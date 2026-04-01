#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Api.App.Endpoints.Metadata;

public interface IApiEndpointReflectionMetadata
{
    T? GetAttribute<T>() where T : Attribute;
    T[] GetAttributes<T>() where T : Attribute;

    Attribute[] GetAllAttributes();
}
