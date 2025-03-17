using System;

namespace Api.Core.AppCode.Reflection;

public class EtagAttribute : Attribute
{
    public EtagAttribute(double expiraionDays = 1, bool appendResponseHeaders = true)
    {
        ExpirationDays = expiraionDays;
        AppendResponseHeaders = appendResponseHeaders;
    }

    public double ExpirationDays { get; }

    public bool AppendResponseHeaders { get; }
}
