namespace E.Standard.WebMapping.Core.Abstraction;

public interface IField
{
    string Name { get; }
    string Alias { get; }
    FieldType Type { get; }
}
