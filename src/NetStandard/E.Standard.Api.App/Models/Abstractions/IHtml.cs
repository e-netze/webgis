using E.Standard.Api.App.Services.Cache;

namespace E.Standard.Api.App.Models.Abstractions;

public interface IHtml
{
    string ToHtmlString();
}

public interface IHtml2 : IHtml
{
    string[] PropertyLinks { get; }
}

public interface IHtmlForm : IHtml
{
    object Object { get; }
}

public interface IHtml3 : IHtml
{
    string ToHtmlString(CacheService cache);
}