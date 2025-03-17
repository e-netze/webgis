namespace Api.Core.Models.Diagnostic;

public enum DiagnosticElementStatus
{
    Ok = 0,
    Missing = 1,
    NameConfusion = 2,
    CmsMissing = 4,
    Execption = 8
}
