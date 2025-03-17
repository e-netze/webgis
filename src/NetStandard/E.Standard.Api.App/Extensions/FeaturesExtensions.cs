using E.Standard.Api.App.DTOs;
using System;
using System.Security.Cryptography;
using System.Text;

namespace E.Standard.Api.App.Extensions;

static public class FeaturesExtensions
{
    static public string CalcHashCode(this FeaturesDTO features)
    {
        if (features == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();

        if (features.metadata != null)
        {
            sb.Append($"{features.metadata.ToolTypeString}:{features.metadata.ServiceId}-{features.metadata.QueryId}");
        }

        if (features.features != null)
        {
            foreach (var feature in features.features)
            {
                sb.Append($"{feature.oid},");
            }
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());

        using (var sha256 = SHA256.Create())
        {
            return Convert.ToBase64String(sha256.ComputeHash(bytes));
        }
    }
}
