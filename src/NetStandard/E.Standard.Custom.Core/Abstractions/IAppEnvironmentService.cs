namespace E.Standard.Custom.Core.Abstractions;

public interface IAppEnvironment
{
    string ConfigRootPath { get; }

    string AppEtcPath { get; }

    string AppRootUrl { get; }
}
