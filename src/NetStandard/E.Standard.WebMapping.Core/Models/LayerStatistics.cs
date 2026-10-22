using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Models;

public enum LayerStatisticsFieldKind
{
    Text,
    Number,
    Date
}

public class LayerStatisticsRequest
{
    public string Field { get; set; }

    public LayerStatisticsFieldKind FieldKind { get; set; }

    /// <summary>
    /// Group the features by the field values (count per value)
    /// </summary>
    public bool GroupByValues { get; set; }

    /// <summary>
    /// Maximum number of returned groups (ordered by count desc). The rest is summarized in OtherFeaturesCount
    /// </summary>
    public int MaxGroups { get; set; }
}

public class LayerStatistics
{
    /// <summary>
    /// Number of all features matching the filter
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Number of features with a NULL value in the field
    /// </summary>
    public long NullCount { get; set; }

    /// <summary>
    /// Min/Max: double for number fields, formatted string for date fields
    /// </summary>
    public object Min { get; set; }
    public object Max { get; set; }

    public double? Average { get; set; }
    public double? Sum { get; set; }

    public List<LayerStatisticsGroup> Groups { get; set; }

    public int OtherGroupsCount { get; set; }
    public long OtherFeaturesCount { get; set; }
}

public class LayerStatisticsGroup
{
    /// <summary>
    /// Raw value from the service (string, long, double, ... or null)
    /// Used to build a where clause for this group.
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// Display label (eg. coded value domain name)
    /// </summary>
    public string Label { get; set; }

    public long Count { get; set; }
}
