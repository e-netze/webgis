#nullable enable

namespace E.Standard.WebMapping.Core;

public enum TimeEpochRelation
{
    Default
}

public class TimeEpochDefinition
{
    public TimeEpochDefinition()
    {
    }

    public long? StartTime { get; set; }
    public long? EndTime { get; set; }

    public TimeEpochRelation Relation { get; set; } = TimeEpochRelation.Default;

    public long[]? ToJavascriptEpochArray()
    {
        return (StartTime.HasValue, EndTime.HasValue) switch
        {
            (true, true) => new long[] { StartTime!.Value, EndTime!.Value },
            (true, false) => new long[] { StartTime!.Value },
            (false, true) => new long[] { EndTime!.Value },
            _ => null
        };
    }
}
