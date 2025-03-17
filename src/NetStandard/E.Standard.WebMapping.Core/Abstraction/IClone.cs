namespace E.Standard.WebMapping.Core.Abstraction;

public interface IClone<TInstance, TParent>
{
    TInstance Clone(TParent parent);
}
