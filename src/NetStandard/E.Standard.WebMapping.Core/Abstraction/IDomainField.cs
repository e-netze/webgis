namespace E.Standard.WebMapping.Core.Abstraction;

public interface IDomainField : IField
{
    FieldDomain Domain { get; }
}
