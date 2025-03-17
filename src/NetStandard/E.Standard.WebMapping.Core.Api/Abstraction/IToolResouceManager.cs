using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IToolResouceManager
{
    void AddResource(string name, object resource);

    void AddImageResource(string name, IBitmap image);
    void AddImageResource(string name, byte[] image);
}
