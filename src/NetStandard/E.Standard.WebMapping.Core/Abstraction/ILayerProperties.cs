namespace E.Standard.WebMapping.Core.Abstraction;

public interface ILayerProperties
{
    string Id { get; }

    string Aliasname { get; }

    string LegendAliasname { get; }

    bool Visible { get; }

    bool Locked { get; }

    bool ShowInLegend { get; }

    string Name { get; }
}
