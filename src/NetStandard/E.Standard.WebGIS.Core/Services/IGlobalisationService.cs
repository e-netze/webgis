namespace E.Standard.WebGIS.Core.Services;

public interface IGlobalisationService
{
    string Get(string key, string language = "");

    string DefaultLanguage { get; }
}
