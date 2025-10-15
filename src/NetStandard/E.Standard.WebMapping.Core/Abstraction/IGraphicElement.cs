using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.Core.Abstraction;

public interface IGraphicElement
{
    void Draw(ICanvas canvas, IMap map);
    
    Envelope Extent { get; }   
}
