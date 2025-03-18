using E.Standard.Localization.Abstractions;

namespace Portal.Core.AppCode.Services;

public class CultureProvider : ICultureProvider
{
    public string Culture { get; } = "de";
}
