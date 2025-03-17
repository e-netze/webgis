namespace E.Standard.CMS.Core.UI.Abstraction;

public interface ICmsApplicationSettings
{
    object this[string key] { get; set; }
    object GetValue(string key, object defValue);
    void SetValue(string key, object Value);
}
