namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomSecretUrlParameterDecoder
{
    string Name { get; }
    string Decode(string input);
}
