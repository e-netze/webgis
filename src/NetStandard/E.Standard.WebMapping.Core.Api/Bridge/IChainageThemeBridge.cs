namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface IChainageThemeBridge : IApiObjectBridge
{
    string Id { get; }
    string Name { get; }
    string ServiceId { get; }

    string PointLayerId { get; }
    string LineLayerId { get; }

    int CalcSrefId { get; }

    string Expression { get; set; }
    string Unit { get; set; }
    string PointLineRelation { get; set; }
    string PointStatField { get; set; }
}
