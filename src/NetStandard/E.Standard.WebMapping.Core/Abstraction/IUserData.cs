namespace E.Standard.WebMapping.Core.Abstraction;

public interface IUserData
{
    object UserValue(string key, object defaultValue);
    string UserString(string key);
    void SetUserValue(string key, object value);

    void ClearAllUserValues();
    void ClearUserValue(string key);
}
