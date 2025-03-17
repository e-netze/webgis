namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface IGeoReferenceItem
{
    string Id { get; }
    string Label { get; }
    string Value { get; }

    string Link { get; }

    string SubText { get; }

    string Thumbnail { get; }

    double[] Coords { get; }

    double[] BBox { get; }

    string Category { get; }
    double Score { get; }
}
