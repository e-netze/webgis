using E.Standard.Platform;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace E.Standard.WebMapping.Core.Api.Bridge;

public enum QueryFields
{
    TableFields = 0,
    Id = 1,
    All = 2,
    Shape = 3
}

public class ApiQueryFilter
{
    public ApiQueryFilter()
    {
        this.QueryItems = new NameValueCollection();
        this.QueryGeometry = true;
        this.Fields = QueryFields.TableFields;
        this.SuppressResolveAttributeDomains = false;
    }

    internal ApiQueryFilter(ApiQueryFilter filter)
        : this()
    {
        foreach (var key in filter.QueryItems.AllKeys)
        {
            this.QueryItems.Add(key, filter.QueryItems[key]);
        }

        this.QueryGeometry = filter.QueryGeometry;
        this.FeatureSpatialReference = filter.FeatureSpatialReference;
        this.SuppressResolveAttributeDomains = filter.SuppressResolveAttributeDomains;
    }

    public NameValueCollection QueryItems
    {
        get; set;
    }

    public void AddQueryItems(IDictionary<string, object> dict, bool append = false)
    {
        if (append == false)
        {
            this.QueryItems.Clear();
        }

        foreach (var key in dict.Keys)
        {
            object val = dict[key];

            if (val is System.Double ||
               val is System.Single)
            {
                val = Convert.ToDouble(val).ToPlatformNumberString();
            }
            else
            {
                val = val.ToString();
            }

            this.QueryItems.Add(key, (string)val);
        }
    }

    public bool QueryGeometry { get; set; }

    public SpatialReference FeatureSpatialReference { get; set; }

    public QueryFields Fields { get; set; }

    public bool SuppressResolveAttributeDomains { get; set; }

    virtual public ApiQueryFilter Clone()
    {
        ApiQueryFilter clone = new ApiQueryFilter(this);
        return clone;
    }

    public string Tool { get; set; }
}
