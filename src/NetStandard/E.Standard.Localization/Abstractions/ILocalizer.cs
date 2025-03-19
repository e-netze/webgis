namespace E.Standard.Localization.Abstractions;
public interface ILocalizer
{
    string Localize(string key);
}
public interface ILocalizer<T> : ILocalizer
{
}
