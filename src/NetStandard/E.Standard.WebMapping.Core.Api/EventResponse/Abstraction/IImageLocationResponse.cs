namespace E.Standard.WebMapping.Core.Api.EventResponse.Abstraction;

public interface IImageLocationResponse
{
    string id { get; set; }
    string url { get; set; }
    string requestid { get; set; }

    double[] extent { get; set; }

    double scale { get; set; }
}
