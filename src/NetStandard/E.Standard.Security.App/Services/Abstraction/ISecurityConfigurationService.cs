namespace E.Standard.Security.App.Services.Abstraction;

public interface ISecurityConfigurationService
{
    string this[string key] { get; }
}
