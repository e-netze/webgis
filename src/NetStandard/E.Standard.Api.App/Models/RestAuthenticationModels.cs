namespace E.Standard.Api.App.Models;

public class RestLoginModel : ErrorHandlingModel
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
}