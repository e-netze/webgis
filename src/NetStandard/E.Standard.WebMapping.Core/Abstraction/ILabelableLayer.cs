namespace E.Standard.WebMapping.Core.Abstraction;

public interface ILabelableLayer
{
    Renderer.LabelRenderer LabelRenderer
    {
        set;
    }
    bool UseLabelRenderer
    {
        get;
        set;
    }
}
