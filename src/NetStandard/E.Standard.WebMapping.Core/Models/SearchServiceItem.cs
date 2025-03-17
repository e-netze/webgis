using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebMapping.Core.Models;

public class SearchServiceItem
{
    public SearchServiceItem(ISearchService service)
    {
        this.Service = service;
    }

    public ISearchService Service { get; set; }

    public string Id { get; set; }

    public Shape Geometry { get; set; }

    public string SuggestText { get; set; }
    public string ThumbnailUrl { get; set; }
    public string Subtext { get; set; }
    public string Link { get; set; }
    public string Category { get; set; }
    public double[] BBox { get; set; }

    public double Score { get; set; }

    public bool DoYouMean { get; set; }
}
