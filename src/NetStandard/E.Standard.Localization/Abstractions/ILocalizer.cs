namespace E.Standard.Localization.Abstractions;

public interface ILocalizer
{
    public enum LocalizeMode
    {
        NamespaceWithFallbackToKey,
        NamespaceOnly,
        ExcactKeyOnly
    }

    public enum LocalizerDefaultValue
    {
        OriginalKey,
        Null,
        EmptyString
    }

    string Localize(string key, LocalizeMode mode = LocalizeMode.NamespaceWithFallbackToKey, LocalizerDefaultValue defaultValue = LocalizerDefaultValue.OriginalKey);

    ILocalizer<TClass> CreateFor<TClass>();
}
public interface ILocalizer<T> : ILocalizer
{
      
}
