using System;
using System.Collections.Generic;

using E.Standard.Api.App.Services.Cache;

namespace E.Standard.Api.App.Exceptions;

static internal class CollectionExtensions
{
    static public void ClearAndDispose<T>(this Dictionary<string, AuthObject<T>> dict)
    {
        foreach (var authObject in dict.Values)
        {
            if (authObject.QueryObject(null) is IDisposable)
            {
                ((IDisposable)authObject.QueryObject(null)).Dispose();
            }
        }

        dict.Clear();
    }
}
