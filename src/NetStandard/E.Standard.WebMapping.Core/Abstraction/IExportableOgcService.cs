using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IExportableOgcService
{
    bool ExportWms { get; set; }
    Envelope OgcEnvelope { get; set; }
}
