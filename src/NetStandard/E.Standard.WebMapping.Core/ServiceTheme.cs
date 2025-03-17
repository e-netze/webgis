using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Linq;
using System.Text;

namespace E.Standard.WebMapping.Core;

public class ServiceTheme
{
    public string Id { get; set; }
    public string Name { get; set; }

    public static ServiceDiagnostic CheckServiceLayers(IMapService service, ServiceTheme[] serviceThemes)
    {
        if (service == null)
        {
            return null;
        }

        if (serviceThemes == null) { return new ServiceDiagnostic() { State = ServiceDiagnosticState.Ok }; }
        if (service.Layers == null && serviceThemes.Length > 0) { return new ServiceDiagnostic() { State = ServiceDiagnosticState.LayersMissing, Message = "All Layers" }; }


        int state = (int)ServiceDiagnosticState.Ok;
        StringBuilder message = new StringBuilder();

        foreach (var serviceTheme in serviceThemes)
        {
            var layer = service.Layers.ThreadSafeLinq().Where(m => m.ID == serviceTheme.Id).FirstOrDefault();

            if (layer == null)
            {
                state = Math.Max(state, (int)ServiceDiagnosticState.LayersMissing);
                message.Append("ERROR: Layer " + serviceTheme.Name + " is missing\n");
            }
            else if (layer.Name != serviceTheme.Name)
            {
                state = Math.Max(state, (int)ServiceDiagnosticState.LayersNameConfusion);
                message.Append("WARNING: Layer-Name-Confusion " + serviceTheme.Name + "<>" + layer.Name + "\n");
            }
        }

        return new ServiceDiagnostic()
        {
            State = (ServiceDiagnosticState)state,
            Message = message.ToString()
        };
    }
}
