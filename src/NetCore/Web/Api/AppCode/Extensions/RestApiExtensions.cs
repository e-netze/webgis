using Amazon.Runtime.Internal.Transform;
using E.Standard.Json;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.GeoServices.Print;
using Microsoft.AspNetCore.Http;
using System;

namespace Api.Core.AppCode.Extensions;

static public class RestApiExtensions
{
    static public VisFilterDefinitionDTO[] VisFilterDefinitionsFromParameters(this HttpRequest httpRequest)
    {
        string filterJson = httpRequest.FormOrQuery("filters");

        if (String.IsNullOrWhiteSpace(filterJson))
        {
            return null;
        }

        var filters = JSerializer.Deserialize<VisFilterDefinitionDTO[]>(filterJson);
        foreach (var filter in filters)
        {
            filter.CalcServiceId();
        }
        return filters;
    }

    static public TimeEpochDefinitionDTO[] TimeEpochDefinitionDTOsFromParameters(this HttpRequest httpRequest)
    {
        string timeEpochJson = httpRequest.FormOrQuery("timeEpoch");

        if (String.IsNullOrWhiteSpace(timeEpochJson))
        {
            return null;
        }

        return JSerializer.Deserialize<TimeEpochDefinitionDTO[]>(timeEpochJson);
    }

    static public LabelingDefinitionDTO[] LabelDefinitionsFromParameters(this HttpRequest httpRequest)
    {
        var labelingJson = httpRequest.FormOrQuery("labels");

        if (String.IsNullOrWhiteSpace(labelingJson))
        {
            return null;
        }

        var labels = JSerializer.Deserialize<LabelingDefinitionDTO[]>(labelingJson);
        foreach (var label in labels)
        {
            label.CalcServiceId();
        }

        return labels;
    }

    #region Print / PrintLayout

    // printFormatString: zB A4.Portrait
    static public PageSize GetPageSize(this string printFormatString)
    {
        PageSize pageSize = PageSize.A4;

        if (!String.IsNullOrWhiteSpace(printFormatString))
        {
            pageSize = (PageSize)Enum.Parse(typeof(PageSize), printFormatString.Split('.')[0], true);
        }

        return pageSize;
    }

    // printFormatString: zB A4.Portrait
    static public PageOrientation GetPageOrientation(this string printFormatString)
    {
        PageOrientation pageOrientation = PageOrientation.Landscape;

        if (!String.IsNullOrWhiteSpace(printFormatString) && printFormatString.Contains("."))
        {
            pageOrientation = (PageOrientation)Enum.Parse(typeof(PageOrientation), printFormatString.Split('.')[1], true);
        }

        return pageOrientation;
    }

    #endregion
}
