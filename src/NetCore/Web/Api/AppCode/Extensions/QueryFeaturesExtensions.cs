using E.Standard.Api.App.DTOs.Print;
using E.Standard.Extensions.Formatting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Api.Core.AppCode.Extensions;

public enum LabelUnionMode
{
    First = 0,
    All = 1,
    Distinct = 2,
    Counter = 3,
    DistinctCounter = 4
}

static public class QueryFeaturesExtensions
{
    static public string ToCsv(this QueryFeaturesDTO queryFeatures, char separator = ';', bool addHeader = true, bool excel = false)
    {
        if (queryFeatures?.Features == null || queryFeatures.Features.Count() == 0)
        {
            return String.Empty;
        }

        var firstProperties = queryFeatures.Features.FirstOrDefault().Properties?.FirstOrDefault();
        if (firstProperties == null)
        {
            return String.Empty;
        }

        List<string> columns = new List<string>();
        columns.Add("MarkerIndex");

        bool hasUnionFetures = queryFeatures.HasUnionFeatures();
        if (hasUnionFetures)
        {
            columns.Add("SubIndex");
        }

        columns.AddRange(firstProperties.Keys
                                .Where(k => !k.StartsWith("_") && !firstProperties[k].ToString().StartsWith("<a "))
                                .Select(k => k));

        StringBuilder sb = new StringBuilder();

        if (addHeader)
        {
            bool firstHeader = true;
            foreach (var header in columns)
            {
                if (!firstHeader)
                {
                    sb.Append(separator);
                }
                else
                {
                    firstHeader = false;
                }

                sb.Append(header);
            }
            sb.Append(System.Environment.NewLine);
        }

        foreach (var feature in queryFeatures.Features)
        {
            int propertiesIndex = 1;
            foreach (var featureProperties in feature.Properties)
            {
                bool firstColumn = true;
                foreach (var column in columns)
                {
                    string value;
                    if (column == "MarkerIndex")
                    {
                        value = propertiesIndex == 1 ?
                            (feature.MarkerIndex + 1).ToString() :
                            String.Empty;
                    }
                    else if (column == "SubIndex")
                    {
                        value = propertiesIndex.ToString();
                    }
                    else
                    {
                        value = featureProperties[column]?.ToString() ?? String.Empty;
                    }

                    if (!firstColumn)
                    {
                        sb.Append(separator);
                    }
                    else
                    {
                        firstColumn = false;
                    }

                    if (value.StartsWith("<a "))
                    {
                        int hrefPos = value.IndexOf("href=");

                        int quoteStartPos = hrefPos + 5;
                        var quote = value[hrefPos + 5].ToString();
                        int quoteClosePos = value.IndexOf(quote, quoteStartPos + 1);

                        value = value.Substring(quoteStartPos + 1, quoteClosePos - quoteStartPos - 1);
                    }

                    if (excel && !value.IsNumber())
                    {
                        sb.Append($@"=""{value}""");  // verhindert, dass beispielsweise Grundstücksnummer in ein Datum umgewandelt werden
                    }
                    else
                    {
                        sb.Append(value);
                    }
                }
                sb.Append(System.Environment.NewLine);
                propertiesIndex++;
            }
        }

        return sb.ToString();
    }

    static public bool HasUnionFeatures(this QueryFeaturesDTO queryFeatures)
    {
        return queryFeatures?
            .Features?
            .Where(f => f.Properties != null && f.Properties.Count() > 1)
            .FirstOrDefault() != null;
    }

    static public string GetAttributeLabel(this QueryFeatureDTO queryFeature, string propertyName, LabelUnionMode mode = LabelUnionMode.First)
    {
        var values = queryFeature.GetAttributeLabels(propertyName);

        switch (mode)
        {
            case LabelUnionMode.All:
                return String.Join('\n', values);
            case LabelUnionMode.Distinct:
                return String.Join('\n', values.Distinct());
            case LabelUnionMode.Counter:
            case LabelUnionMode.DistinctCounter:
                values = mode == LabelUnionMode.DistinctCounter ? values.Distinct() : values;
                if (values.Count() <= 1)
                {
                    return values.FirstOrDefault() ?? String.Empty;
                }
                return $"{values.FirstOrDefault()} (+{(values.Count() - 1)})";
            default:  // LabelUnionMode.First
                return values.FirstOrDefault() ?? String.Empty;
        }
    }

    static public IEnumerable<string> GetAttributeLabels(this QueryFeatureDTO queryFeature, string propertyName)
    {
        if (queryFeature?.Properties != null)
        {
            return queryFeature.Properties
                .Where(p => p.ContainsKey(propertyName))
                .Select(p => p[propertyName]?.ToString())
                .Where(v => !String.IsNullOrEmpty(v));

        }

        return new string[0];
    }
}
