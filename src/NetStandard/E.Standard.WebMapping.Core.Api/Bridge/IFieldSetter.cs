namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface IFieldSetter
{
    string Field { get; }
    string ValueExpression { get; }

    bool IsDefaultValue { get; }
    bool IsRequired { get; }
}
