namespace E.Standard.Localization.Abstractions;
public interface ILocalizer
{
    string Localize(string key);

    ILocalizer<TClass> CreateFor<TClass>();
}
public interface ILocalizer<T> : ILocalizer
{
      
}
