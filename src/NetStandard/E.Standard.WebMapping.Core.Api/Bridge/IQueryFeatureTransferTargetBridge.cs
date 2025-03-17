namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface IQueryFeatureTransferTargetBridge
{
    string ServiceId { get; }
    string EditThemeId { get; }

    bool PipelineSuppressAutovalues { get; set; }
    bool PipelineSuppressValidation { get; set; }
}
