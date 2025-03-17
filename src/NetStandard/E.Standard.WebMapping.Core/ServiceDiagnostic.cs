using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebMapping.Core;

public class ServiceDiagnostic
{
    public ServiceDiagnosticState State { get; internal set; }
    public string Message { get; internal set; }

    public bool ThrowExeption(IMapService service)
    {
        if (service == null)
        {
            return false;
        }

        var level = service.DiagnosticsWaringLevel;
        if (service.Map != null &&
            service.Map.DiagnosticsWaringLevel != ServiceDiagnosticsWarningLevel.Never &&
            (int)service.Map.DiagnosticsWaringLevel > (int)level)
        {
            level = service.Map.DiagnosticsWaringLevel;
        }

        return ThrowExeption(level);
    }

    public bool ThrowExeption(ServiceDiagnosticsWarningLevel level)
    {
        switch (level)
        {
            case ServiceDiagnosticsWarningLevel.Never:
                return false;
            case ServiceDiagnosticsWarningLevel.Error:
                return (int)this.State >= 100;
            case ServiceDiagnosticsWarningLevel.Warning:
                return (int)this.State >= 1;
        }

        return false;
    }

    static public ServiceDiagnostic Empty = new ServiceDiagnostic();
}
