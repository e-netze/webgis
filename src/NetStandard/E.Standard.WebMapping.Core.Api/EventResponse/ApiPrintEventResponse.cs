namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class ApiPrintEventResponse : ApiEventResponse
{
    public string Url { get; set; }
    public string Path { get; set; }
    public string PreviewUrl { get; set; }
    public int Length { get; set; }
}
