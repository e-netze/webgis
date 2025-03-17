using E.Standard.Api.App.Models.Abstractions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class SearchServiceFormDTO : IHtml
{
    public SearchServiceFormDTO(ISearchService searchService, IEnumerable<SearchServiceAggregationBucket> types = null)
    {
        this.SearchService = searchService;
        this.Types = types;
    }

    public ISearchService SearchService { get; set; }
    public IEnumerable<SearchServiceAggregationBucket> Types { get; private set; }
    public string Action { get; set; }

    #region IHtml Member

    public string ToHtmlString()
    {
        if (this.SearchService == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append(HtmlHelper.ToHeader("Search Service", HtmlHelper.HeaderType.h2));

        sb.Append(HtmlHelper.ToTable(new string[] { "Id", "Name" }, new object[] { SearchService.Id, SearchService.Name }));
        sb.Append(HtmlHelper.LineBreak(2));

        sb.Append(HtmlHelper.Autocomplete("Suchbegriff", "term", this.Action + "?c=query"));

        if (this.Types != null)
        {
            sb.Append(HtmlHelper.ToHeader("Categories", HtmlHelper.HeaderType.h4));
            foreach (var category in this.Types)
            {
                sb.Append(HtmlHelper.Text(category.Key + " (" + category.Count + ")", true));
            }
        }

        return sb.ToString();
    }

    #endregion
}