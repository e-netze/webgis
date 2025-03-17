namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiClientButton : IApiButton
{
    ApiClientButtonCommand ClientCommand { get; }
}
